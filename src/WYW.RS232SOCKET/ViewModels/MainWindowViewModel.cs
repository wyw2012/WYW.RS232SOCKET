using System;
using WYW.RS232SOCKET.Models;
using WYW.UI.Controls;
using WYW.Communication.TransferLayer;
using WYW.Communication.ApplicationlLayer;
using WYW.Communication.Protocol;
using System.Threading;
using System.Windows;

namespace WYW.RS232SOCKET.ViewModels
{
    partial class MainWindowViewModel : ObservableObject
    {
        private Thread sendThread = null;
        private static object displayLocker = new object();
        public MainWindowViewModel()
        {
            OpenCommand = new RelayCommand(Open);
            CloseCommand = new RelayCommand(Close);

            SendCommand = new RelayCommand(Send);
            StopSendCommand = new RelayCommand(StopSend);
            ClearCommand = new RelayCommand(Clear);
            OpenFileCommand = new RelayCommand(OpenFile);
            CopyTextCommand = new RelayCommand<object>(CopyText);

            CreateRegisterCommand = new RelayCommand(CreateRegister);
            LoadTemplateCommand = new RelayCommand(LoadTemplate);
            SaveTemplateCommand = new RelayCommand(SaveTemplate);
            ReadRegisterCommand = new RelayCommand(ReadRegister);
            WriteRegisterCommand = new RelayCommand(WriteRegister);
        }

        #region 属性
        private CommunicationType communicationType;
        private Device device;
        private bool isModbus;
        private bool isOpen;
        /// <summary>
        /// 通信类型
        /// </summary>
        public CommunicationType CommunicationType
        {
            get => communicationType;
            set => SetProperty(ref communicationType, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public Device Device
        {
            get => device;
            set => SetProperty(ref device, value);
        }

        private TransferBase client;

        /// <summary>
        /// 
        /// </summary>
        public TransferBase Client
        {
            get => client;
            set => SetProperty(ref client, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsModbus
        {
            get => isModbus;
            set => SetProperty(ref isModbus, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsOpen
        {
            get => isOpen;
            set => SetProperty(ref isOpen, value);
        }
        public Config Config { get; } = new Config();
        #endregion

        #region 命令

        public RelayCommand OpenCommand { get; private set; }
        public RelayCommand CloseCommand { get; private set; }
        private void Open()
        {
            try
            {
                if (IsModbus)
                {
                    if (CommunicationType == CommunicationType.UDPClient ||
                        CommunicationType == CommunicationType.UDPServer)
                    {
                        throw new Exception($"Modbus协议不支持{CommunicationType}");
                    }
                }
                switch (CommunicationType)
                {
                    case CommunicationType.RS232:
                        Client = new RS232Client(Config.RS232.PortName, Config.RS232.BaudRate, Config.RS232.Parity, Config.RS232.DataBits, Config.RS232.StopBits, Config.Common.WriteBufferSize, Config.Common.ReceiveBufferSize);
                        break;
                    case CommunicationType.TCPClient:
                        Client = new TCPClient(Config.TCPClient.RemoteIP, Config.TCPClient.RemotePort, Config.Common.ReceiveBufferSize);
                        break;
                    case CommunicationType.TCPServer:
                        Client = new TCPServer(Config.TCPServer.LocalIP, Config.TCPServer.LocalPort, Config.Common.ReceiveBufferSize);
                        break;
                    case CommunicationType.UDPClient:
                        Client = new UDPClient(Config.UDPClient.LocalIP, Config.UDPClient.LocalPort, Config.UDPClient.BroadcastAddress, Config.UDPClient.BroadcastPort, Config.Common.ReceiveBufferSize);
                        break;
                    case CommunicationType.UDPServer:
                        Client = new UDPServer(Config.UDPServer.LocalIP, Config.UDPServer.LocalPort, Config.Common.ReceiveBufferSize);
                        break;
                }
                if (IsModbus)
                {

                    if (Config.Modbus.ModbusType == ModbusType.主站)
                    {
                        Device = new ModbusMaster(Client);
                    }
                    else
                    {
                        Device = new ModbusSlave(Client, Config.Modbus.SlaveID);
                    }
                }
                else
                {
                    Device = new Device(Client, Communication.ProtocolType.HexBare);
                }
                Clear();
                Client.StatusChangedEvent += Client_StatusChangedEvent;
                Device.ProtocolReceivedEvent += Device_ProtocolReceivedEvent; ;
                Device.ProtocolTransmitedEvent += Device_ProtocolTransmitedEvent;
                Device.Open();
                IsOpen = true;
            }
            catch (Exception ex)
            {
                MessageBoxWindow.Error(ex.Message, "错误");
            }
        }

        private void Close()
        {
            if (Device != null)
            {
                try
                {
                    StopSend(); 
                    Device.Close();
                    Device.ProtocolReceivedEvent -= Device_ProtocolReceivedEvent;
                    Device.ProtocolTransmitedEvent -= Device_ProtocolTransmitedEvent;
                    Device.Dispose();
                    Device = null;
                    IsOpen = false;
                }
                catch (Exception ex)
                {
                    MessageBoxWindow.Error(ex.Message, "错误");
                }
            }
        }

        #endregion

        #region 事件处理
        private void Client_StatusChangedEvent(object sender, StatusChangedEventArgs e)
        {
            lock (displayLocker)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Config.Display.DisplayItems.Count >= Config.Display.MaxItemCount)
                    {
                        Config.Display.DisplayItems.RemoveAt(0);
                    }

                    Config.Display.DisplayItems.Add($"[{e.CreateTime:HH:mm:ss.fff}] {e.Message}");
                });
            }
        }
        /// <summary>
        /// 发送事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="obj"></param>
        private void Device_ProtocolTransmitedEvent(object sender, ProtocolBase obj)
        {
            Config.Status.TotalSended += obj.FullBytes.Length;
            if (!Config.Display.EnableDisplay)
                return;
            lock (displayLocker)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Config.Display.DisplayItems.Count >= Config.Display.MaxItemCount)
                    {
                        Config.Display.DisplayItems.RemoveAt(0);
                    }
                    if (Config.Display.DisplayType == 0)
                    {
                        Config.Display.DisplayItems.Add($"[{obj.CreateTime:HH:mm:ss.fff}] [Tx] {obj.FullBytes.ToHexString()}");
                    }
                    else
                    {
                        Config.Display.DisplayItems.Add($"[{obj.CreateTime:HH:mm:ss.fff}] [Tx] {obj.FullBytes.ToUTF8()}");
                    }
                });
            }
        }
        /// <summary>
        /// 接收事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="obj"></param>
        private void Device_ProtocolReceivedEvent(object sender, ProtocolBase obj)
        {
            Config.Status.TotalReceived += obj.FullBytes.Length;
            if (!Config.Display.EnableDisplay)
                return;
            lock (displayLocker)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Config.Display.DisplayItems.Count >= Config.Display.MaxItemCount)
                    {
                        Config.Display.DisplayItems.RemoveAt(0);
                    }
                    if (Config.Display.DisplayType == 0)
                    {
                        Config.Display.DisplayItems.Add($"[{obj.CreateTime:HH:mm:ss.fff}] [Rx] {obj.FullBytes.ToHexString()}");
                    }
                    else
                    {
                        Config.Display.DisplayItems.Add($"[{obj.CreateTime:HH:mm:ss.fff}] [Rx] {obj.FullBytes.ToUTF8()}");
                    }
                });
            }
            if (IsModbus && Config.Modbus.ModbusType == ModbusType.从站)
            {
                ProcessSlaveRecived(obj);
            }
        }
        #endregion

    }
}

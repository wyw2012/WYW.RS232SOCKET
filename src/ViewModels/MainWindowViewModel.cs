using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WYW.RS232SOCKET.Protocol;
using WYW.RS232SOCKET.Models;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Net;
using System.IO;
using WYW.RS232SOCKET.Devices;
using WYW.RS232SOCKET.Events;
using System.Collections.ObjectModel;
using MessageBoxImage = WYW.UI.Controls.MessageBoxImage;
using WYW.UI.Controls;

namespace WYW.RS232SOCKET.ViewModels
{
    class MainWindowViewModel : ObservableObject
    {
        private DeviceBase device = null;
        private List<byte[]> sendQueue = new List<byte[]>(); // 循环发送队列，所有的发送数据必须加入发送队列
        private static object ReadLock = new object();
        private List<byte> receiveBuffer = new List<byte>();
        private DateTime lastReceiveTime = DateTime.Now; // 最后一次接收数据的时间
        private bool isKeepThreadAlive = false;
        private int bufferSize = 4096;
        public MainWindowViewModel()
        {
            string hostName = Dns.GetHostName();
            var ip= Dns.GetHostAddresses(hostName).Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).Select(x=>x.ToString()).ToArray();
            IPList = new List<string>() { "127.0.0.1" };
            IPList.AddRange(ip);
            InitCommand();

            if (IPList.Count > 0)
            {
                // 设置广播地址
                var temp = IPList[LocalIPIndex].ToString().Split('.');
                if (temp.Length == 4)
                {
                    BroadcastAddress = $"{temp[0]}.{temp[1]}.{temp[2]}.255";
                }
                // 远程地址默认为本地地址
                RemoteIP = IPList[LocalIPIndex].ToString();
            }
        }

        #region 属性
        #region Common
        private int protocolType;
        private int communicationType;
        private bool isSendAscii = true;
        private bool isReceiveAscii = true;
        private bool isCircledSend;
        private bool isSendFile;
        private int circleInterval = 1000;
        private string sendText;
        private int totalReceived;
        private int totalSended;
        private bool isOpened;
        private double progress;
        private int asciiCheckType;
        private bool sendButtonEnabled = true;
        private Visibility protocolGridVisible = Visibility.Visible;
        /// <summary>
        /// 设备通讯类型，0 串口；1 TCP Client；2 TCP Server；3 UDP Client；4 UDP Server
        /// </summary>
        public int CommunicationType
        {
            get => communicationType;
            set
            {
                SetProperty(ref communicationType, value);
                ProtocolType = 0;
                if (value <= 2)
                {
                    ProtocolGridVisible = Visibility.Visible;
                }
                else
                {
                    ProtocolGridVisible = Visibility.Collapsed;
                }
            }
        }
        /// <summary>
        /// 协议类型，0 None;1 Modbus
        /// </summary>
        public int ProtocolType
        {
            get { return protocolType; }
            set
            {
                if (protocolType != value)
                {
                    protocolType = value;
                    OnPropertyChanged("ProtocolType");
                }
            }
        }
        public string[] PortNames => System.IO.Ports.SerialPort.GetPortNames();

        public List<string> IPList { get; }


        /// <summary>
        /// 是否按照ASCII格式发送数据
        /// </summary>
        public bool IsSendAscii
        {
            get { return isSendAscii; }
            set
            {
                if (isSendAscii != value)
                {
                    isSendAscii = value;
                    OnPropertyChanged("IsSendAscii");
                }
            }
        }

        /// <summary>
        ///  是否按照ASCII格式显示接受数据
        /// </summary>
        public bool IsReceiveAscii
        {
            get { return isReceiveAscii; }
            set
            {
                if (isReceiveAscii != value)
                {
                    isReceiveAscii = value;
                    OnPropertyChanged("IsReceiveAscii");
                }
            }
        }

        /// <summary>
        /// 是否定时发送
        /// </summary>
        public bool IsCircledSend
        {
            get { return isCircledSend; }
            set
            {
                if (isCircledSend != value)
                {
                    isCircledSend = value;
                    OnPropertyChanged("IsCircledSend");
                }
            }
        }

        /// <summary>
        /// 循环发送时间间隔，单位ms
        /// </summary>
        public int CircleInterval
        {
            get { return circleInterval; }
            set
            {
                if (circleInterval != value)
                {
                    circleInterval = value;
                    OnPropertyChanged("CircleInterval");
                }
            }
        }

        /// <summary>
        /// 是否发送文件
        /// </summary>
        public bool IsSendFile
        {
            get { return isSendFile; }
            set
            {
                if (isSendFile != value)
                {
                    isSendFile = value;
                    OnPropertyChanged("IsSendFile");
                    if (value)
                    {
                        var ofd = new OpenFileDialog
                        {
                            Filter = "文本文档|*.txt",
                            FilterIndex = 1,
                            RestoreDirectory = true,
                        };
                        if (ofd.ShowDialog() == true)
                        {
                            SendText = ofd.FileName;
                        }
                        else
                        {
                            IsSendFile = false;
                        }
                    }
                    else
                    {
                        SendText = "";
                        Progress=0;
                    }
                }
            }
        }

        /// <summary>
        ///  发送栏文本
        /// </summary>
        public string SendText
        {
            get { return sendText; }
            set
            {
                if (sendText != value)
                {
                    sendText = value;
                    OnPropertyChanged("SendText");
                }
            }
        }

        /// <summary>
        /// 累计接收字节
        /// </summary>
        public int TotalReceived
        {
            get { return totalReceived; }
            set
            {
                if (totalReceived != value)
                {
                    totalReceived = value;
                    OnPropertyChanged("TotalReceived");
                }
            }
        }

        /// <summary>
        /// 积累发送字节
        /// </summary>
        public int TotalSended
        {
            get { return totalSended; }
            set
            {
                if (totalSended != value)
                {
                    totalSended = value;
                    OnPropertyChanged("TotalSended");
                }
            }
        }
        /// <summary>
        /// 设备是否打开
        /// </summary>
        public bool IsOpened
        {
            get { return isOpened; }
            set
            {
                if (isOpened != value)
                {
                    isOpened = value;
                    OnPropertyChanged("IsOpened");
                }
            }
        }

        /// <summary>
        /// 发送进度，仅适用于发送文件
        /// </summary>
        public double Progress
        {
            get { return progress; }
            set
            {
                if (progress != value)
                {
                    progress = value;
                    OnPropertyChanged("Progress");
                }
            }
        }


        /// <summary>
        /// ASCII的校验类型。0 无校验；1 加回车；2 加回车换行；3 加累加和
        /// </summary>
        public int AsciiCheckType
        {
            get => asciiCheckType;
            set => SetProperty(ref asciiCheckType, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public ObservableCollection<string> DisplayItems { get; } = new ObservableCollection<string>();

        /// <summary>
        /// 
        /// </summary>
        public bool SendButtonEnabled
        {
            get => sendButtonEnabled;
            set => SetProperty(ref sendButtonEnabled, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public Visibility ProtocolGridVisible
        {
            get => protocolGridVisible;
            set => SetProperty(ref protocolGridVisible, value);
        }
        #endregion

        #region RS232
        private int portNameIndex;
        private int baudRate = 9600;
        private int parity = 0;
        private int dataBits = 8;
        private int stopBits = 1;


        /// <summary>
        /// 
        /// </summary>
        /// 
        public int PortNameIndex
        {
            get { return portNameIndex; }
            set
            {
                if (portNameIndex != value)
                {
                    portNameIndex = value;
                    OnPropertyChanged("PortName");
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public int BaudRate
        {
            get { return baudRate; }
            set
            {
                if (baudRate != value)
                {
                    baudRate = value;
                    OnPropertyChanged("BaudRate");
                }
            }
        }

        /// <summary>
        /// 奇偶校验位，0 None；1 Odd；2 Even；4 Mark；5 Space
        /// </summary>
        public int Parity
        {
            get { return parity; }
            set
            {
                if (parity != value)
                {
                    parity = value;
                    OnPropertyChanged("Parity");
                }
            }
        }

        /// <summary>
        /// 数据位，取值范围[5,8]
        /// </summary>
        public int DataBits
        {
            get { return dataBits; }
            set
            {
                if (dataBits != value)
                {
                    dataBits = value;
                    OnPropertyChanged("DataBits");
                }
            }
        }



        /// <summary>
        /// 停止位，0 None；1 One；2 Two；3 OnePointFive,
        /// </summary>
        public int StopBits
        {
            get { return stopBits; }
            set
            {
                if (stopBits != value)
                {
                    stopBits = value;
                    OnPropertyChanged("StopBits");
                }
            }
        }


        #endregion

        #region TCP Client
        private string remoteIP;
        private int remotePort = 502;

        /// <summary>
        /// 
        /// </summary>
        public string RemoteIP
        {
            get => remoteIP;
            set => SetProperty(ref remoteIP, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public int RemotePort
        {
            get => remotePort;
            set => SetProperty(ref remotePort, value);
        }


        #endregion

        #region TCP Server
        private int localIPIndex;
        private int localPort = 502;
        /// <summary>
        /// 
        /// </summary>
        public int LocalIPIndex
        {
            get { return localIPIndex; }
            set
            {
                if (localIPIndex != value)
                {
                    localIPIndex = value;
                    OnPropertyChanged("LocalIP");

                    if (CommunicationType == 3)
                    {
                        var temp = IPList[LocalIPIndex].ToString().Split('.');
                        if (temp.Length == 4)
                        {
                            BroadcastAddress = $"{temp[0]}.{temp[1]}.{temp[2]}.255";
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int LocalPort
        {
            get { return localPort; }
            set
            {
                if (localPort != value)
                {
                    localPort = value;
                    OnPropertyChanged("LocalPort");
                }
            }
        }
        #endregion

        #region UDP Client

        private string broadcastAddress;
        private int broadcastPort;
        /// <summary>
        /// 广播地址
        /// </summary>
        public string BroadcastAddress
        {
            get => broadcastAddress;
            set => SetProperty(ref broadcastAddress, value);
        }

        /// <summary>
        /// 广播端口
        /// </summary>
        public int BroadcastPort
        {
            get => broadcastPort;
            set => SetProperty(ref broadcastPort, value);
        }

        #endregion

        #region Modbus

        private int stationType;
        private int slaveID = 1;
        private int transactionID;

        /// <summary>
        /// 站点类型,0 主站；1 从站
        /// </summary>
        public int StationType
        {
            get { return stationType; }
            set
            {
                if (stationType != value)
                {
                    stationType = value;
                    OnPropertyChanged("StationType");
                }
            }
        }


        /// <summary>
        /// 从站地址
        /// </summary>
        public int SlaveID
        {
            get { return slaveID; }
            set
            {
                if (slaveID != value)
                {
                    slaveID = value;
                    OnPropertyChanged("SlaveID");
                }
            }
        }

        /// <summary>
        /// 事务标识符
        /// </summary>
        public int TransactionID
        {
            get { return transactionID; }
            set
            {
                if (transactionID != value)
                {
                    transactionID = value;
                    OnPropertyChanged("TransactionID");
                }
            }
        }

        private int startAddress;
        private int registerCount = 10;
        /// <summary>
        /// 
        /// </summary>
        public int StartAddress
        {
            get { return startAddress; }
            set
            {
                if (startAddress != value)
                {
                    startAddress = value;
                    OnPropertyChanged("StartAddress");
                }
            }
        }

        public int RegisterCount
        {
            get { return registerCount; }
            set
            {
                if (registerCount != value)
                {
                    registerCount = value;
                    OnPropertyChanged("RegisterCount");
                }
            }
        }

        public ObservableCollection<Register> Registers { get; } = new ObservableCollection<Register>();

        #endregion

        #endregion


        #region 命令
        private void InitCommand()
        {
            OpenCommand = new RelayCommand(Open);
            CloseCommand = new RelayCommand(Close);
            ClearCommand = new RelayCommand(Clear);
            SendCommand = new RelayCommand(Send);
            StopCommand = new RelayCommand(Stop);
            CreateResiterCommand = new RelayCommand(CreateResiter);
            ReadResiterCommand = new RelayCommand(ReadResiter);
            WriteResiterCommand = new RelayCommand(WriteResiter);
        }

        public RelayCommand ClearCommand { get; private set; }
        public RelayCommand OpenCommand { get; private set; }
        public RelayCommand CloseCommand { get; private set; }
        public RelayCommand SendCommand { get; private set; }
        public RelayCommand CreateResiterCommand { get; private set; }
        public RelayCommand ReadResiterCommand { get; private set; }
        public RelayCommand WriteResiterCommand { get; private set; }
        public RelayCommand StopCommand { get; private set; }

        private void Clear()
        {
            DisplayItems.Clear();
            TotalReceived = 0;
            TotalSended = 0;
        }

        private void Open()
        {
            try
            {
                switch (CommunicationType)
                {
                    case 0:
                        device = new RS232(PortNames[portNameIndex], BaudRate, Parity, DataBits, StopBits, bufferSize);
                        break;
                    case 1:
                        device = new TCPClient(RemoteIP, RemotePort, bufferSize);
                        break;
                    case 2:
                        device = new TCPServer(IPList[LocalIPIndex].ToString(), LocalPort, bufferSize);
                        break;
                    case 3:
                        device = new UDPClient(IPList[LocalIPIndex].ToString(), LocalPort, BroadcastAddress, BroadcastPort, bufferSize);
                        break;
                    case 4:
                        device = new UDPServer(IPList[LocalIPIndex].ToString(), LocalPort, bufferSize);
                        break;
                }

                TotalReceived = 0;
                TotalSended = 0;
                device.CommunicationMessageEvent += Device_CommunicationMessageEvent;
                device.DeviceStatusChangedEvent += Device_DeviceNotifyEvent;
                device.Open();
                IsOpened = true;
            }
            catch (Exception ex)
            {
                MessageBoxWindow.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close()
        {
            if (device != null)
            {
                try
                {
                    device.Close();
                    device.CommunicationMessageEvent -= Device_CommunicationMessageEvent;
                    device.DeviceStatusChangedEvent -= Device_DeviceNotifyEvent;
                    device = null;
                    IsOpened = false;
                    IsCircledSend = false;
                    IsSendFile = false;
                    isKeepThreadAlive = false;
                }
                catch (Exception ex)
                {
                    MessageBoxWindow.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Send()
        {
            try
            {
                sendQueue.Clear();
                if (IsSendFile)
                {
                    CheckSendFile(out sendQueue);
                }
                else
                {
                    CheckSendText(out sendQueue);
                }

                ThreadPool.QueueUserWorkItem(delegate
                {
                    isKeepThreadAlive = true;
                    SendButtonEnabled = false;
                    do
                    {
                        if (!IsOpened || !isKeepThreadAlive)
                            break;
                        Progress = 0;
                        for (int i = 0; i < sendQueue.Count; i++)
                        {

                            if (!IsOpened || !isKeepThreadAlive)
                                break;
                            device.Write(sendQueue[i]);
                            Progress = ((i + 1) * 100) / sendQueue.Count;
                            if (i != sendQueue.Count - 1 || isCircledSend)
                            {
                                Thread.Sleep(CircleInterval);
                            }

                        }


                    } while (isCircledSend);


                    SendButtonEnabled = true;

                });
            }
            catch (Exception ex)
            {
                SendButtonEnabled = true;
                MessageBoxWindow.Show(ex.Message, "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Stop()
        {
            isKeepThreadAlive = false;
        }

        private void CreateResiter()
        {
            Registers.Clear();
            for (int i = 0; i < RegisterCount; i++)
            {
                Registers.Add(new Register(i + StartAddress, 0));
            }
        }

        private void ReadResiter()
        {
            if (device == null || !IsOpened)
            {
                MessageBoxWindow.Show("请先点击“开始”按钮", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var array = GetMasterSendReadBytes();
            ThreadPool.QueueUserWorkItem(delegate
            {
                isKeepThreadAlive = true;
                SendButtonEnabled = false;
                do
                {
                    if (!IsOpened || !isKeepThreadAlive)
                        break;
                    device.Write(array);
                    Thread.Sleep(CircleInterval);
 
                } while (isCircledSend);


                SendButtonEnabled = true;

            });
        }
        private void WriteResiter()
        {
            if (device == null || !IsOpened)
            {
                MessageBoxWindow.Show("请先点击“开始”按钮", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var array = GetMasterSendWriteMoreRegistersBytes();
            ThreadPool.QueueUserWorkItem(delegate
            {
                isKeepThreadAlive = true;
                SendButtonEnabled = false;
                do
                {
                    if (!IsOpened || !isKeepThreadAlive)
                        break;
                    device.Write(array);
                    Thread.Sleep(CircleInterval);

                } while (isCircledSend);
                SendButtonEnabled = true;
            });
        }
        #endregion


        #region 事件
        private void Device_DeviceNotifyEvent(object sender, DeviceStautsChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new Action(delegate
            {
                if (DisplayItems.Count > 1000)
                {
                    DisplayItems.RemoveAt(0);
                }
                DisplayItems.Add(e.ToString());
            }));
        }

        private void Device_CommunicationMessageEvent(object sender, DeviceDataTransferedEventArgs e)
        {
            // 解析Modbus协议
            if (ProtocolType == 1)
            {
                if (e.Type == MessageType.Receive)
                {
                    lastReceiveTime = DateTime.Now;
                    List<ProtocolBase> items = new List<ProtocolBase>();
                    lock (ReadLock)
                    {
                        // 如果两次接收数据的时间大于100ms，则自动清除缓存区数据
                        if ((DateTime.Now - lastReceiveTime).TotalMilliseconds > 100)
                        {
                            receiveBuffer.Clear();
                        }
                        lastReceiveTime = DateTime.Now;
                        receiveBuffer.AddRange(e.Content);

                        if (CommunicationType == 0)
                        {
                            items = ModbusRTU.Analyse(receiveBuffer);
                        }
                        else
                        {
                            items = ModbusTCP.Analyse(receiveBuffer);
                        }
                    }

                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        if (StationType == 1)
                        {
                            foreach (var item in items)
                            {
                                if (item.NodeID == SlaveID)
                                {
                                    var array = GetSlaveReceive(item);
                                    device.Write(array);
                                }
                            }
                        }
                        else
                        {
                            foreach (var item in items)
                            {
                                ProcessMasterReceive(item);
                            }
                        }
                    });
                }
            }
            if (e.Type == MessageType.Receive)
            {
                TotalReceived += e.Content.Length;
            }
            else
            {
                TotalSended += e.Content.Length;
            }
            Application.Current.Dispatcher.Invoke(new Action(delegate
            {
                if (DisplayItems.Count > 1000)
                {
                    DisplayItems.RemoveAt(0);
                }
                if (ProtocolType == 0 && IsReceiveAscii)
                {
                    DisplayItems.Add(e.ToASCII());
                }
                else
                {
                    DisplayItems.Add(e.ToHex());
                }
            }));

        }

        #endregion

        #region 私有函数
        private void CheckSendText(out List<byte[]> list)
        {
            if (device == null)
            {
                throw new Exception("请先点击“开始”按钮。");
            }
            if (string.IsNullOrEmpty(SendText))
            {
                throw new Exception("发送栏无数据。");
            }
            list = new List<byte[]>();

            if (IsSendAscii)
            {
                var temp = new List<byte>();
                temp.AddRange(Encoding.UTF8.GetBytes(SendText));
                switch ((CheckType)AsciiCheckType)
                {
                    case CheckType.CR:
                        temp.AddRange(Encoding.UTF8.GetBytes("\r"));
                        break;
                    case CheckType.CRLF:
                        temp.AddRange(Encoding.UTF8.GetBytes("\r\n"));
                        break;
                    case CheckType.CheckSum:
                        temp.Add((byte)temp.Sum(x => x));
                        break;
                }
                list.Add(temp.ToArray());
            }
            else
            {
                var chars = Regex.Replace(SendText, @"\s", ""); // 剔除空格
                if (chars.Length % 2 == 1)
                {
                    throw new Exception("字符格式不符合十六进制。");
                }
                var hexs = Regex.Split(chars, @"(?<=\G.{2})(?!$)");   // 两两分组
                try
                {
                    list.Add(hexs.Select(item => Convert.ToByte(item, 16)).ToArray());
                }
                catch
                {
                    throw new Exception("字符格式不符合十六进制。");
                }
            }
        }

        private void CheckSendFile(out List<byte[]> list)
        {
            if (device == null)
            {
                throw new Exception("请先点击“开始”按钮。");
            }
            if (string.IsNullOrEmpty(SendText))
            {
                throw new Exception("发送栏无数据。");
            }
            list = new List<byte[]>();
            var sendTexts = File.ReadLines(SendText);
            if (IsSendAscii)
            {
                foreach (var item in sendTexts)
                {
                    if (string.IsNullOrEmpty(item))
                    {
                        continue;
                    }
                    var temp = new List<byte>();
                    temp.AddRange(Encoding.UTF8.GetBytes(item));
                    switch ((CheckType)AsciiCheckType)
                    {
                        case CheckType.CR:
                            temp.AddRange(Encoding.UTF8.GetBytes("\r"));
                            break;
                        case CheckType.CRLF:
                            temp.AddRange(Encoding.UTF8.GetBytes("\r\n"));
                            break;
                        case CheckType.CheckSum:
                            temp.Add((byte)temp.Sum(x => x));
                            break;
                    }
                    list.Add(temp.ToArray());

                }
            }
            else
            {
                foreach (var item in sendTexts)
                {
                    if (string.IsNullOrEmpty(item))
                    {
                        continue;
                    }
                    var chars = Regex.Replace(item, @"\s", ""); // 剔除空格
                    if (chars.Length % 2 == 1)
                    {
                        throw new Exception("字符格式不符合十六进制。");
                    }
                    var hexs = Regex.Split(chars, @"(?<=\G.{2})(?!$)");   // 两两分组
                    try
                    {
                        list.Add(hexs.Select(x => Convert.ToByte(x, 16)).ToArray());
                    }
                    catch
                    {
                        throw new Exception("字符格式不符合十六进制。");
                    }
                }
            }
        }
        #endregion

        #region Modbus协议处理
        private byte[] GetMasterSendReadBytes()
        {
            List<byte> content = new List<byte>();
            content.AddRange(GetBytesBigEndian((UInt16)startAddress));
            content.AddRange(GetBytesBigEndian((UInt16)(registerCount)));
            ProtocolBase obj = GetProtocolObject(ModbusCommand.ReadMoreRegisters, content.ToArray());

            return obj.FullBytes;
        }
        private byte[] GetMasterSendWriteMoreRegistersBytes()
        {
            List<byte> content = new List<byte>();
            content.AddRange(GetBytesBigEndian((UInt16)startAddress));
            content.AddRange(GetBytesBigEndian((UInt16)(registerCount)));
            content.Add((byte)(registerCount * 2));
            for (int i = 0; i < registerCount; i++)
            {
                var register = Registers.SingleOrDefault(x => x.Address == i + startAddress);
                if (register != null)
                {
                    content.AddRange(GetBytesBigEndian((UInt16)register.Value));
                }
                else
                {
                    content.AddRange(GetBytesBigEndian((UInt16)0));
                }

            }
            ProtocolBase obj = GetProtocolObject(ModbusCommand.WriteMoreRegisters, content.ToArray());
            return obj.FullBytes;
        }
        private void ProcessMasterReceive(ProtocolBase receive)
        {
            if (receive.Command == ModbusCommand.ReadMoreRegisters)
            {
                int count = receive.Content[0] / 2;
                for (int i = 0; i < count; i++)
                {
                    var register = Registers.SingleOrDefault(x => x.Address == StartAddress + i);
                    var value = GetUInt16BigEndian(receive.Content, i * 2 + 1);
                    if (register != null)
                    {
                        register.Value = value;
                    }
                    else
                    {
                        Registers.Add(new Register(StartAddress + i, value));
                    }
                }
            }
        }
        private byte[] GetSlaveReceive(ProtocolBase receive)
        {

            List<byte> content = new List<byte>();
            Register register;
            int value;

            if (receive is ModbusTCP tcp)
            {
                TransactionID = tcp.TransactionID;
            }
            switch (receive.Command)
            {
                case ModbusCommand.ReadMoreRegisters:
                    var startIndex = GetUInt16BigEndian(receive.Content, 0);
                    var count = GetUInt16BigEndian(receive.Content, 2);
                    content.Add(((byte)(count * 2)));
                    for (int i = 0; i < count; i++)
                    {
                        register = Registers.SingleOrDefault(x => x.Address == startIndex + i);
                        if (register != null)
                        {
                            content.AddRange(GetBytesBigEndian((UInt16)register.Value));
                        }
                        else
                        {
                            content.AddRange(GetBytesBigEndian((UInt16)0));
                        }
                    }
                    break;
                case ModbusCommand.WriteOneRegisters:
                    var registerAddress = GetUInt16BigEndian(receive.Content, 0);
                    var registerValue = GetUInt16BigEndian(receive.Content, 2);
                    content.AddRange(GetBytesBigEndian((UInt16)registerAddress));
                    content.AddRange(GetBytesBigEndian((UInt16)(registerValue)));
                    register = Registers.SingleOrDefault(x => x.Address == registerAddress);
                    value = GetUInt16BigEndian(receive.Content, 2);
                    if (register != null)
                    {
                        register.Value = value;
                    }
                    else
                    {
                        Registers.Add(new Register(registerAddress, value));
                    }
                    break;
                case ModbusCommand.WriteMoreRegisters:
                    startIndex = GetUInt16BigEndian(receive.Content, 0);
                    count = GetUInt16BigEndian(receive.Content, 2);
                    content.AddRange(GetBytesBigEndian((UInt16)startIndex));
                    content.AddRange(GetBytesBigEndian((UInt16)(count)));
                    for (int i = 0; i < count; i++)
                    {
                        register = Registers.SingleOrDefault(x => x.Address == startIndex + i);
                        value = GetUInt16BigEndian(receive.Content, i * 2 + 5);
                        if (register != null)
                        {
                            register.Value = value;
                        }
                        else
                        {
                            Registers.Add(new Register(startIndex + i, value));
                        }
                    }
                    break;
                case ModbusCommand.ReadWriteRegisters:
                    // 处理读取
                    startIndex = GetUInt16BigEndian(receive.Content, 0);
                    count = GetUInt16BigEndian(receive.Content, 2);
                    content.Add(((byte)(count * 2)));
                    for (int i = 0; i < count; i++)
                    {
                        register = Registers.SingleOrDefault(x => x.Address == startIndex + i);
                        if (register != null)
                        {
                            content.AddRange(GetBytesBigEndian((UInt16)register.Value));
                        }
                        else
                        {
                            content.AddRange(GetBytesBigEndian((UInt16)0));
                        }
                    }
                    // 处理写入
                    startIndex = GetUInt16BigEndian(receive.Content, 4);
                    count = GetUInt16BigEndian(receive.Content, 6);
                    for (int i = 0; i < count; i++)
                    {
                        register = Registers.SingleOrDefault(x => x.Address == startIndex + i);
                        value = GetUInt16BigEndian(receive.Content, i * 2 + 9);
                        if (register != null)
                        {
                            register.Value = value;
                        }
                        else
                        {
                            Registers.Add(new Register(startIndex + i, value));
                        }
                    }
                    break;

            }
            ProtocolBase obj = GetProtocolObject(receive.Command, content.ToArray());
            return obj.FullBytes;
        }


        private ProtocolBase GetProtocolObject(ModbusCommand cmd, byte[] content)
        {
            ProtocolBase obj = null;
            if (CommunicationType == 0)
            {
                obj = new ModbusRTU((byte)slaveID, cmd, content);
            }
            else
            {
                obj = new ModbusTCP((byte)slaveID, cmd, content, (UInt16)transactionID);
            }
            return obj;
        }
        private byte[] GetBytesBigEndian(UInt16 value)
        {
            byte[] result = new byte[2];
            result[0] = (byte)(value >> 8);
            result[1] = (byte)(value & 0xFF);
            return result;
        }
        private UInt16 GetUInt16BigEndian(byte[] value, int startIndex = 0)
        {
            int result = 0;
            if (value.Length + startIndex >= 2)
            {
                result = (value[startIndex] << 8) + value[startIndex + 1];
            }
            return (UInt16)result;
        }
        #endregion

    }
}

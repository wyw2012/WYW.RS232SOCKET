using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using WYW.Communication;
using WYW.Communication.Models;
using WYW.Communication.Protocol;
using WYW.Communication.TransferLayer;
using WYW.RS232SOCKET.Common;
using WYW.UI.Controls;

namespace WYW.RS232SOCKET.Models
{
    internal class DeviceController : ObservableObject
    {
        private static object locker = new object();
        public DeviceController()
        {
            string hostName = Dns.GetHostName();
            var ip = Dns.GetHostAddresses(hostName).Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).Select(x => x.ToString()).ToArray();
            IPList.AddRange(ip);
            IPList.Add("127.0.0.1");
            try
            {
               VISAResourceNames.AddRange(VISAClient.GetResouceNames()); 
            }
            catch
            {
            }
            // 设置默认值
            if (IPList.Count > 0)
            {
              Config.TCPClient.RemoteIP = Config.TCPServer.LocalIP = Config.UDPClient.LocalIP = Config.UDPServer.LocalIP = IPList[0];
            }
            if (PortNames.Length > 0)
            {
                Config.RS232.PortName = PortNames[0];
            }

        }
        public string[] PortNames => System.IO.Ports.SerialPort.GetPortNames();
        public List<string> IPList { get; } = new List<string>();

        public List<string> VISAResourceNames { get; }=new List<string>();

        #region 协议
        private CommunicationType communicationType;
        private int protocolType;

        /// <summary>
        /// 通信类型
        /// </summary>
        public CommunicationType CommunicationType
        {
            get => communicationType;
            set
            {
                SetProperty(ref communicationType, value);
                if (value == CommunicationType.UDPClient ||
                    value == CommunicationType.UDPServer ||
                    value== CommunicationType.VISA)
                {
                    ProtocolType = 0;
                }
            }
        }

        /// <summary>
        /// 协议类型，0 None; 1 Modbus RTU；2 Modbus TCP
        /// </summary>
        public int ProtocolType
        {
            get => protocolType;
            set => SetProperty(ref protocolType, value);
        }

        private bool isHighAccuracyTimer;

        /// <summary>
        /// 高精度模式
        /// </summary>
        public bool IsHighAccuracyTimer { get => isHighAccuracyTimer; set => SetProperty(ref isHighAccuracyTimer, value); }

        #endregion

        #region 实时显示
        /// <summary>
        /// 消息集合
        /// </summary>
        public ObservableCollection<string> MessageCollection { get; } = new ObservableCollection<string>();
        /// <summary>
        /// 寄存器集合
        /// </summary>
        public ObservableCollectionEx<Register> RegisterCollection { get; } = new ObservableCollectionEx<Register>();
        #endregion

        #region 设备
        private Device device;


        /// <summary>
        /// 
        /// </summary>
        public Device Device
        {
            get => device;
            set => SetProperty(ref device, value);
        }


        #endregion

        #region 配置
        public ConfigModel Config{ get; }= new ConfigModel();
        #endregion

        #region 公共方法

        public void Open()
        {
            CreateDevice(CreateClient());
            Clear();
            Device.Client.StatusChangedEvent += Client_StatusChangedEvent;
            Device.ProtocolReceivedEvent += Device_ProtocolReceivedEvent; ;
            Device.ProtocolTransmitedEvent += Device_ProtocolTransmitedEvent;
            Device.LogFolder = "Log";
            Device.Open();
        }
        public void Close()
        {
            if (Device != null)
            {
                Device.Close();
                Device.Client.StatusChangedEvent -= Client_StatusChangedEvent;
                Device.ProtocolReceivedEvent -= Device_ProtocolReceivedEvent;
                Device.ProtocolTransmitedEvent -= Device_ProtocolTransmitedEvent;
                Device.Dispose();
                Device = null;

            }
        }
        public void Clear()
        {
            lock (locker)
            {
                MessageCollection.Clear();
            }
            Config.Status.TotalReceived = 0;
            Config.Status.TotalSended = 0;
            Config.Status.Progress = 0;
            Config.Display.LastSend = "";
            Config.Display.LastReceive = "";
        }

        #endregion

        #region 事件处理
        private void Client_StatusChangedEvent(object sender, StatusChangedEventArgs e)
        {
            Config.Status.Message = e.Message;
            MessageBoxControl.Tip($"[{e.CreateTime:HH:mm:ss.fff}] {e.Message}");
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
            lock (locker)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (MessageCollection.Count >= Config.Display.MaxMessageCount)
                    {
                        MessageCollection.RemoveAt(0);
                    }
                    if (Config.Display.DisplayType == 1 && ProtocolType==0)
                    {
                        Config.Display.LastSend = $"[{obj.CreateTime:HH:mm:ss.fff}] [Tx] {obj.FullBytes.ToUTF8()}";
                       
                    }
                    else
                    {
                        Config.Display.LastSend = $"[{obj.CreateTime:HH:mm:ss.fff}] [Tx] {obj.FullBytes.ToHexString()}";
                    }
                    MessageCollection.Add(Config.Display.LastSend);
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
            lock (locker)
            {

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (ProtocolType > 0 && Config.Modbus.StationType == StationType.从站)
                    {
                        MessageCollection.Clear();
                    }
                    if (MessageCollection.Count >= Config.Display.MaxMessageCount)
                    {
                        MessageCollection.RemoveAt(0);
                    }
                    if (Config.Display.DisplayType == 1 && ProtocolType == 0)
                    {
                        Config.Display.LastReceive = $"[{obj.CreateTime:HH:mm:ss.fff}] [Rx] {obj.FullBytes.ToUTF8()}";
                    }
                    else
                    {
                        Config.Display.LastReceive = $"[{obj.CreateTime:HH:mm:ss.fff}] [Rx] {obj.FullBytes.ToHexString()}";
                    }
                    MessageCollection.Add(Config.Display.LastReceive);
                });
            }
            if (ProtocolType == 0 && Config.Receive.IsAutoResponse)
            {
                ProcessAutoResponse(obj);
            }
            if (ProtocolType > 0 && Config.Modbus.StationType == StationType.从站)
            {
                ProcessSlaveRecived(obj);
            }

        }

        private void ProcessAutoResponse(ProtocolBase obj)
        {
            if (Config.Receive.Response.ContainsKey(obj.FullBytes.ToHexString()))
            {
                Device.SendBytes(Config.Receive.Response[obj.FullBytes.ToHexString()]);
            }
        }
        /// <summary>
        /// Slave处理接收
        /// </summary>
        private void ProcessSlaveRecived(ProtocolBase obj)
        {
            // TODO 待完善
            ModbusCommand cmd = ModbusCommand.ReadMoreInputResiters;
            if (obj is ModbusTCP tcp)
            {
                if (Config.Modbus.SlaveID != tcp.SlaveID)
                {
                    return;
                }
                Config.Modbus.TransactionID = tcp.TransactionID;
                cmd = tcp.Command;
            }
            else if (obj is ModbusRTU rtu)
            {
                cmd = rtu.Command;
            }
            #region 获取发送的内容
            List<byte> content = new List<byte>(); // 待发送的内容
            Register register; // 临时变量
            int value;
            switch (cmd)
            {
                case ModbusCommand.ReadMoreCoils:
                    var startAddress = BitConverterHelper.ToUInt16(obj.Content, 0, EndianType.BigEndian);
                    var count = BitConverterHelper.ToUInt16(obj.Content, 2, EndianType.BigEndian);
                    content.Add((byte)count); // 添加长度
                    for (int i = 0; i < count; i++) // 添加寄存器值
                    {
                        register = RegisterCollection.SingleOrDefault(x => x.Address == startAddress + i && x.RegisterType == RegisterType.线圈);
                        // 当前队列中查找不到，则新增一个
                        if (register == null)
                        {
                            register = new Register(startAddress + i, 0, RegisterType.线圈);
                            RegisterCollection.Add(register);
                        }
                        else
                        {
                            content.Add(register.Value == "1" ? (byte)1 : (byte)0);
                        }
                      
                    }
                    break;
                case ModbusCommand.ReadMoreDiscreteInputRegisters:
                     startAddress = BitConverterHelper.ToUInt16(obj.Content, 0, EndianType.BigEndian);
                     count = BitConverterHelper.ToUInt16(obj.Content, 2, EndianType.BigEndian);
                    content.Add((byte)count); // 添加长度
                    for (int i = 0; i < count; i++) // 添加寄存器值
                    {
                        register = RegisterCollection.SingleOrDefault(x => x.Address == startAddress + i && x.RegisterType == RegisterType.离散量输入);
                        // 当前队列中查找不到，则新增一个
                        if (register == null)
                        {
                            register = new Register(startAddress + i, 0, RegisterType.线圈)
                            {
                                WriteType= RegisterWriteType.只读
                            };
                            RegisterCollection.Add(register);
                        }
                        else
                        {
                            content.Add(register.Value == "1" ? (byte)1 : (byte)0);
                        }
                    }
                    break;
                case ModbusCommand.ReadMoreInputResiters:
                    // TODO 必须保持Master和Slave寄存器配置一致，否则会出现错乱
                    startAddress = BitConverterHelper.ToUInt16(obj.Content, 0, EndianType.BigEndian);
                    count = BitConverterHelper.ToUInt16(obj.Content, 2, EndianType.BigEndian);
                    content.Add(((byte)(count * 2))); // 添加长度
                    for (int i = 0; i < count; i++) // 添加寄存器值
                    {
                        register = RegisterCollection.SingleOrDefault(x => x.Address == startAddress + i && x.RegisterType == RegisterType.输入寄存器);
                        if (register != null)
                        {
                            i = i - 1 + register.RegisterCount;
                        }
                        else
                        {
                            register = new Register(startAddress + i, 0, RegisterType.输入寄存器);
                            RegisterCollection.Add(register);
                        }
                        content.AddRange(register.GetBytes());
                    }
                    break;
                case ModbusCommand.ReadMoreHoldingRegisters:
                    // TODO 必须保持Master和Slave寄存器配置一致，否则会出现错乱
                    startAddress = BitConverterHelper.ToUInt16(obj.Content, 0, EndianType.BigEndian);
                    count = BitConverterHelper.ToUInt16(obj.Content, 2, EndianType.BigEndian);
                    content.Add(((byte)(count * 2))); // 添加长度
                    for (int i = 0; i < count; i++) // 添加寄存器值
                    {
                        register = RegisterCollection.SingleOrDefault(x => x.Address == startAddress + i && x.RegisterType == RegisterType.保持寄存器);
                        if (register != null)
                        {
                            i = i - 1 + register.RegisterCount;
                        }
                        else
                        {
                            register = new Register(startAddress + i, 0, RegisterType.保持寄存器);
                            RegisterCollection.Add(register);
                        }
                        content.AddRange(register.GetBytes());
                    }
                    break;
                case ModbusCommand.WriteOneCoil:
                    var registerAddress = BitConverterHelper.ToUInt16(obj.Content, 0, EndianType.BigEndian);
                    var registerValue = BitConverterHelper.ToUInt16(obj.Content, 2, EndianType.BigEndian);
                    content.AddRange(BitConverterHelper.GetBytes((UInt16)registerAddress, EndianType.BigEndian));
                    content.AddRange(BitConverterHelper.GetBytes((UInt16)(registerValue), EndianType.BigEndian));
                    register = RegisterCollection.SingleOrDefault(x => x.Address == registerAddress && x.RegisterType == RegisterType.线圈);
                    if (register == null)
                    {
                        register = new Register(registerAddress, registerValue, RegisterType.线圈);
                        RegisterCollection.Add(register);
                    }
                    else
                    {
                        register.Value = registerValue.ToString();
                    }
                    break;
                case ModbusCommand.WriteMoreCoils:
                    startAddress = BitConverterHelper.ToUInt16(obj.Content, 0, EndianType.BigEndian);
                    count = BitConverterHelper.ToUInt16(obj.Content, 2, EndianType.BigEndian);
                    registerValue = BitConverterHelper.ToUInt16(obj.Content, 5, EndianType.LittleEndian); // 这里用小端，便于移位处理

                    content.AddRange(BitConverterHelper.GetBytes((UInt16)startAddress, EndianType.BigEndian));
                    content.AddRange(BitConverterHelper.GetBytes((UInt16)(count), EndianType.BigEndian));

                    for (int i = 0; i < count; i++)
                    {
                        register = RegisterCollection.SingleOrDefault(x => x.Address == startAddress + i && x.RegisterType == RegisterType.线圈);
                        if (register == null)
                        {
                            register = new Register(startAddress+i, (registerValue>>i)&1, RegisterType.线圈);
                            RegisterCollection.Add(register);
                        }
                        else
                        {
                            register.Value = ((registerValue >> i) & 1).ToString();
                        }
                    }
                    break;
                case ModbusCommand.WriteOneHoldingRegister:
                     registerAddress = BitConverterHelper.ToUInt16(obj.Content, 0, EndianType.BigEndian);
                     registerValue = BitConverterHelper.ToUInt16(obj.Content, 2, EndianType.BigEndian);
                    content.AddRange(BitConverterHelper.GetBytes((UInt16)registerAddress, EndianType.BigEndian));
                    content.AddRange(BitConverterHelper.GetBytes((UInt16)(registerValue), EndianType.BigEndian));
                    register = RegisterCollection.SingleOrDefault(x => x.Address == registerAddress && x.RegisterType== RegisterType.保持寄存器);
                    if (register == null)
                    {
                        register = new Register(registerAddress, registerValue, RegisterType.保持寄存器);
                        RegisterCollection.Add(register);
                    }
                    else
                    {
                        register.Value = registerValue.ToString();
                    }
                    break;
                case ModbusCommand.WriteMoreHoldingRegisters:
                    startAddress = BitConverterHelper.ToUInt16(obj.Content, 0, EndianType.BigEndian);  // 起始地址
                    count = BitConverterHelper.ToUInt16(obj.Content, 2, EndianType.BigEndian); // 寄存器数量
                    content.AddRange(BitConverterHelper.GetBytes((UInt16)startAddress, EndianType.BigEndian));
                    content.AddRange(BitConverterHelper.GetBytes((UInt16)(count), EndianType.BigEndian));
                    // TODO 必须保持Master和Slave寄存器配置一致，否则会出现错乱
                    int index = 0;
                    while (index < count)
                    {
                        register = RegisterCollection.SingleOrDefault(x => x.Address == startAddress + index && x.RegisterType== RegisterType.保持寄存器);
                        if (register == null)
                        {
                            value = BitConverterHelper.ToUInt16(obj.Content, index * 2 + 5, EndianType.BigEndian);
                            RegisterCollection.Add(new Register(startAddress + index, value, RegisterType.保持寄存器));
                            index += 1;
                        }
                        else
                        {
                            register.Value = register.GetValue(obj.Content, index * 2 + 5);
                            index += register.RegisterCount;
                        }
                    }
                    break;
                case ModbusCommand.ReadWriteHoldingRegisters:
                    // 处理读取
                    startAddress = BitConverterHelper.ToUInt16(obj.Content, 0, EndianType.BigEndian);
                    count = BitConverterHelper.ToUInt16(obj.Content, 2, EndianType.BigEndian);
                    content.Add(((byte)(count * 2)));
                    for (int i = 0; i < count; i++)
                    {
                        register = RegisterCollection.SingleOrDefault(x => x.Address == startAddress + i && x.RegisterType== RegisterType.保持寄存器);
                        if (register != null)
                        {
                            content.AddRange(register.GetBytes());
                            i = i - 1 + register.RegisterCount;
                        }
                        else
                        {
                            content.AddRange(new byte[] { 0, 0 });
                        }
                    }
                    // 处理写入
                    startAddress = BitConverterHelper.ToUInt16(obj.Content, 4, EndianType.BigEndian);
                    count = BitConverterHelper.ToUInt16(obj.Content, 6, EndianType.BigEndian);
                    index = 0;
                    while (index < count)
                    {
                        register = RegisterCollection.SingleOrDefault(x => x.Address == startAddress + index && x.RegisterType == RegisterType.保持寄存器);
                        if (register != null)
                        {
                            register.Value = register.GetValue(obj.Content, index * 2 + 9);
                            index += register.RegisterCount;
                        }
                        else
                        {
                            value = BitConverterHelper.ToUInt16(obj.Content, index * 2 + 9, EndianType.BigEndian);
                            RegisterCollection.Add(new Register(startAddress + index, value));
                            index += 1;
                        }
                    }
                    break;

            }
            #endregion

            ProtocolBase response = null;
            if (obj is ModbusRTU)
            {
                response = new ModbusRTU((byte)Config.Modbus.SlaveID, cmd, content.ToArray());
            }
            else if (obj is ModbusTCP)
            {
                response = new ModbusTCP((byte)Config.Modbus.SlaveID, cmd, content.ToArray(), Config.Modbus.TransactionID);
            }
            Device.SendBytes(response.FullBytes);
        }

        #endregion

        #region 私有方法

        private TransferBase CreateClient()
        {
            TransferBase client = null;
            switch (CommunicationType)
            {
                case CommunicationType.RS232:
                    client = new RS232Client(Config.RS232.PortName, Config.RS232.BaudRate, Config.RS232.Parity, Config.RS232.DataBits, Config.RS232.StopBits, Config.RS232.WriteBufferSize, Config.RS232.ReceiveBufferSize);
                    break;
                case CommunicationType.TCPClient:
                    client = new TCPClient(Config.TCPClient.RemoteIP, Config.TCPClient.RemotePort, Config.TCPClient.ReceiveBufferSize);
                    break;
                case CommunicationType.TCPServer:
                    client = new TCPServer(Config.TCPServer.LocalIP, Config.TCPServer.LocalPort, Config.TCPServer.ReceiveBufferSize);
                    break;
                case CommunicationType.UDPClient:
                    client = new UDPClient(Config.UDPClient.LocalIP, Config.UDPClient.LocalPort, Config.UDPClient.BroadcastAddress, Config.UDPClient.BroadcastPort, Config.UDPClient.ReceiveBufferSize);
                    break;
                case CommunicationType.UDPServer:
                    client = new UDPServer(Config.UDPServer.LocalIP, Config.UDPServer.LocalPort, Config.UDPServer.ReceiveBufferSize);
                    break;
                case CommunicationType.VISA:
                    if(string.IsNullOrEmpty(Config.VISA.ResourceName))
                    {
                        throw new Exception("资源名称不能为空");
                    }
                    client = new VISAClient(Config.VISA.ResourceName, false) { TerminationCharacter= Config.VISA.TerminationCharacter,TimeoutMilliseconds=Config.VISA.ReceiveTimeout };
                    break;
            }
            return client;
        }
        private void CreateDevice(TransferBase client)
        {
            if (ProtocolType == 1 || ProtocolType == 2)
            {

                if (Config.Modbus.StationType == StationType.主站)
                {
                    Device = new ModbusMaster(client, (ModbusProtocolType)ProtocolType);
                }
                else
                {
                    Device = new ModbusSlave(client, Config.Modbus.SlaveID, (ModbusSlave.ModbusProtocolType)ProtocolType);
                }
            }
            else
            {
                Device = new Device(client);
            }
          Device.IsHighAccuracyTimer = true;
        }

        #endregion

    }


}

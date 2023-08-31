using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
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
            IPList = new List<string>();
            IPList.AddRange(ip);
            IPList.Add("127.0.0.1");

            // 设置默认值
            if (IPList.Count > 0)
            {
                TCPClient.RemoteIP = TCPServer.LocalIP = UDPClient.LocalIP = UDPServer.LocalIP = IPList[0];
                // 设置广播地址
                var temp = IPList[0].Split('.');
                if (temp.Length == 4)
                {
                    UDPClient.BroadcastAddress = $"{temp[0]}.{temp[1]}.{temp[2]}.255";
                }
            }
            if (PortNames.Length > 0)
            {
                RS232.PortName = PortNames[0];
            }

        }
        public string[] PortNames => System.IO.Ports.SerialPort.GetPortNames();
        public List<string> IPList { get; }

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
                    value == CommunicationType.UDPServer)
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
        public RS232Config RS232 { get; } = new RS232Config();
        public TCPClientConfig TCPClient { get; } = new TCPClientConfig();
        public TCPServerConfig TCPServer { get; } = new TCPServerConfig();
        public UDPClientConfig UDPClient { get; } = new UDPClientConfig();
        public UDPServerConfig UDPServer { get; } = new UDPServerConfig();
        public DisplayConfig Display { get; } = new DisplayConfig();
        public SendConfig Send { get; } = new SendConfig();
        public ReceiveConfig Receive { get; } = new ReceiveConfig();
        public StatusConfig Status { get; } = new StatusConfig();
        public ModbusConfig Modbus { get; } = new ModbusConfig();
        public RegisterConfig Register { get; } = new RegisterConfig();
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
            Status.TotalReceived = 0;
            Status.TotalSended = 0;
            Status.Progress = 0;
        }

        #endregion

        #region 事件处理
        private void Client_StatusChangedEvent(object sender, StatusChangedEventArgs e)
        {
            Status.Message= e.Message;
            MessageBoxControl.Tip($"[{e.CreateTime:HH:mm:ss.fff}] {e.Message}");
        }
        /// <summary>
        /// 发送事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="obj"></param>
        private void Device_ProtocolTransmitedEvent(object sender, ProtocolBase obj)
        {
            Status.TotalSended += obj.FullBytes.Length;
            if (!Display.EnableDisplay)
                return;
            lock (locker)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (MessageCollection.Count >= Display.MaxMessageCount)
                    {
                        MessageCollection.RemoveAt(0);
                    }
                    if (Display.DisplayType == 0)
                    {
                        MessageCollection.Add($"[{obj.CreateTime:HH:mm:ss.fff}] [Tx] {obj.FullBytes.ToHexString()}");
                    }
                    else
                    {
                        MessageCollection.Add($"[{obj.CreateTime:HH:mm:ss.fff}] [Tx] {obj.FullBytes.ToUTF8()}");
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
            Status.TotalReceived += obj.FullBytes.Length;
            if (!Display.EnableDisplay)
                return;
            lock (locker)
            {

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (ProtocolType > 0 && Modbus.StationType == StationType.从站)
                    {
                        MessageCollection.Clear();
                    }
                    if (MessageCollection.Count >= Display.MaxMessageCount)
                    {
                        MessageCollection.RemoveAt(0);
                    }
                    if (Display.DisplayType == 0)
                    {
                        MessageCollection.Add($"[{obj.CreateTime:HH:mm:ss.fff}] [Rx] {obj.FullBytes.ToHexString()}");
                    }
                    else
                    {
                        MessageCollection.Add($"[{obj.CreateTime:HH:mm:ss.fff}] [Rx] {obj.FullBytes.ToUTF8()}");
                    }
                });
            }
            if (ProtocolType == 0 && Receive.IsAutoResponse)
            {
                ProcessAutoResponse(obj);
            }
            if (ProtocolType > 0 && Modbus.StationType == StationType.从站)
            {
                ProcessSlaveRecived(obj);
            }

        }

        private void ProcessAutoResponse(ProtocolBase obj)
        {
            if (Receive.Response.ContainsKey(obj.FullBytes.ToHexString()))
            {
                Device.SendBytes(Receive.Response[obj.FullBytes.ToHexString()]);
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
                if (Modbus.SlaveID != tcp.SlaveID)
                {
                    return;
                }
                Modbus.TransactionID = tcp.TransactionID;
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
                case ModbusCommand.ReadMoreDiscreteInputRegisters:
                    var startAddress = BitConverterHelper.ToUInt16(obj.Content, 0, EndianType.BigEndian);
                    var count = BitConverterHelper.ToUInt16(obj.Content, 2, EndianType.BigEndian);
                    content.Add((byte)count ); // 添加长度
                    for (int i = 0; i < count; i++) // 添加寄存器值
                    {
                        register = RegisterCollection.SingleOrDefault(x => x.Address == startAddress + i);
                        if (register != null)
                        {
                            content.Add(register.Value=="1"?(byte)1: (byte)0);
                            i = i - 1 + register.RegisterCount;
                        }
                        else
                        {
                            content.Add((byte)0);
                        }
                    }
                    break;
                case ModbusCommand.ReadMoreInputResiters:
                case ModbusCommand.ReadMoreHoldingRegisters:
                     startAddress = BitConverterHelper.ToUInt16(obj.Content, 0, EndianType.BigEndian);
                     count = BitConverterHelper.ToUInt16(obj.Content, 2, EndianType.BigEndian);
                    content.Add(((byte)(count * 2))); // 添加长度
                    for (int i = 0; i < count; i++) // 添加寄存器值
                    {
                        register = RegisterCollection.SingleOrDefault(x => x.Address == startAddress + i);
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
                    break;
                case ModbusCommand.WriteOneCoil:
                case ModbusCommand.WriteOneHoldingRegister:
                    var registerAddress = BitConverterHelper.ToUInt16(obj.Content, 0, EndianType.BigEndian);
                    var registerValue = BitConverterHelper.ToUInt16(obj.Content, 2, EndianType.BigEndian);
                    content.AddRange(BitConverterHelper.GetBytes((UInt16)registerAddress, EndianType.BigEndian));
                    content.AddRange(BitConverterHelper.GetBytes((UInt16)(registerValue), EndianType.BigEndian));
                    register = RegisterCollection.SingleOrDefault(x => x.Address == registerAddress);
                    if (register != null)
                    {
                        register.Value = register.GetValue(obj.Content, 2);
                    }
                    else
                    {
                        value = BitConverterHelper.ToUInt16(obj.Content, 2, EndianType.BigEndian);
                        RegisterCollection.Add(new Register(registerAddress, value));
                    }
                    break;
                case ModbusCommand.WriteMoreHoldingRegisters:
                    startAddress = BitConverterHelper.ToUInt16(obj.Content, 0, EndianType.BigEndian);  // 起始地址
                    count = BitConverterHelper.ToUInt16(obj.Content, 2, EndianType.BigEndian); // 寄存器数量
                    content.AddRange(BitConverterHelper.GetBytes((UInt16)startAddress, EndianType.BigEndian));
                    content.AddRange(BitConverterHelper.GetBytes((UInt16)(count), EndianType.BigEndian));

                    int index = 0;
                    while (index < count)
                    {
                        register = RegisterCollection.SingleOrDefault(x => x.Address == startAddress + index);
                        if (register != null)
                        {
                            register.Value = register.GetValue(obj.Content, index * 2 + 5);
                            index += register.RegisterCount;
                        }
                        else
                        {
                            value = BitConverterHelper.ToUInt16(obj.Content, index * 2 + 5, EndianType.BigEndian);
                            RegisterCollection.Add(new Register(startAddress + index, value));
                            index += 1;
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
                        register = RegisterCollection.SingleOrDefault(x => x.Address == startAddress + i);
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
                        register = RegisterCollection.SingleOrDefault(x => x.Address == startAddress + index);
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
                response = new ModbusRTU((byte)Modbus.SlaveID, cmd, content.ToArray());
            }
            else if (obj is ModbusTCP)
            {
                response = new ModbusTCP((byte)Modbus.SlaveID, cmd, content.ToArray(), Modbus.TransactionID);
            }
            Device.SendBytes(response.FullBytes);
        }

        #endregion

        #region 私有方法

        private TransferBase CreateClient()
        {
            TransferBase client =null;
            switch (CommunicationType)
            {
                case CommunicationType.RS232:
                    client = new RS232Client(RS232.PortName, RS232.BaudRate, RS232.Parity, RS232.DataBits, RS232.StopBits, RS232.WriteBufferSize, RS232.ReceiveBufferSize);
                    break;
                case CommunicationType.TCPClient:
                    client = new TCPClient(TCPClient.RemoteIP, TCPClient.RemotePort, TCPClient.ReceiveBufferSize);
                    break;
                case CommunicationType.TCPServer:
                    client = new TCPServer(TCPServer.LocalIP, TCPServer.LocalPort, TCPServer.ReceiveBufferSize);
                    break;
                case CommunicationType.UDPClient:
                    client = new UDPClient(UDPClient.LocalIP, UDPClient.LocalPort, UDPClient.BroadcastAddress, UDPClient.BroadcastPort, UDPClient.ReceiveBufferSize);
                    break;
                case CommunicationType.UDPServer:
                    client = new UDPServer(UDPServer.LocalIP, UDPServer.LocalPort, UDPServer.ReceiveBufferSize);
                    break;
            }
            return client;
        }
        private void CreateDevice(TransferBase client)
        {
            if (ProtocolType == 1 || ProtocolType == 2)
            {

                if (Modbus.StationType == StationType.主站)
                {
                    Device= new ModbusMaster(client, (ModbusProtocolType)ProtocolType);
                }
                else
                {
                    Device= new ModbusSlave(client, Modbus.SlaveID, (ModbusSlave.ModbusProtocolType)ProtocolType);
                }
            }
            else
            {
                Device=new Device(client);
            }
        }
 
        #endregion

    }

    class RS232Config : ObservableObject
    {
        private string portName;
        private int baudRate = 9600;
        private int parity = 0;
        private int dataBits = 8;
        private int stopBits = 1;
        private int writeBufferSize = 4096;
        private int receiveBufferSize = 4096;
        /// <summary>
        /// 
        /// </summary>
        public string PortName
        {
            get => portName;
            set => SetProperty(ref portName, value);
        }
        /// <summary>
        /// 波特率
        /// </summary>
        public int BaudRate
        {
            get => baudRate;
            set => SetProperty(ref baudRate, value);
        }
        /// <summary>
        /// 奇偶校验位，0 None；1 Odd；2 Even；4 Mark；5 Space
        /// </summary>
        public int Parity
        {
            get => parity;
            set => SetProperty(ref parity, value);
        }
        /// <summary>
        /// 数据位，取值范围[5,8]
        /// </summary>
        public int DataBits
        {
            get => dataBits;
            set => SetProperty(ref dataBits, value);
        }
        /// <summary>
        /// 停止位，0 HexBare；1 One；2 Two；3 OnePointFive,
        /// </summary>
        public int StopBits
        {
            get => stopBits;
            set => SetProperty(ref stopBits, value);
        }

        /// <summary>
        /// 写缓存
        /// </summary>
        public int WriteBufferSize
        {
            get => writeBufferSize;
            set => SetProperty(ref writeBufferSize, value);
        }

        /// <summary>
        /// 读缓存
        /// </summary>
        public int ReceiveBufferSize
        {
            get => receiveBufferSize;
            set => SetProperty(ref receiveBufferSize, value);
        }
    }
    class TCPClientConfig : ObservableObject
    {
        private string remoteIP;
        private int remotePort = 502;
        private int receiveBufferSize = 4096;
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
        /// <summary>
        /// 读缓存
        /// </summary>
        public int ReceiveBufferSize
        {
            get => receiveBufferSize;
            set => SetProperty(ref receiveBufferSize, value);
        }
    }
    class TCPServerConfig : ObservableObject
    {
        private string localIP;
        private int localPort = 502;
        private int receiveBufferSize = 4096;
        /// <summary>
        /// 
        /// </summary>
        public string LocalIP
        {
            get => localIP;
            set => SetProperty(ref localIP, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public int LocalPort
        {
            get => localPort;
            set => SetProperty(ref localPort, value);
        }
        /// <summary>
        /// 读缓存
        /// </summary>
        public int ReceiveBufferSize
        {
            get => receiveBufferSize;
            set => SetProperty(ref receiveBufferSize, value);
        }
    }
    class UDPClientConfig : ObservableObject
    {
        private string localIP;
        private int localPort = 502;

        private string broadcastAddress;
        private int broadcastPort;
        private int receiveBufferSize = 4096;
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
        /// <summary>
        /// 
        /// </summary>
        public string LocalIP
        {
            get => localIP;
            set => SetProperty(ref localIP, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public int LocalPort
        {
            get => localPort;
            set => SetProperty(ref localPort, value);
        }
        /// <summary>
        /// 读缓存
        /// </summary>
        public int ReceiveBufferSize
        {
            get => receiveBufferSize;
            set => SetProperty(ref receiveBufferSize, value);
        }
    }
    class UDPServerConfig : TCPServerConfig
    {

    }
    class DisplayConfig : ObservableObject
    {

        private int maxMessageCount = 18;

        /// <summary>
        /// 最大显示的条目数量
        /// </summary>
        public int MaxMessageCount
        {
            get => maxMessageCount;
            set => SetProperty(ref maxMessageCount, value);
        }

        private int displayType=1;

        /// <summary>
        /// 显示类型，0 Hex；1 UTF-8
        /// </summary>
        public int DisplayType
        {
            get => displayType;
            set => SetProperty(ref displayType, value);
        }

        private bool enableDisplay = true;

        /// <summary>
        /// 启用窗口显示
        /// </summary>
        public bool EnableDisplay
        {
            get => enableDisplay;
            set => SetProperty(ref enableDisplay, value);
        }

    }
    class SendConfig : ObservableObject
    {
        private bool isCyclic;
        private int cyclicInterval = 1000;
        private string sendText;
        private ProtocolType protocolType= ProtocolType.UTF8;
        private int responseTimeout = 200;
        /// <summary>
        /// 是否循环
        /// </summary>
        public bool IsCyclic
        {
            get => isCyclic;
            set => SetProperty(ref isCyclic, value);
        }

        /// <summary>
        /// 循环间隔时间，单位ms
        /// </summary>
        public int CyclicInterval
        {
            get => cyclicInterval;
            set => SetProperty(ref cyclicInterval, value);
        }

        /// <summary>
        /// 发送栏文本
        /// </summary>
        public string SendText
        {
            get => sendText;
            set => SetProperty(ref sendText, value);
        }

        /// <summary>
        /// 校验类型
        /// </summary>
        public ProtocolType ProtocolType
        {
            get => protocolType;
            set => SetProperty(ref protocolType, value);
        }


        private bool isSendFile;

        /// <summary>
        /// 如果发送文件，SendText显示发送文件的路径
        /// </summary>
        public bool IsSendFile
        {
            get => isSendFile;
            set => SetProperty(ref isSendFile, value);
        }
        /// <summary>
        /// 应答超时时间，单位ms
        /// </summary>
        public int ResponseTimeout
        {
            get => responseTimeout;
            set => SetProperty(ref responseTimeout, value);
        }
    }
    class ReceiveConfig : ObservableObject
    {
        private bool isAutoResponse;

        /// <summary>
        /// 
        /// </summary>
        public bool IsAutoResponse { get => isAutoResponse; set => SetProperty(ref isAutoResponse, value); }

        /// <summary>
        /// 应答对象字典，Key是接收，Value是应答
        /// </summary>
        public Dictionary<string, byte[]> Response { get; set; } = new Dictionary<string, byte[]>();
        private ProtocolType protocolType;
        /// <summary>
        /// 校验类型
        /// </summary>
        public ProtocolType ProtocolType
        {
            get => protocolType;
            set => SetProperty(ref protocolType, value);
        }
    }
    class StatusConfig : ObservableObject
    {
        private int totalReceived;

        /// <summary>
        /// 
        /// </summary>
        public int TotalReceived
        {
            get => totalReceived;
            set => SetProperty(ref totalReceived, value);
        }

        private int totalSended;

        /// <summary>
        /// 
        /// </summary>
        public int TotalSended
        {
            get => totalSended;
            set => SetProperty(ref totalSended, value);
        }

        private double progress;

        /// <summary>
        /// 
        /// </summary>
        public double Progress
        {
            get => progress;
            set => SetProperty(ref progress, value);
        }

        private string message;

        /// <summary>
        /// 
        /// </summary>
        public string Message { get => message; set => SetProperty(ref message, value); }

        private Visibility progressBarVisibility= Visibility.Collapsed;

        /// <summary>
        /// 
        /// </summary>
        public Visibility ProgressBarVisibility { get => progressBarVisibility; set => SetProperty(ref progressBarVisibility, value); }


    }
    class ModbusConfig : ObservableObject
    {
        private StationType stationType;
        private byte slaveID = 1;
        private UInt16 transactionID;
   
        /// <summary>
        /// 工作站类型
        /// </summary>
        public StationType StationType
        {
            get => stationType;
            set => SetProperty(ref stationType, value);
        }


        /// <summary>
        /// 从站节点编号
        /// </summary>
        public byte SlaveID
        {
            get => slaveID;
            set => SetProperty(ref slaveID, value);
        }

        /// <summary>
        /// 事务标识符
        /// </summary>
        public UInt16 TransactionID
        {
            get => transactionID;
            set => SetProperty(ref transactionID, value);
        }

        private bool isSupportMultiWriteCommand=true;

        /// <summary>
        /// 是否支持写多个寄存器或线圈指令，例如0x10、0x0F
        /// </summary>
        public bool IsSupportMultiWriteCommand { get => isSupportMultiWriteCommand; set => SetProperty(ref isSupportMultiWriteCommand, value); }

    }

    class RegisterConfig:ObservableObject
    {
        private int startAddress;
        private int registerCount = 10;
        /// <summary>
        /// 起始地址
        /// </summary>
        public int StartAddress
        {
            get => startAddress;
            set => SetProperty(ref startAddress, value);
        }
        /// <summary>
        /// 寄存器个数
        /// </summary>
        public int RegisterCount { get => registerCount; set => SetProperty(ref registerCount, value); }

    }
}

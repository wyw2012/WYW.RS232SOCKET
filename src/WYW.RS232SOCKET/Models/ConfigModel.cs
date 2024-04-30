using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WYW.RS232SOCKET.Models
{
    class ConfigModel
    {
        public RS232Config RS232 { get; } = new RS232Config();
        public TCPClientConfig TCPClient { get; } = new TCPClientConfig();
        public TCPServerConfig TCPServer { get; } = new TCPServerConfig();
        public UDPClientConfig UDPClient { get; } = new UDPClientConfig();
        public UDPServerConfig UDPServer { get; } = new UDPServerConfig();
        public VISAConfig VISA { get; } = new VISAConfig();
        public DisplayConfig Display { get; } = new DisplayConfig();
        public SendConfig Send { get; } = new SendConfig();
        public ReceiveConfig Receive { get; } = new ReceiveConfig();
        public StatusConfig Status { get; } = new StatusConfig();
        public ModbusConfig Modbus { get; } = new ModbusConfig();
        public RegisterConfig Register { get; } = new RegisterConfig();
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
        private int localPort = 8899;

        private string broadcastAddress="255.255.255.255";
        private int broadcastPort=502;
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
    class VISAConfig : ObservableObject
    {
        private string resourceName;

        /// <summary>
        /// 
        /// </summary>
        public string ResourceName { get => resourceName; set => SetProperty(ref resourceName, value); }


        private byte terminationCharacter=0x0A;

        /// <summary>
        /// 终止符
        /// </summary>
        public byte TerminationCharacter { get => terminationCharacter; set => SetProperty(ref terminationCharacter, value); }


        private int receiveTimeout=10;

        /// <summary>
        /// 
        /// </summary>
        public int ReceiveTimeout { get => receiveTimeout; set => SetProperty(ref receiveTimeout, value); }


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

        private int displayType = 1;

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

        private string lastSend;

        /// <summary>
        /// 
        /// </summary>
        public string LastSend { get => lastSend; set => SetProperty(ref lastSend, value); }

        private string lastReceive;

        /// <summary>
        /// 
        /// </summary>
        public string LastReceive { get => lastReceive; set => SetProperty(ref lastReceive, value); }


    }
    class SendConfig : ObservableObject
    {
        private bool isCyclic;
        private int cyclicInterval = 1000;
        private string sendText;
        private TextProtocolType protocolType = TextProtocolType.UTF8;
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
        public TextProtocolType ProtocolType
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
        private TextProtocolType protocolType;
        /// <summary>
        /// 校验类型
        /// </summary>
        public TextProtocolType ProtocolType
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

        private Visibility progressBarVisibility = Visibility.Collapsed;

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

        private bool isSupportMultiWriteCommand = true;

        /// <summary>
        /// 是否支持写多个寄存器或线圈指令，例如0x10、0x0F
        /// </summary>
        public bool IsSupportMultiWriteCommand { get => isSupportMultiWriteCommand; set => SetProperty(ref isSupportMultiWriteCommand, value); }

    }
    class RegisterConfig : ObservableObject
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

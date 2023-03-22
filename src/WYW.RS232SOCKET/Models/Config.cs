using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;

namespace WYW.RS232SOCKET.Models
{
    internal class Config : ObservableObject
    {
        public Config()
        {
            string hostName = Dns.GetHostName();
            var ip = Dns.GetHostAddresses(hostName).Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).Select(x => x.ToString()).ToArray();
            IPList = new List<string>();
            IPList.AddRange(ip);
            IPList.Add("127.0.0.1");

            // 设置默认值
            if (IPList.Count > 0)
            {
                TCPClient.RemoteIP = TCPServer.LocalIP = UDPClient.LocalIP= UDPServer.LocalIP = IPList[0];
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

        public RS232Config RS232 { get; } = new RS232Config();
        public TCPClientConfig TCPClient { get; } = new TCPClientConfig();
        public TCPServerConfig TCPServer { get; } = new TCPServerConfig();
        public UDPClientConfig UDPClient { get; } = new UDPClientConfig();
        public UDPServerConfig UDPServer { get; } = new UDPServerConfig();
        public DisplayConfig Display { get; }  =new DisplayConfig();
        public SendConfig Send { get; } = new SendConfig();
        public StatusConfig Status { get; } = new StatusConfig();
        public ModbusConfig Modbus { get; } = new ModbusConfig();
        public CommonConfig Common { get; } = new CommonConfig();

    }
    class CommonConfig:ObservableObject
    {
        private int writeBufferSize = 4096;

        /// <summary>
        /// 
        /// </summary>
        public int WriteBufferSize
        {
            get => writeBufferSize;
            set => SetProperty(ref writeBufferSize, value);
        }

        private int receiveBufferSize=4096;

        /// <summary>
        /// 
        /// </summary>
        public int ReceiveBufferSize
        {
            get => receiveBufferSize;
            set => SetProperty(ref receiveBufferSize, value);
        }

    }
    class RS232Config : ObservableObject
    {
        private string portName;
        private int baudRate = 9600;
        private int parity = 0;
        private int dataBits = 8;
        private int stopBits = 1;

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
    }

    class TCPClientConfig : ObservableObject
    {
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

    }

    class TCPServerConfig : ObservableObject
    {
        private string localIP;
        private int localPort = 502;
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

    }
    class UDPClientConfig : ObservableObject
    {
        private string localIP;
        private int localPort = 502;
        
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
    }
    class UDPServerConfig : TCPServerConfig
    {

    }
    class DisplayConfig : ObservableObject
    {

        private int maxItemCount=200;

        /// <summary>
        /// 最大显示的条目数量
        /// </summary>
        public int MaxItemCount
        {
            get => maxItemCount;
            set => SetProperty(ref maxItemCount, value);
        }

        private int displayType;

        /// <summary>
        /// 显示类型，0 Hex；1 UTF-8
        /// </summary>
        public int DisplayType
        {
            get => displayType;
            set => SetProperty(ref displayType, value);
        }
        public ObservableCollection<string> DisplayItems { get; } = new ObservableCollection<string>();

    }
    class SendConfig : ObservableObject
    {
        private bool isCyclic;
        private int cyclicInterval=1000;
        private string sendText;
        private ProtocolType protocolType;

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

        private bool isSending;

        /// <summary>
        /// 正在发送
        /// </summary>
        public bool IsSending
        {
            get => isSending;
            set => SetProperty(ref isSending, value);
        }

    }

    class ModbusConfig:ObservableObject
    {
        private ModbusType modbusType;
        private byte slaveID=1;
        private UInt16 transactionID;
        private int responseTimeout = 200;
        /// <summary>
        /// Modbus类型
        /// </summary>
        public ModbusType ModbusType
        {
            get => modbusType;
            set => SetProperty(ref modbusType, value);
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



        /// <summary>
        /// 应答超时时间，单位ms
        /// </summary>
        public int ResponseTimeout
        {
            get => responseTimeout;
            set => SetProperty(ref responseTimeout, value);
        }

    }
}

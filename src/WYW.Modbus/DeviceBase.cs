using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WYW.Modbus.Clients;
using WYW.Modbus.Protocols;

namespace WYW.Modbus
{

    public abstract class DeviceBase : ObservableObject
    {
        protected bool isKeepHeartbeatTheadAlive = true;
        protected DateTime lastReceiveTime = DateTime.MinValue;

        public DeviceBase()
        {
            
        }
        public DeviceBase(ClientBase client, ProtocolType protocol)
        {
            Client = client;
            Protocol = protocol;
        }
        #region 属性
        public ClientBase Client { get; set; }
        public ProtocolType Protocol { get; set; }
        /// <summary>
        /// 心跳配置
        /// </summary>
        public Heartbeat Heartbeat { get; } = new Heartbeat();

        private bool isConnected;
        private int? deviceID;
        private bool logEnabled;
        private bool isPrintDebugLog;
        /// <summary>
        /// 
        /// </summary>
        public bool IsConnected
        {
            get => isConnected;
            set
            {
                if (isConnected != value)
                {
                    isConnected = value;
                    OnPropertyChanged(nameof(IsConnected));
                    if (value)
                    {
                        Task.Run(OnWhenConnected);
                    }
                }
            }
        }


        /// <summary>
        /// 设备编号，用于多设备之间的区分
        /// </summary>
        public int? DeviceID
        {
            get => deviceID;
            set
            {
                SetProperty(ref deviceID, value);
            }
        }
        /// <summary>
        /// 是否启用日志
        /// </summary>
        public bool LogEnabled
        {
            get { return logEnabled; }
            set { logEnabled = value; }
        }
        private string logFolder = "Log\\Device";

        /// <summary>
        /// 日志文件夹，默认值为“Log\\Device”
        /// </summary>
        public string LogFolder { get => logFolder; set => SetProperty(ref logFolder, value); }
        /// <summary>
        /// 
        /// </summary>
        public bool IsPrintDebugInfo { get => isPrintDebugLog; set => SetProperty(ref isPrintDebugLog, value); }

        private bool isDebugMode;

        /// <summary>
        /// 是否是调试模式
        /// </summary>
        public bool IsDebugMode { get => isDebugMode; set => SetProperty(ref isDebugMode, value); }


        #endregion

        #region 公共方法
        public virtual void Open()
        {
            if (!IsDebugMode)
            {
                Client?.Open();
                Task.Run(StartHeartbeat);
            }
            else
            {
                IsConnected = true;
            }
          

        }
        public virtual void Close()
        {
            Client?.Close();
        }
        #endregion

        #region 虚方法


        internal abstract ExecutionResult SendProtocol(ProtocolBase obj, bool isNeedResponse, int maxSendCount, int responseTimeout);
        /// <summary>
        /// 建立连接时触发
        /// </summary>
        protected virtual void OnWhenConnected()
        {

        }
        /// <summary>
        /// 心跳触发方法，根据Result.IsSuccess判断心跳是否成功
        /// </summary>
        /// <param name="result"></param>
        protected virtual void OnHeartbeatTriggered(ExecutionResult result)
        {

        }
        #endregion

        #region 私有方法
        private void StartHeartbeat()
        {
            isKeepHeartbeatTheadAlive = true;
            var heartbeatContent = Heartbeat.Content;
            if (heartbeatContent == null)
            {
                IsConnected = Client.IsEstablished;
                return;
            }
            while (isKeepHeartbeatTheadAlive)
            {
                Thread.Sleep(200);
                if (!Heartbeat.IsEnabled)
                {
                    IsConnected = Client.IsEstablished;
                    continue;
                }
                if (!Client.IsEstablished)
                {
                    IsConnected = false;
                    continue;
                }
                if ((DateTime.Now - lastReceiveTime).TotalSeconds >= Heartbeat.IntervalSeconds)
                {
                    heartbeatContent.CreateTime = DateTime.Now;
                    var result = SendProtocol(heartbeatContent, true, Heartbeat.MaxRetryCount, Heartbeat.Timeout);
                    IsConnected = result.IsSuccess;
                    OnHeartbeatTriggered(result);
                }
            }
        }

        protected string GetLogFolder()
        {
            if (DeviceID != null)
            {
                return Path.Combine(LogFolder, DeviceID.ToString());
            }
            else
            {
                return LogFolder;
            }
        }
        #endregion
    }
}

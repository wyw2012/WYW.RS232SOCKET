using Serilog.Events;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WYW.Modbus.Clients;
using WYW.Modbus.Protocols;
using Serilog.Core;

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
            set
            {
                logEnabled = value;
            }
        }
        private string logFolder = "Log\\Device";

        /// <summary>
        /// 日志文件夹，默认值为“Log\\Device”
        /// </summary>
        public string LogFolder
        {
            get => logFolder;
            set
            {
                SetProperty(ref logFolder, value);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public bool IsPrintDebugInfo { get => isPrintDebugLog; set => SetProperty(ref isPrintDebugLog, value); }

        private bool isDebugMode;

        /// <summary>
        /// 是否是调试模式
        /// </summary>
        public bool IsDebugMode { get => isDebugMode; set => SetProperty(ref isDebugMode, value); }

        /// <summary>
        ///最后一次发送指令的内容
        /// </summary>
        public string[] LastCommandText { get; protected set; } = new string[2];

        private Logger logger;

        public Logger Logger
        {
            get
            {
                if (logger == null)
                {
                    logger = GetLogger();
                }
                return logger;
            }
        }
        /// <summary>
        /// 是否是高精度时钟模式
        /// </summary>
        public bool IsHighAccuracyTimer { get; set; }
        /// <summary>
        /// 日志保留的时长
        /// </summary>
        public int LogKeepDays { get; set; } = 90;
        #endregion

        #region 公共方法
        public virtual void Open()
        {
            if (LogEnabled && Client != null)
            {
                Client.Logger = Logger;
            }
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
            logger?.Dispose();
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
            //var heartbeatContent = Heartbeat.Content;
            //if (heartbeatContent == null)
            //{
            //    IsConnected = Client.IsEstablished;
            //    return;
            //}
            while (isKeepHeartbeatTheadAlive)
            {
                Thread.Sleep(200);
                if (!Heartbeat.IsEnabled || Heartbeat.Content == null)
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
                    Heartbeat.Content.CreateTime = DateTime.Now;
                    var result = SendProtocol(Heartbeat.Content, true, Heartbeat.MaxRetryCount, Heartbeat.Timeout);
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

        private Logger GetLogger()
        {
            // 初始化日志库
            // 1 按天存储
            // 2 每个文件限制10M
            // 3 最多保存60天的日志
            string logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", GetLogFolder());
            TimeSpan keepLogTime = new TimeSpan(LogKeepDays, 0, 0, 0);
            string serilogOutputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Message:lj}{NewLine}{Exception}";
            return new LoggerConfiguration()
                                     .MinimumLevel.Debug()
                                     .WriteTo.Logger(lg => lg.Filter.ByIncludingOnly(p => p.Level == LogEventLevel.Debug).WriteTo.File($"{logFolder}\\.txt", retainedFileCountLimit: null, retainedFileTimeLimit: keepLogTime, rollOnFileSizeLimit: true, fileSizeLimitBytes: 10 * 1024 * 1024, rollingInterval: RollingInterval.Day, outputTemplate: serilogOutputTemplate))
                                     .WriteTo.Logger(lg => lg.Filter.ByIncludingOnly(p => p.Level == LogEventLevel.Information).WriteTo.File($"{logFolder}\\Information\\.txt", retainedFileCountLimit: null, retainedFileTimeLimit: keepLogTime, rollOnFileSizeLimit: true, fileSizeLimitBytes: 10 * 1024 * 1024, rollingInterval: RollingInterval.Day, outputTemplate: serilogOutputTemplate))
                                     .WriteTo.Logger(lg => lg.Filter.ByIncludingOnly(p => p.Level == LogEventLevel.Warning).WriteTo.File($"{logFolder}\\Warning\\.txt", retainedFileCountLimit: null, retainedFileTimeLimit: keepLogTime, rollOnFileSizeLimit: true, fileSizeLimitBytes: 10 * 1024 * 1024, rollingInterval: RollingInterval.Day, outputTemplate: serilogOutputTemplate))
                                     .WriteTo.Logger(lg => lg.Filter.ByIncludingOnly(p => p.Level == LogEventLevel.Error).WriteTo.File($"{logFolder}\\Error\\.txt", retainedFileCountLimit: null, retainedFileTimeLimit: keepLogTime, rollOnFileSizeLimit: true, fileSizeLimitBytes: 10 * 1024 * 1024, rollingInterval: RollingInterval.Day, outputTemplate: serilogOutputTemplate))
                                     .WriteTo.Logger(lg => lg.Filter.ByIncludingOnly(p => p.Level == LogEventLevel.Fatal).WriteTo.File($"{logFolder}\\Fatal\\.txt", retainedFileCountLimit: null, retainedFileTimeLimit: keepLogTime, rollOnFileSizeLimit: true, fileSizeLimitBytes: 10 * 1024 * 1024, rollingInterval: RollingInterval.Day, outputTemplate: serilogOutputTemplate))
                                     .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                                     .Enrich.FromLogContext()
                                     .CreateLogger();
        }
        #endregion
    }
}

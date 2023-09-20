using WYW.Communication.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WYW.Communication.TransferLayer;
using System.Collections.Concurrent;
using DataReceivedEventArgs = WYW.Communication.TransferLayer.DataReceivedEventArgs;
using WYW.Communication.ApplicationlLayer;
using System.Threading.Tasks;
using Ivi.Visa;

namespace WYW.Communication
{
    /// <summary>
    /// 使用协议的设备
    /// </summary>
    public class Device : ObservableObject, IDisposable
    {
        private bool disposed = false;
        private bool isOpened = false;
        private static object ReadLock = new object();
        private List<byte> receiveBuffer = new List<byte>();
        private DateTime lastReceiveTime = DateTime.Now; // 最后一次接收数据的时间
        private Thread sendThread = null, heartbeatThread = null;
        private bool isKeepSendThreadAlive = true, isKeepHeartbeatTheadAlive = true;
        private ConcurrentQueue<ProtocolTransmitModel> SendQueue = new ConcurrentQueue<ProtocolTransmitModel>();
        private ProtocolTransmitModel lastSendNeedResponse = null;


        #region 构造函数
        public Device(TransferBase client)
        {
            Client = client;
            Client.PropertyChanged += Client_PropertyChanged;
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="Client">传输媒介</param>
        /// <param name="protocol">应用层协议类型</param>
        public Device(TransferBase Client, ProtocolType protocol) : this(Client)
        {
            ProtocolType = protocol;
        }
        #endregion

        #region  属性
        private DeviceStatus deviceStatus = DeviceStatus.UnConnected;
        private bool isConnected;
        private bool logEnabled;
        /// <summary>
        /// 是否建立连接，如果有心跳，则利用心跳判断，如果无心跳，则以通讯建立连接为标志
        /// </summary>
        public bool IsConnected
        {
            get => isConnected;
            set
            {
                SetProperty(ref isConnected, value);
                if (!value)
                {
                    DeviceStatus = DeviceStatus.UnConnected;
                }
                else
                {
                    if (DeviceStatus == DeviceStatus.UnConnected)
                    {
                        DeviceStatus = DeviceStatus.Standy;
                    }
                }
            }
        }
        /// <summary>
        /// 设备编号，用于多设备之间的区分
        /// </summary>
        public int DeviceID { get; set; }

        public TransferBase Client { get; }
        /// <summary>
        /// 设备状态
        /// </summary>
        public DeviceStatus DeviceStatus { get => deviceStatus; protected set => SetProperty(ref deviceStatus, value); }

        /// <summary>
        /// 心跳配置
        /// </summary>
        public Heartbeat Heartbeat { get; } = new Heartbeat();
        /// <summary>
        /// 是否启用日志
        /// </summary>
        public bool LogEnabled
        {
            get { return logEnabled; }
            set { logEnabled = value; }
        }
        /// <summary>
        /// 是否为调试模式，调试模式下发送返回成功，但Response值为null，需要调用者继续处理
        /// </summary>
        public bool IsDebugModel { get; set; }
        /// <summary>
        /// 日志文件夹，默认值为“Log\\Device”
        /// </summary>
        public string LogFolder { get; set; } = "Log\\Device";
        /// <summary>
        /// 应用层协议类型
        /// </summary>
        public ProtocolType ProtocolType { get; set; }
        /// <summary>
        /// 最后一次接收到数据时间
        /// </summary>
        public DateTime LastReceiveTime { get; protected set; } = DateTime.MinValue;
        #endregion

        #region 事件
        public delegate void ProtocolReceivedEventHandler(object sender, ProtocolBase obj);
        public delegate void ProtocolTransmitedEventHandler(object sender, ProtocolBase obj);
        public delegate void HeartbeatTriggeredEventHandler(object sender, ExecutionResult result);

        /// <summary>
        /// 接收消息事件
        /// </summary>
        public event ProtocolReceivedEventHandler ProtocolReceivedEvent;
        /// <summary>
        /// 发送消息事件
        /// </summary>
        public event ProtocolTransmitedEventHandler ProtocolTransmitedEvent;
        /// <summary>
        /// 心跳触发事件，通过IsSuccess判断心跳是否正常
        /// </summary>
        public event HeartbeatTriggeredEventHandler HeartbeatTriggeredEvent;
        #endregion

        #region  公共方法
        /// <summary>
        /// 打开设备
        /// </summary>
        public void Open()
        {
            if (IsDebugModel)
            {
                IsConnected = true;
                DeviceStatus = DeviceStatus.Standy;
                return;
            }

            if (isOpened)
            {
                return;
            }
            if (Client != null)
            {
                Client.Open();
                Client.DataReceivedEvent += Client_DataReceivedEvent;
                Client.StatusChangedEvent += Client_StatusChangedEvent;
                isOpened = true;
                heartbeatThread = new Thread(StartHeartbeat) { IsBackground = true };
                heartbeatThread.Start();
                sendThread = new Thread(ProcessSendQueue) { IsBackground = true };
                sendThread.Start();

            }
        }

        private void Client_StatusChangedEvent(object sender, StatusChangedEventArgs e)
        {
            if (LogEnabled)
            {
                Logger.WriteLine(LogFolder, e.ToString());
            }
        }

        /// <summary>
        /// 关闭设备
        /// </summary>
        public void Close()
        {
            if (IsDebugModel)
            {
                IsConnected = false;
                return;
            }

            if (!isOpened)
            {
                return;
            }
            if (Client != null)
            {
                Client.DataReceivedEvent -= Client_DataReceivedEvent;
                Client.StatusChangedEvent -= Client_StatusChangedEvent;
                Client.Close();
                isOpened = false;
                IsConnected = false;
                isKeepHeartbeatTheadAlive = false;
                heartbeatThread = null;
                isKeepSendThreadAlive = false;
                sendThread = null;

            }
        }
        /// <summary>
        /// 复位错误，并清除报警状态
        /// </summary>
        public virtual void ResetError()
        {
            if (!IsConnected)
            {
                DeviceStatus = DeviceStatus.UnConnected;
            }
            else
            {
                DeviceStatus = DeviceStatus.Standy;
            }
        }


        /// <summary>
        /// 发送字节
        /// </summary>
        /// <param name="buffer">字节数组</param>
        public void SendBytes(byte[] buffer)
        {
            ProtocolTransmitModel arg = new ProtocolTransmitModel(new HexBare(buffer))
            {
                IsNeedResponse = false,
            };
            Write(arg);
        }
        /// <summary>
        /// 发送字符串指令。该方法根据ProtocolType自动包装
        /// </summary>
        /// <param name="command">命令的有效字符串，如果为Hexbare，则字符串则为16进制格式字符串</param>
        /// <param name="isNeedResponse">是否需要应答</param>
        /// <param name="maxSendCount">最大发送次数</param>
        /// <param name="responseTimeout">单次发送超时时间，单位ms</param>
        /// <returns>通过IsSuccess判断是否发送成功，Response返回应答数据</returns>
        /// <returns></returns>
        public ExecutionResult SendCommand(string command, bool isNeedResponse = true, int maxSendCount = 1, int responseTimeout = 300)
        {
            ProtocolBase obj = null;
            switch (ProtocolType)
            {
                case ProtocolType.HexBare:
                    obj = new HexBare(command.ToHexArray());
                    break;
                case ProtocolType.AsciiBare:
                    obj = new AsciiBare(command);
                    break;
                case ProtocolType.AsciiCR:
                    obj = new AsciiCR(command);
                    break;
                case ProtocolType.AsciiLF:
                    obj = new AsciiLF(command);
                    break;
                case ProtocolType.AsciiCRLF:
                    obj = new AsciiCRLF(command);
                    break;
                case ProtocolType.AsciiCheckSum:
                    obj = new AsciiCheckSum(command);
                    break;
            }
            return SendProtocol(obj, isNeedResponse, maxSendCount, responseTimeout);
        }
        /// <summary>
        /// 释放资源，取消强事件订阅
        /// </summary>
        public void Dispose()
        {
            this.Close();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region 保护方法
        protected virtual void OnDataReceived(ProtocolBase e)
        {
            Task.Run(() =>
            {
                ProtocolReceivedEvent?.Invoke(this, e);
                if (LogEnabled)
                {
                    Logger.WriteLine(LogFolder, $"[{e.CreateTime:yyyy-MM-dd HH:mm:ss.fff}] [Rx] {e.FriendlyText}");
                }
            });

        }
        protected virtual void OnDataTransmited(ProtocolBase e)
        {
            Task.Run(() =>
            {
                ProtocolTransmitedEvent?.Invoke(this, e);
                if (LogEnabled)
                {
                    Logger.WriteLine(LogFolder, $"[{e.CreateTime:yyyy-MM-dd HH:mm:ss.fff}] [Tx] {e.FriendlyText}");
                }
            });
        }
        protected virtual void OnHeartbeatTriggered(ExecutionResult result)
        {

        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Client.PropertyChanged -= Client_PropertyChanged;
                }
                disposed = true;
            }
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="sendObject">发送对象</param>
        /// <param name="isNeedResponse">是否需要应答</param>
        /// <param name="maxSendCount">最大发送次数</param>
        /// <param name="responseTimeout">单次发送超时时间，单位ms</param>
        /// <returns>通过IsSuccess判断是否发送成功，Response返回应答数据</returns>
        protected ExecutionResult SendProtocol(ProtocolBase sendObject, bool isNeedResponse = true, int maxSendCount = 1, int responseTimeout = 300)
        {
            if (IsDebugModel)
            {
                return ExecutionResult.Success(null);
            }

            if (sendObject.GetType().Name != ProtocolType.ToString())
            {
                throw new ArgumentException("发送的对象类型与协议类型不匹配，请设置属性ProtocolType");
            }
            ProtocolTransmitModel arg = new ProtocolTransmitModel(sendObject)
            {
                MaxSendCount = maxSendCount,
                ResponseTimeout = responseTimeout,
                IsNeedResponse = isNeedResponse,
            };

            SendQueue.Enqueue(arg);
            CheckSendThread();
            if (arg.IsNeedResponse)
            {
                while (!arg.HasReceiveResponse)
                {
                    // 超时退出
                    if ((DateTime.Now - arg.CreateTime).TotalMilliseconds > arg.MaxSendCount * arg.ResponseTimeout)
                    {
                        break;
                    }
                    Thread.Sleep(1);
                }
                if (arg.HasReceiveResponse)
                {
                    return ExecutionResult.Success(arg.ResponseBody);
                }
                else
                {
                    DeviceStatus = DeviceStatus.Warning;
                    return ExecutionResult.Failed(Properties.Message.CommunicationTimeout);
                }

            }
            return ExecutionResult.Success(null);
        }
        #endregion

        #region 私有方法

        #region 处理发送

        private void ProcessSendQueue()
        {
            isKeepSendThreadAlive = true;
            while (isKeepSendThreadAlive)
            {
                if (SendQueue.Count > 0)
                {

                    var cmd = SendQueue.FirstOrDefault();
                    if (cmd != null)
                    {
                        // 如果排队的时间已经超过最大的超时时间，则从发送队列移除
                        if ((DateTime.Now - cmd.CreateTime).TotalMilliseconds > cmd.MaxSendCount * cmd.ResponseTimeout)
                        {
                            SendQueue.TryDequeue(out _);
                            continue;
                        }
                        if (cmd.IsNeedResponse)
                        {
                            lastSendNeedResponse = cmd;
                            for (int i = 0; i < cmd.MaxSendCount; i++)
                            {
                                Write(cmd);
                                // 如果手动接收数据，则调用读取方法
                                if (!Client.IsAutoReceiveData)
                                {
                                    Client.Read(cmd.ResponseTimeout);
                                }
                                while (!cmd.HasReceiveResponse)
                                {
                                    if ((DateTime.Now - cmd.LastWriteTime).TotalMilliseconds >= cmd.ResponseTimeout)
                                    {
                                        break;
                                    }
                                    Thread.Sleep(1);
                                }
                                if (cmd.HasReceiveResponse)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Write(cmd);
                        }
                        SendQueue.TryDequeue(out _);
                    }
                }
                Thread.Sleep(1);
            }
        }
        /// <summary>
        /// 检查发送线程，如果发送线程意外终止，则重启发送线程
        /// </summary>
        private void CheckSendThread()
        {
            // 如果线程异常死掉
            if (sendThread != null && !sendThread.IsAlive)
            {
                isKeepSendThreadAlive = false;
                sendThread = null;
                sendThread = new Thread(ProcessSendQueue) { IsBackground = true };
                sendThread.Start();
            }
        }
        private void Write(ProtocolTransmitModel arg)
        {
            try
            {
                // 建立连接后才发送
                if (Client.IsEstablished)
                {
                    arg.LastWriteTime = DateTime.Now;
                    OnDataTransmited(arg.SendBody);
                    Client.Write(arg.SendBody.FullBytes);
                }
            }
            catch (Exception ex)
            {
            }

        }
        #endregion

        #region 处理接收

        private void Client_DataReceivedEvent(object sender, DataReceivedEventArgs e)
        {
            LastReceiveTime = DateTime.Now;
            List<ProtocolBase> items = new List<ProtocolBase>();
            lock (ReadLock)
            {
                // 如果两次接收数据的时间大于100ms，则自动清除缓存区数据
                if ((DateTime.Now - LastReceiveTime).TotalMilliseconds > 100)
                {
                    receiveBuffer.Clear();
                }
                lastReceiveTime = DateTime.Now;
                receiveBuffer.AddRange(e.Data);

                // 按照不同的协议格式进行解析
                switch (ProtocolType)
                {
                    case ProtocolType.HexBare:
                        items = HexBare.Analyse(receiveBuffer);
                        break;
                    case ProtocolType.AsciiCRLF:
                        items = AsciiCRLF.Analyse(receiveBuffer);
                        break;
                    case ProtocolType.AsciiCR:
                        items = AsciiCR.Analyse(receiveBuffer);
                        break;
                    case ProtocolType.AsciiLF:
                        items = AsciiLF.Analyse(receiveBuffer);
                        break;
                    case ProtocolType.AsciiCheckSum:
                        items = AsciiCheckSum.Analyse(receiveBuffer);
                        break;
                    case ProtocolType.ModbusTCP:
                        items = ModbusTCP.Analyse(receiveBuffer);
                        break;
                    case ProtocolType.ModbusRTU:
                        items = ModbusRTU.Analyse(receiveBuffer);
                        break;
                    case ProtocolType.AsciiBare:
                        // 接收到最后一个字节延迟20ms，如果未接收到新数据，再分析结果
                        Thread.Sleep(20);
                        if ((DateTime.Now - lastReceiveTime).TotalMilliseconds < 20)
                        {
                            return;
                        }
                        items = AsciiBare.Analyse(receiveBuffer);
                        break;
                }
            }
            // 如果接收到符合协议的数据，则认为通讯成功
            if (items.Count > 0)
            {
                IsConnected = true;
            }
            ProcessProtocolItems(items);
        }
        private void ProcessProtocolItems(List<ProtocolBase> items)
        {
            foreach (var item in items)
            {
                if (lastSendNeedResponse != null && !lastSendNeedResponse.HasReceiveResponse &&
                    (DateTime.Now - lastSendNeedResponse.LastWriteTime).TotalMilliseconds <= lastSendNeedResponse.ResponseTimeout)
                {
                    // 判断发送和接收的标识符是否一致，如果一致则认为发送成功
                    if (lastSendNeedResponse.SendBody.IsMatch(item))
                    {
                        lastSendNeedResponse.HasReceiveResponse = true;
                        lastSendNeedResponse.ResponseBody = item;
                    }

                }
                OnDataReceived(item);
            }
        }

        #endregion

        #region 心跳
        /// <summary>
        /// 心跳线程
        /// </summary>
        private void StartHeartbeat()
        {
            isKeepHeartbeatTheadAlive = true;
            while (isKeepHeartbeatTheadAlive)
            {
                if (Heartbeat.Content == null || !Heartbeat.IsEnabled)
                {
                    Thread.Sleep(2000);
                    continue;
                }
                if (!Client.IsEstablished)
                {
                    Thread.Sleep(2000);
                    continue;
                }
                if ((DateTime.Now - LastReceiveTime).TotalSeconds >= Heartbeat.IntervalSeconds)
                {
                    var result = SendProtocol(Heartbeat.Content, true, Heartbeat.MaxRetryCount, Heartbeat.Timeout);
                    InvokeHeartbeatTriggered(result);
                    IsConnected = result.IsSuccess;
                }
                Thread.Sleep(2000);
            }
        }

        private void InvokeHeartbeatTriggered(ExecutionResult result)
        {
            switch (Heartbeat.HeartbeatTriggerCondition)
            {
                case HeartbeatTriggerCondition.Always:
                    Task.Run(() =>
                    {
                        HeartbeatTriggeredEvent?.Invoke(this, result);
                        OnHeartbeatTriggered(result);
                    });
                    break;
                case HeartbeatTriggerCondition.Changed:
                    if (IsConnected != result.IsSuccess)
                    {
                        Task.Run(() =>
                        {
                            HeartbeatTriggeredEvent?.Invoke(this, result);
                            OnHeartbeatTriggered(result);
                        });
                    }
                    break;
            }
        }
        #endregion



        #endregion

        #region 事件处理

        private void Client_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!Heartbeat.IsEnabled || Heartbeat.Content == null)
            {
                // 握手信息改变，则连接状态也同步改变
                IsConnected = Client.IsEstablished;
            }
        }
        #endregion

    }
}

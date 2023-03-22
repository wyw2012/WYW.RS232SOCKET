using WYW.Communication.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WYW.Communication.TransferLayer;
using System.Collections.Concurrent;
using DataReceivedEventArgs = WYW.Communication.TransferLayer.DataReceivedEventArgs;

namespace WYW.Communication.ApplicationlLayer
{
    /// <summary>
    /// 使用协议的设备
    /// </summary>
    public class Device : ObservableObject, IDisposable
    {
        private bool isOpened = false;
        private TransferBase client;
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
            this.client = client;
            client.PropertyChanged += Client_PropertyChanged;
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="client">传输媒介</param>
        /// <param name="protocol">应用层协议类型</param>
        public Device(TransferBase client, ProtocolType protocol) : this(client)
        {
            ProtocolType = protocol;
        }
        #endregion

        #region  属性

        private bool isConnected;
        private bool logEnabled;
        /// <summary>
        /// 是否建立连接，如果有心跳，则利用心跳判断，如果无心跳，则以通讯建立连接为标志
        /// </summary>
        public bool IsConnected
        {
            get => isConnected;
            set => SetProperty(ref isConnected, value);
        }
        /// <summary>
        /// 是否保持心跳，如果为true，需要设置HeartbeatContent
        /// </summary>
        public bool IsKeepHeartbeat { get; set; }
        /// <summary>
        /// 仅在IsKeepHeartbeat=true时有效
        /// </summary>
        public ProtocolBase HeartbeatContent { get; set; } = null;
        /// <summary>
        /// 最后一次接收到数据时间
        /// </summary>
        public DateTime LastReceiveTime { get; protected set; } = DateTime.MinValue;

        /// <summary>
        /// 是否启用日志
        /// </summary>
        public bool LogEnabled
        {
            get { return logEnabled; }
            set { logEnabled = value; }
        }
        /// <summary>
        /// 应用层协议类型
        /// </summary>
        public ProtocolType ProtocolType { get; protected set; }
        #endregion

        #region 事件

        public delegate void ProtocolReceivedEventHandler(object sender, ProtocolBase obj);
        public delegate void ProtocolTransmitedEventHandler(object sender, ProtocolBase obj);
        /// <summary>
        /// 接收消息事件
        /// </summary>
        public event ProtocolReceivedEventHandler ProtocolReceivedEvent;
        /// <summary>
        /// 发送消息事件
        /// </summary>
        public event ProtocolTransmitedEventHandler ProtocolTransmitedEvent;

        #endregion

        #region  公共方法
        /// <summary>
        /// 打开设备
        /// </summary>
        public void Open()
        {
            if (isOpened)
            {
                return;
            }
            if (client != null)
            {
                client.Open();
                client.DataReceivedEvent += Client_DataReceivedEvent;
                isOpened = true;
                heartbeatThread = new Thread(StartHeartbeat) { IsBackground = true };
                heartbeatThread.Start();
                sendThread = new Thread(ProcessSendQueue) { IsBackground = true };
                sendThread.Start();
            }
        }
        /// <summary>
        /// 关闭设备
        /// </summary>
        public void Close()
        {
            if (!isOpened)
            {
                return;
            }
            if (client != null)
            {
                client.DataReceivedEvent -= Client_DataReceivedEvent;
                client.Close();
                isOpened = false;
                IsConnected = false;
                isKeepHeartbeatTheadAlive = false;
                heartbeatThread = null;
                isKeepSendThreadAlive = false;
                sendThread = null;
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
        public ExecutionResult SendProtocol(ProtocolBase sendObject, bool isNeedResponse = true, int maxSendCount = 1, int responseTimeout = 300)
        {
            if (sendObject.GetType().Name != ProtocolType.ToString())
            {
                throw new ArgumentException("发送的对象类型与协议类型不匹配");
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
                    return ExecutionResult.Failed();
                }

            }
            return ExecutionResult.Success(null);
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
        /// 释放资源，取消强事件订阅
        /// </summary>
        public void Dispose()
        {
            client.PropertyChanged -= Client_PropertyChanged;
        }
        #endregion

        #region 保护方法
        protected virtual void OnDataReceived(ProtocolBase e)
        {
            ProtocolReceivedEvent?.Invoke(this, e);
            if (LogEnabled)
            {
                Logger.Debug($"[{e.CreateTime:yyyy-MM-dd HH:mm:ss.fff}] [Rx] {e.FriendlyText}");
            }
        }
        protected virtual void OnDataTransmited(ProtocolBase e)
        {
            ProtocolTransmitedEvent?.Invoke(this, e);
            if (LogEnabled)
            {
                Logger.Debug($"[{e.CreateTime:yyyy-MM-dd HH:mm:ss.fff}] [Tx] {e.FriendlyText}");
            }
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
                if (client.IsEstablished)
                {
                    client.Write(arg.SendBody.FullBytes);
                    arg.LastWriteTime = DateTime.Now;
                    OnDataTransmited(arg.SendBody);
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
            IsConnected = true;
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
                        ThreadPool.QueueUserWorkItem(delegate
                        {
                            // 接收到最后一个字节延迟20ms，如果未接收到新数据，再分析结果
                            Thread.Sleep(20);
                            if ((DateTime.Now - lastReceiveTime).TotalMilliseconds > 20)
                            {
                                items = AsciiBare.Analyse(receiveBuffer);
                                ProcessProtocolItems(items);
                            }
                        });
                        return;

                }
                ProcessProtocolItems(items);
            }

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
                if (HeartbeatContent == null || !IsKeepHeartbeat)
                {
                    continue;
                }
                if ((DateTime.Now - LastReceiveTime).TotalSeconds >= 5)
                {
                    IsConnected = SendProtocol(HeartbeatContent).IsSuccess;
                }
                Thread.Sleep(2000);
            }
        }
        #endregion
        #endregion

        #region 事件处理

        private void Client_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!IsKeepHeartbeat || HeartbeatContent == null)
            {
                // 握手信息改变，则连接状态也同步改变
                IsConnected = client.IsEstablished;
            }
        }
        #endregion

    }
}

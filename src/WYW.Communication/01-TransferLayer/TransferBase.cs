using System.Diagnostics;
using System.Threading.Tasks;

namespace WYW.Communication.TransferLayer
{
    /// <summary>
    /// 传输层基类，子类有RS232Client、TCPClient、TCPServer、UDPClient、UDPServer
    /// </summary>
    public abstract class TransferBase : ObservableObject
    {

        #region 属性
        private bool isEstablished;
        private bool isOpen;
        private bool logEnabled;

        /// <summary>
        /// 是否记录日志
        /// </summary>
        public bool LogEnabled
        {
            get => logEnabled;
            set => SetProperty(ref logEnabled, value);
        }

        /// <summary>
        /// 通讯是否建立连接，这里只是传输层建立连接，应用层使用IsConnected
        /// </summary>
        public bool IsEstablished
        {
            get => isEstablished;
            set => SetProperty(ref isEstablished, value);
        }
        /// <summary>
        /// 是否启动，建立连接请使用IsEstablished属性
        /// </summary>
        public bool IsOpen
        {
            get => isOpen;
            set => SetProperty(ref isOpen, value);
        }
        /// <summary>
        /// 日志文件夹，绝对或者相对路径
        /// </summary>
        public string LogFolder { get; set; } = "Log\\Commucation";

        /// <summary>
        /// 是否自动接收数据，默认自动读取数据，然后通过<see cref="DataReceivedEvent"/>事件通知订阅者
        /// </summary>
        public bool IsAutoReceiveData { get; protected set; } = true;
        #endregion

        #region  公共事件
        public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);
        public delegate void DataTransmitedEventHandler(object sender, DataTransmitedEventArgs e);
        public delegate void StatusChangedEventHandler(object sender, StatusChangedEventArgs e);

        /// <summary>
        /// 数据接收事件
        /// </summary>
        public event DataReceivedEventHandler DataReceivedEvent;
        /// <summary>
        /// 数据发送事件
        /// </summary>
        public event DataTransmitedEventHandler DataTransmitedEvent;
        /// <summary>
        /// 状态改变事件
        /// </summary>
        public event StatusChangedEventHandler StatusChangedEvent;

        #endregion

        #region 公共方法
        /// <summary>
        /// 打开设备
        /// </summary>
        public abstract void Open();
        /// <summary>
        /// 关闭设备
        /// </summary>
        public abstract void Close();
        /// <summary>
        /// 写数据
        /// </summary>
        /// <param name="content"></param>
        public abstract void Write(byte[] content);

        /// <summary>
        /// 读数据
        /// </summary>
        /// <param name="timeout">超时时间，单位ms</param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public virtual byte[] Read(int timeout = 0)
        {
            throw new System.Exception("The read method is not supported, please use DataTransmitedEvent to instead it.");
        }

        /// <summary>
        /// 清空IO缓存
        /// </summary>
        /// <exception cref="System.Exception"></exception>
        public virtual void ClearBuffer()
        {
           
        }
        #endregion

        #region 内部方法
        protected void OnStatusChanged(string message)
        {
            Task.Run(() =>
            {
                StatusChangedEventArgs e = new StatusChangedEventArgs(message);
                StatusChangedEvent?.Invoke(this, e);

#if DEBUG
                Trace.WriteLine(e.ToString());
#endif
                if (LogEnabled)
                {
                    Logger.WriteLine(LogFolder, e.ToString());
                }
            });
        }
        protected void OnDataReceived(byte[] buffer)
        {
            Task.Run(() =>
            {
                DataReceivedEventArgs e = new DataReceivedEventArgs(buffer);
#if DEBUG
                Trace.WriteLine(e.ToString());
#endif
                DataReceivedEvent?.Invoke(this, e);
                if (LogEnabled)
                {
                    Logger.WriteLine(LogFolder, e.ToString());
                }
            });


        }
        protected void OnDataTransmited(byte[] buffer)
        {
            Task.Run(() =>
            {
                DataTransmitedEventArgs e = new DataTransmitedEventArgs(buffer);

#if DEBUG
                Trace.WriteLine(e.ToString());
#endif
                DataTransmitedEvent?.Invoke(this, e);
                if (LogEnabled)
                {
                    Logger.WriteLine(LogFolder, e.ToString());
                }
            });

        }
        #endregion

    }
}

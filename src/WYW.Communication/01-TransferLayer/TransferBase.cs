using System.Diagnostics;

namespace WYW.Communication.TransferLayer
{
    public abstract class TransferBase : ObservableObject
    {

        #region 属性
        private bool isEstablished;
        private bool isOpen;
        /// <summary>
        /// 是否记录日志
        /// </summary>
        public bool LogEnabled { get; set; }

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


        #endregion

        #region 内部方法
        protected void OnStatusChanged(string message)
        {
            StatusChangedEventArgs e = new StatusChangedEventArgs(message);
            StatusChangedEvent?.Invoke(this, e);

#if DEBUG
            Trace.WriteLine(e.ToString());
#endif
            if (LogEnabled)
            {
                Logger.Debug(e.ToString(), false);
            }

        }
        protected void OnDataReceived(byte[] buffer)
        {
            DataReceivedEventArgs e = new DataReceivedEventArgs(buffer);
            DataReceivedEvent?.Invoke(this, e);
#if DEBUG
            Trace.WriteLine(e.ToString());
#endif
            if (LogEnabled)
            {
                Logger.Debug(e.ToString(), false);
            }

        }
        protected void OnDataTransmited(byte[] buffer)
        {
            DataTransmitedEventArgs e = new DataTransmitedEventArgs(buffer);
            DataTransmitedEvent?.Invoke(this, e);
#if DEBUG
            Trace.WriteLine(e.ToString());
#endif
            if (LogEnabled)
            {
                Logger.Debug(e.ToString(), false);
            }
        }
        #endregion

    }
}

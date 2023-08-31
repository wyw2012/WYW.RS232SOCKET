using System;
using System.IO.Ports;

namespace WYW.Communication.TransferLayer
{
    public class RS232Client : TransferBase
    {
        private readonly SerialPort serialPort;
        private readonly int writeBufferSize;
        private static object ReadLocker = new object();
        #region 构造函数

        public RS232Client(string portName, int baudRate = 9600, int parity = 0, int dataBits = 8, int stopBits = 1, int writeBufferSize = 4096, int receiveBufferSize = 4096)
        {
            this.writeBufferSize = writeBufferSize;
            serialPort = new SerialPort(portName, baudRate, (Parity)parity, dataBits, (StopBits)stopBits)
            {
                ReadBufferSize = receiveBufferSize,
                WriteBufferSize = writeBufferSize
            };
        }
        #endregion

        #region 实现虚方法
        public override void Open()
        {
            if (IsOpen)
                return;
            serialPort.Open();
            serialPort.DataReceived += serialPort_DataReceived;
            IsEstablished = IsOpen = true;
            OnStatusChanged($"{serialPort.PortName}打开 [{serialPort.BaudRate},{serialPort.Parity},{serialPort.DataBits},{serialPort.StopBits}]");
        }
        public override void Close()
        {
            if (!IsOpen)
                return;
            serialPort.DataReceived -= serialPort_DataReceived;
            OnStatusChanged($"{serialPort.PortName}关闭");
            serialPort.Close();
            IsEstablished = IsOpen = false;
   

        }
        /// <summary>
        /// 发送数据，如果数据长度超过写缓存大小，则会被自动拆分多次发送
        /// </summary>
        /// <param name="content"></param>
        public override void Write(byte[] content)
        {
            if (content == null || content.Length == 0)
            {
                return;
            }
            if (serialPort != null)
            {
                try
                {
                    int row = (content.Length - 1) / writeBufferSize + 1; // 向上取整
                    int remain = content.Length % writeBufferSize;
                    double waitTime = 1000 / (serialPort.BaudRate / 10.0) * writeBufferSize; // 最长的一帧发送时间，单位：ms
                    for (int i = 0; i < row; i++)
                    {
                        if (i == row - 1)
                        {
                            serialPort.Write(content, i * writeBufferSize, remain == 0 ? writeBufferSize : remain);
                        }
                        else
                        {
                            serialPort.Write(content, i * writeBufferSize, writeBufferSize);
                            //Thread.Sleep((int)waitTime);
                        }
                    }
                    OnDataTransmited(content);
                }
                catch (Exception ex)
                {
                    OnStatusChanged($"发送失败，发送数据：{content.ToHexString()}，原因：{ex.Message}");
                }
            }
        }

        public override void ClearBuffer()
        {
            serialPort?.DiscardInBuffer();
            serialPort?.DiscardOutBuffer();
        }
        #endregion
        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (ReadLocker)
            {
                if (e.EventType == SerialData.Eof)
                    return;
                var bytesLength = serialPort.BytesToRead;
                var receivedData = new byte[bytesLength];
                serialPort.Read(receivedData, 0, bytesLength);
                OnDataReceived(receivedData);
            }
        }
    }
}

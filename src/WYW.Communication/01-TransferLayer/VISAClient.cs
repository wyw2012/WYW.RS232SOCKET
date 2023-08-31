using NationalInstruments.Visa;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WYW.Communication.TransferLayer
{
    public class VISAClient : TransferBase
    {
        private string resourceName = string.Empty;
        private MessageBasedSession mbSession;
        public VISAClient(string resourceName)
        {
            this.resourceName = resourceName;
            IsManulReceiveData = true;
        }
        public override void Close()
        {
            if (!IsOpen)
                return;
            mbSession?.Dispose();
            IsEstablished = IsOpen = false;
            OnStatusChanged("主动断开VISA连接。");

        }

        public override void Open()
        {
            if (IsOpen)
                return;
            IsOpen = true;
            Task.Run(() =>
            {
                while (IsOpen)
                {
                    try
                    {
                        using (var rmSession = new ResourceManager())
                        {

                            mbSession = (MessageBasedSession)rmSession.Open(resourceName);
                            OnStatusChanged($"VISA已建立连接");
                            IsEstablished = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        IsEstablished = false;
                    }
                    Thread.Sleep(2000);
                }
            });
        }

        public override void Write(byte[] content)
        {
            if (mbSession != null)
            {
                try
                {
                    mbSession?.RawIO.Write(content);
                    OnDataTransmited(content);
                }
                catch (Exception ex)
                {
                    OnStatusChanged($"发送失败，发送数据：{content.ToHexString()}，原因：{ex.Message}");
                }
            }
        }
        /// <summary>
        /// 清空IO缓存
        /// </summary>
        public override void ClearBuffer()
        {
            try { mbSession?.Clear(); } catch { }
        }
        public override byte[] Read(int timeout = 0)
        {
            if(mbSession ==null)
                return new byte[0];
            try
            {
                mbSession.TimeoutMilliseconds = timeout;
                var buffer = mbSession.RawIO.Read();
                if (buffer.Length > 0)
                {
                    OnDataReceived(buffer);
                    return buffer;
                }
            }
            catch (Exception ex)
            {
            }
            return new byte[0];
        }


        /// <summary>
        /// 获取所有资源名称
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetResouceNames()
        {
            using (var rmSession = new ResourceManager())
            {
                return rmSession.Find("(ASRL|GPIB|TCPIP|USB)?*");
            }
        }
    }
}

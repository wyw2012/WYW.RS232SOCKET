using Ivi.Visa;
using NationalInstruments.Visa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace WYW.Modbus.Clients
{
    public class VISAClient : ClientBase
    {
        private string resourceName = string.Empty;
        private MessageBasedSession mbSession;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resourceName">资源名称</param>>
        public VISAClient(string resourceName)
        {
            this.resourceName = resourceName;
        }
        #region 属性
        public int ReceiveTimeout { get; set; } = 2000;
        /// <summary>
        /// 接收终止符
        /// </summary>
        public byte TerminationCharacter { get; set; } = 0x0A;
        #endregion
        public override void Close()
        {
            mbSession?.Dispose();
            IsEstablished = IsOpen = false;
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
                    if (IsEstablished)
                    {
                        Thread.Sleep(2000);
                        continue;
                    }
                    try
                    {
                        using (var rmSession = new ResourceManager())
                        {
                            var resouces = rmSession.Find("(ASRL|GPIB|TCPIP|USB)?*");
                            // 使用模糊查找
                            if (!resouces.Any(x => x == resourceName) &&
                                resouces.Any(x => x.Contains(resourceName)))
                            {
                                var items = resouces.Where(x => x.Contains(resourceName));
                                resourceName = items.FirstOrDefault();
                            }

                            if (resouces.Any(x => x == resourceName))
                            {
                                mbSession?.Dispose();
                                mbSession = (MessageBasedSession)rmSession.Open(resourceName);

                                mbSession.TerminationCharacter = TerminationCharacter;
                                mbSession.TerminationCharacterEnabled = true;
                                mbSession.Clear();
                                ClearReceiveBuffer();
                                IsEstablished = true;
                            }

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
        public override void ClearReceiveBuffer()
        {
            // 清空IO缓存，没有其它方式，只能通过读取方法清空
            try
            {
                mbSession.RawIO.Read();
            }
            catch (Exception ex)
            {
            }       
        }
        public override bool Read(ref List<byte> receiveBuffer)
        {
            if (mbSession == null)
            {
                ErrorMessage = "设备未打开";
                return false;
            }
               
            try
            {
                mbSession.TimeoutMilliseconds = ReceiveTimeout;
                var buffer = mbSession.RawIO.Read();
                if (buffer.Length > 0)
                {
                    receiveBuffer.AddRange(buffer);
                    return true;
                }
            }
            catch (NativeVisaException ex)
            {
                IsEstablished = false;
                ErrorMessage = ex.ToString();
            }
            catch (IOTimeoutException ex)
            {
                ErrorMessage = ex.ToString();
            }
            catch (Exception ex)
            {
                IsEstablished = false;
                ErrorMessage = ex.ToString();
            }
            return false;
        }

        public override bool Write(byte[] buffer)
        {
            if (mbSession != null)
            {
                try
                {
                    mbSession.RawIO.Write(buffer);
                    return true;
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"{ex.Message}";
                    IsEstablished = false;
                }
            }
            else
            {
                ErrorMessage = "设备未打开";
            }
            return false;
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

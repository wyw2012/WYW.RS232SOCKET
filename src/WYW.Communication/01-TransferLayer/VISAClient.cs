using Ivi.Visa;
using NationalInstruments.Visa;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace WYW.Communication.TransferLayer
{
    public class VISAClient : TransferBase
    {
        private string resourceName = string.Empty;
        private MessageBasedSession mbSession;
        public VISAClient(string resourceName, bool isAutoReceiveData = false)
        {
            this.resourceName = resourceName;
            IsAutoReceiveData = isAutoReceiveData;
        }
        #region 实现虚方法
        public override void Close()
        {
            if (!IsOpen)
                return;
            IsEstablished = IsOpen = false;
            mbSession?.Dispose();
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
                    if (IsEstablished)
                    {
                        Thread.Sleep(2000);
                        continue;
                    }
                    try
                    {
                        using (var rmSession = new ResourceManager())
                        {
                           var resouces=  rmSession.Find("(ASRL|GPIB|TCPIP|USB)?*");
                            if(resouces.Any(x=>x==resourceName))
                            {
                                mbSession?.Dispose();
                                mbSession = (MessageBasedSession)rmSession.Open(resourceName);
                                mbSession.Clear();
                                mbSession.TerminationCharacter = TerminationCharacter;
                                //mbSession.TerminationCharacterEnabled=true;
                                OnStatusChanged($"VISA已建立连接");
                                IsEstablished = true;
                                if (IsAutoReceiveData)
                                {
                                    VISA_DataReceived();
                                }
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
                    IsEstablished = false;
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
        public override byte[] Read(int timeout = 2000)
        {
            if(IsAutoReceiveData)
            {
                throw new Exception("当前为自动读取模式，请订阅DataReceivedEvent事件获取接收的数据");
            }
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

        #endregion

        /// <summary>
        /// 接收终止符
        /// </summary>
        public byte TerminationCharacter { get; set; } = 0x0A;

        /// <summary>
        /// 自动接收数据，经过验证，该方法不可靠
        /// </summary>
        private void VISA_DataReceived()
        {
            while (IsOpen)
            {
                try
                {
                    // 只能接收以0x0A结尾的数组
                    var buffer = mbSession.RawIO.Read();
                    if (buffer.Length > 0)
                    {
                        OnDataReceived(buffer);
                    }
                }
                catch (IOTimeoutException ex)
                {
                }
                catch(Exception ex)
                {
                    // 连接中断了
                    IsEstablished = false;
                    OnStatusChanged($"接收数据异常，{ex.Message}");
                    break; // 退出本次接收线程
                }
                Thread.Sleep(1);
            }
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

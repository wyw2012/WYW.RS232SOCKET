using System;
using System.Net.Sockets;
using System.Threading;

namespace WYW.Communication.TransferLayer
{
    public class UDPClient : TransferBase
    {
        private UdpClient clientSocket;
        private readonly System.Net.IPEndPoint localIpep;
        private System.Net.IPEndPoint remoteIpep;
        private byte[] inBuffer ;
        public UDPClient(string localIP, int localPort, string remoteIP, int remotePort, int receiveBufferSize = 4096)
        {
            inBuffer = new byte[receiveBufferSize];
            localIpep = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(localIP), localPort); // 本地端口监听
            remoteIpep = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(remoteIP), remotePort);
            clientSocket = new UdpClient(localIpep);
        }
        #region 实现虚方法
        public override void Open()
        {
            if (IsOpen)
                return;
            if (clientSocket != null)
            {
                IsOpen = true;
                ThreadPool.QueueUserWorkItem(delegate
                {
                    clientSocket_DataReceived();
                });
                OnStatusChanged("UDP Server已开启。");
            }
        }

        public override void Close()
        {
            if (!IsOpen)
                return;
            if (clientSocket != null)
            {
                clientSocket.Close();
            }            
            IsEstablished= IsOpen = false;          
            OnStatusChanged("UDP Server已主动关闭。");
        }
        public override void Write(byte[] content)
        {
            if (clientSocket != null)
            {
                try
                {
                    clientSocket.Send(content, content.Length, remoteIpep);
                    OnDataTransmited(content);
                }
                catch (Exception ex)
                {
                    IsEstablished = false;
                    OnStatusChanged($"发送失败，发送数据：{content.ToHexString()}，原因：{ex.Message}");
                }
            }
        }
        #endregion
        private void clientSocket_DataReceived()
        {
            while (IsOpen)
            {
                try
                {
                    inBuffer = clientSocket.Receive(ref remoteIpep);
                    if (inBuffer.Length > 0) //如果接收的消息为空 阻塞当前循环  
                    {
                        IsEstablished = true;
                        OnDataReceived(inBuffer);
                    }
                }
                catch (Exception ex)
                {
                    IsEstablished = false;
                    OnStatusChanged($"接收数据异常，{ex.Message}");
                }
                Thread.Sleep(1);
            }
        }
    }
}

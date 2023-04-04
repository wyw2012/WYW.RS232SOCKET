using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WYW.Communication.TransferLayer
{
    public class UDPServer : TransferBase
    {
        private UdpClient serverSocket;
        private readonly IPEndPoint localIpep;
        private IPEndPoint remoteIpep;
        private byte[] inBuffer;
        public UDPServer(string localIP, int localPort, int receiveBufferSize = 4096)
        {
            inBuffer = new byte[receiveBufferSize];
            localIpep = new IPEndPoint(IPAddress.Parse(localIP), localPort); // 本地端口监听
            remoteIpep = new IPEndPoint(IPAddress.Any, 0);
            serverSocket = new UdpClient(localIpep);
        }
        public override void Open()
        {
            if (IsOpen)
                return;
            if (serverSocket != null)
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
            if (serverSocket != null)
            {
                serverSocket.Close();
            }             
            IsEstablished = IsOpen = false;
            OnStatusChanged("UDP Server已主动关闭。");
        }              

        public override void Write(byte[] content)
        {
            if (serverSocket != null)
            {
                try
                {
                    serverSocket.Send(content, content.Length, remoteIpep);
                    OnDataTransmited(content);
                }
                catch (Exception ex)
                {
                    IsEstablished = false;
                    OnStatusChanged($"发送失败，发送数据：{content.ToHexString()}，原因：{ex.Message}");
                }
            }
        }
        private void clientSocket_DataReceived()
        {
            while (IsOpen)
            {
                try
                {
                    inBuffer = serverSocket.Receive(ref remoteIpep);
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

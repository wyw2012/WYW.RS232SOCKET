using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WYW.Communication.TransferLayer
{
    public class TCPServer : TransferBase
    {
        private Socket clientSocket; // 最后一次连接的客户端
        private readonly IPEndPoint ipep;
        private Socket serverSocket;
        private readonly byte[] inBuffer;

        public TCPServer(string ip, int port, int receiveBufferSize = 4096)
        {
            inBuffer = new byte[receiveBufferSize];
            ipep = new IPEndPoint(IPAddress.Parse(ip), port);
            serverSocket = new Socket(ipep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
        }

        #region 实现虚方法
        public override void Open()
        {
            if (IsOpen)
                return;
            if (serverSocket != null)
            {
                try
                {
                    serverSocket.Bind(ipep);
                    serverSocket.Listen(10);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                IsOpen = true;
                ThreadPool.QueueUserWorkItem(delegate
                {
                    while (IsOpen)
                    {
                        try
                        {
                            Socket client = serverSocket.Accept();
                            OnStatusChanged($"Socket建立连接。远程节点：{client.RemoteEndPoint}");
                            Dispose(clientSocket);
                            clientSocket = client; // 仅保持最后一个连接，如果保持多个连接请使用数组
                            IsEstablished = true;
                            Thread.Sleep(15);
                            ThreadPool.QueueUserWorkItem(delegate
                            {
                                clientSocket_DataReceived();
                            });
                        }
                        catch (Exception ex)
                        {
                            IsEstablished = false;
                            OnStatusChanged(ex.Message);
                        }
                        Thread.Sleep(2000);
                    }
                });
            }
        }

        public override void Close()
        {
            if (!IsOpen)
                return;
            if (clientSocket != null)
            {
                clientSocket.Close();
                clientSocket.Dispose();
            }
            if (serverSocket != null)
            {
                serverSocket.Close();
                serverSocket.Dispose();
            }
            IsEstablished = IsOpen = false;
            OnStatusChanged("Socket Server已主动关闭。");
        }

        public override void Write(byte[] content)
        {
            if (clientSocket != null)
            {
                try
                {
                    clientSocket.Send(content);
                    OnDataTransmited(content);
                }
                catch (Exception ex)
                {
                    OnStatusChanged($"发送失败，Socket未建立连接。发送内容：{content.ToHexString()}");
                }
            }
        }
        #endregion
        private void clientSocket_DataReceived()
        {
            while (IsOpen)
            {
                if (clientSocket != null)
                {
                    try
                    {
                        if (clientSocket.Poll(0, SelectMode.SelectRead))
                        {
                            var receivedCount = clientSocket.Receive(inBuffer, inBuffer.Length, SocketFlags.None);
                            if (receivedCount > 0) //如果接收的消息为空 阻塞当前循环  
                            {
                                OnDataReceived(inBuffer.Take(receivedCount).ToArray());
                            }
                        }
                    }
                    // 本地网络断开或者客户机主动断开
                    catch (Exception ex)
                    {
                        OnStatusChanged($"与客户端断开连接，可能是客户端主动断开，也可能是网络中断。");
                        Dispose(clientSocket);
                        IsEstablished = false;
                        break; // 退出本次接收线程
                    }
                }

                Thread.Sleep(1);
            }
        }

        private void Dispose(Socket socket)
        {
            if (socket != null)
            {
                socket.Close();
                socket.Dispose();
                socket = null;
            }
            IsEstablished = false;
        }
    }
}

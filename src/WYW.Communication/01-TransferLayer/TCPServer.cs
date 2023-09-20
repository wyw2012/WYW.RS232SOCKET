using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WYW.Communication.TransferLayer
{
    public class TCPServer : TransferBase
    {
        private Socket lastActiveClientSocket; // 最近一次活动的客户端
        private List<Socket> clientSockets=new List<Socket>(); // 客户端队列
        private readonly IPEndPoint ipep;
        private Socket serverSocket;
        private readonly byte[] inBuffer;
        private readonly int maxClientCount; // 最大客户端数量

        public TCPServer(string ip, int port, int receiveBufferSize = 4096,int maxClientCount = 100)
        {
            inBuffer = new byte[receiveBufferSize];
            ipep = new IPEndPoint(IPAddress.Parse(ip), port);
            this.maxClientCount = maxClientCount;
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
                            // 客户端超过最大数量，则移除最先建立连接的。最合理的方案是移除长时间未通讯的，目前的方案是简单粗暴
                            if(clientSockets.Count>maxClientCount)
                            {
                                Dispose(clientSockets[0]);
                            }
                          
                            clientSockets.Add(client);
                            lastActiveClientSocket = client;
                            Thread.Sleep(15);
                            ThreadPool.QueueUserWorkItem(delegate
                            {
                                clientSocket_DataReceived(client);
                            });
                            IsEstablished = true;
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
            OnStatusChanged("Socket Server已主动关闭。");
            foreach (var client in clientSockets)
            {
                client.Close();
                client.Dispose();
            }
            if (serverSocket != null)
            {
                serverSocket.Close();
                serverSocket.Dispose();
            }
            IsEstablished = IsOpen = false;
      
        }

        public override void Write(byte[] content)
        {
            if (lastActiveClientSocket != null)
            {
                try
                {
                    lastActiveClientSocket.Send(content);
                    OnDataTransmited(content);
                }
                catch (Exception ex)
                {
                    OnStatusChanged($"发送失败，Socket未建立连接。发送内容：{content.ToHexString()}");
                }
            }
        }
        #endregion
        private void clientSocket_DataReceived(Socket client)
        {
            while (IsOpen)
            {
                if (client != null)
                {
                    try
                    {
                        if (client.Poll(0, SelectMode.SelectRead))
                        {
                            var receivedCount = client.Receive(inBuffer, inBuffer.Length, SocketFlags.None);
                            if (receivedCount > 0) //如果接收的消息为空 阻塞当前循环  
                            {
                                lastActiveClientSocket = client;
                                OnDataReceived(inBuffer.Take(receivedCount).ToArray());
                            }
                        }
                    }
                    // 本地网络断开或者客户机主动断开
                    catch (Exception ex)
                    {
                        OnStatusChanged($"与客户端断开连接，可能是客户端主动断开，也可能是网络中断。");
                        Dispose(client);
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
                clientSockets.Remove(socket);
                socket.Close();
                socket.Dispose();
                socket = null;
            }
            IsEstablished = false;
        }
    }
}

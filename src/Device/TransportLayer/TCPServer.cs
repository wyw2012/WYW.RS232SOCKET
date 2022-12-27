using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using WYW.RS232SOCKET.Events;

namespace WYW.RS232SOCKET.Devices
{
    class TCPServer : DeviceBase
    {
        private Socket clientSocket; // 最后一次连接的客户端
        private readonly IPEndPoint ipep;
        private Socket serverSocket;
        private bool isKeepThreadAlive = false;
        private readonly byte[] inBuffer ;
        public TCPServer(string ip, int port, int bufferSize)
        {
            ipep = new IPEndPoint(IPAddress.Parse(ip), port);
            serverSocket = new Socket(ipep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp)
            {
                ReceiveBufferSize = bufferSize,
                SendBufferSize = bufferSize
            };
            inBuffer = new byte[bufferSize];
        }
        public override void Open()
        {
            if (serverSocket != null)
            {
                try
                {
                    serverSocket.Bind(ipep);
                    serverSocket.Listen(2);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                isKeepThreadAlive = true;
                ThreadPool.QueueUserWorkItem(delegate
                {
                    while (isKeepThreadAlive)
                    {
                        try
                        {
                            Socket client = serverSocket.Accept();
                            InvokeDeviceStatuChangedEvent(new DeviceStautsChangedEventArgs($"Socket建立连接。远程节点：{client.RemoteEndPoint}"));
                            Dispose(clientSocket);
                            clientSocket = client; // 保持最后一个连接
                            Thread.Sleep(15);
                            ThreadPool.QueueUserWorkItem(delegate
                            {
                                clientSocket_DataReceived();
                            });
                        }
                        catch (Exception ex)
                        {
                            InvokeDeviceStatuChangedEvent(new DeviceStautsChangedEventArgs(ex.Message));
                        }
                        Thread.Sleep(2000);
                    }
                });
            }
        }

        public override void Close()
        {
            Dispose(clientSocket);
            isKeepThreadAlive = false;
            if (clientSocket != null)
            {
                InvokeDeviceStatuChangedEvent(new DeviceStautsChangedEventArgs("Socekt Server已关闭。"));
            }

            serverSocket?.Close();
        }

        public override void Write(byte[] content)
        {
            if (clientSocket != null)
            {
                InvokeDeviceDataTransferedMessageEvent(new DeviceDataTransferedEventArgs (MessageType.Send, content));
                try
                {
                    clientSocket.Send(content);
                }
                catch (Exception ex)
                {
                    InvokeDeviceStatuChangedEvent(new DeviceStautsChangedEventArgs("发送失败，Socket未建立连接。"));
                }
            }
        }
        private void clientSocket_DataReceived()
        {
            while (isKeepThreadAlive)
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
                                InvokeDeviceDataTransferedMessageEvent(new DeviceDataTransferedEventArgs (MessageType.Receive, inBuffer.Take(receivedCount).ToArray()));
                            }
                        }
                    }
                    // 本地网络断开或者客户机主动断开
                    catch (Exception ex)
                    {
                        InvokeDeviceStatuChangedEvent(new DeviceStautsChangedEventArgs("与客户端断开连接，可能是客户端主动断开，也可能是网络中断。"));
                        Dispose(clientSocket);
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
        }
    }
}

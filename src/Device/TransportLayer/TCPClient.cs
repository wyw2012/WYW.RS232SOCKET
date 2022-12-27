using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using WYW.RS232SOCKET.Events;

namespace WYW.RS232SOCKET.Devices
{
    class TCPClient : DeviceBase
    {
        private Socket clientSocket;
        private readonly System.Net.IPEndPoint ipep;
        private bool isKeepThreadAlive = false;
        private readonly byte[] inBuffer;
        public TCPClient(string ip, int port, int bufferSize)
        {
            ipep = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(ip), port);
            //将网络端点表示为IP地址和端口 用于socket侦听时绑定   
            clientSocket = new Socket(ipep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp)
            {
                ReceiveBufferSize = bufferSize,
                SendBufferSize = bufferSize
            };
            inBuffer = new byte[bufferSize];
        }
        public override void Open()
        {
            if (clientSocket != null)
            {
                isKeepThreadAlive = true;
                ThreadPool.QueueUserWorkItem(delegate
                {
                    while (isKeepThreadAlive)
                    {
                        try
                        {
                            clientSocket.Connect(ipep);
                            InvokeDeviceStatuChangedEvent(new DeviceStautsChangedEventArgs($"Socket已建立连接。本地节点：{clientSocket.LocalEndPoint}"));
                            clientSocket_DataReceived();
                        }
                        // 目标主机未开启socket
                        catch (SocketException)
                        {
                            //Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Cann't connect server.");
                        }
                        // 目标主机主动断开socket
                        catch (InvalidOperationException ex)
                        {
                            InvokeDeviceStatuChangedEvent(new DeviceStautsChangedEventArgs("远程主机主动断开连接。"));
                            clientSocket.Close();
                            clientSocket = new Socket(ipep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                           
                        }
                        Thread.Sleep(2000);
                    }
                });

            }
        }

        public override void Close()
        {
            if (clientSocket != null)
            {
                clientSocket.Close();
                clientSocket.Dispose();
                isKeepThreadAlive = false;
                InvokeDeviceStatuChangedEvent(new DeviceStautsChangedEventArgs("Socket Client已关闭。"));
            }
        }

        public override void Write(byte[] content)
        {
            if (clientSocket != null)
            {
                try
                {
                    InvokeDeviceDataTransferedMessageEvent(new DeviceDataTransferedEventArgs (MessageType.Send, content));
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
                catch (Exception ex)
                {
                    // 连接中断了
                    InvokeDeviceStatuChangedEvent(new DeviceStautsChangedEventArgs(ex.Message));
                    break; // 退出本次接收线程
                }
                Thread.Sleep(1);
            }
        }
    }
}

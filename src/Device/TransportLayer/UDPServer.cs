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
    class UDPServer : DeviceBase
    {
        private UdpClient serverSocket;
        private readonly IPEndPoint localIpep;
        private IPEndPoint remoteIpep;
        private bool isKeepThreadAlive = false;
        private byte[] inBuffer;
        public UDPServer(string localIP, int localPort, int bufferSize)
        {
            localIpep = new IPEndPoint(IPAddress.Parse(localIP), localPort); // 本地端口监听
            remoteIpep = new IPEndPoint(IPAddress.Any, 0); 
            serverSocket = new UdpClient(localIpep);
            inBuffer = new byte[bufferSize];
        }
        public override void Open()
        {
            if (serverSocket != null)
            {
                isKeepThreadAlive = true;
                ThreadPool.QueueUserWorkItem(delegate
                {
                    clientSocket_DataReceived();
                });
                InvokeDeviceStatuChangedEvent(new DeviceStautsChangedEventArgs("UDP Server已开启。"));
            }
        }

        public override void Close()
        {
            if (serverSocket != null)
            {
                serverSocket.Close();
                isKeepThreadAlive = false;
                InvokeDeviceStatuChangedEvent(new DeviceStautsChangedEventArgs("UDP Server已关闭。"));
            }
        }

        public override void Write(byte[] content)
        {
            if (serverSocket != null)
            {
                try
                {
                    InvokeDeviceDataTransferedMessageEvent(new DeviceDataTransferedEventArgs (MessageType.Send, content));
                    serverSocket.Send(content, content.Length, remoteIpep);
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
                    inBuffer = serverSocket.Receive(ref remoteIpep);
                    if (inBuffer.Length > 0) //如果接收的消息为空 阻塞当前循环  
                    {
                        InvokeDeviceDataTransferedMessageEvent(new DeviceDataTransferedEventArgs (MessageType.Receive, inBuffer));
                    }
                }
                catch (Exception ex)
                {
                    InvokeDeviceStatuChangedEvent(new DeviceStautsChangedEventArgs(ex.Message));
                }
                Thread.Sleep(1);
            }
        }
    }
}

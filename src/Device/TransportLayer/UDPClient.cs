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
    class UDPClient : DeviceBase
    {
        private UdpClient clientSocket;
        private readonly System.Net.IPEndPoint localIpep;
        private System.Net.IPEndPoint remoteIpep;
        private bool isKeepThreadAlive = false;
        private byte[] inBuffer;
        public UDPClient(string localIP, int localPort, string remoteIP, int remotePort, int bufferSize)
        {
            localIpep = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(localIP), localPort); // 本地端口监听
            remoteIpep = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(remoteIP), remotePort); 
            clientSocket = new UdpClient(localIpep);
            inBuffer = new byte[bufferSize];
        }
        public override void Open()
        {
            if (clientSocket != null)
            {
                isKeepThreadAlive = true;
                ThreadPool.QueueUserWorkItem(delegate
                {
                    clientSocket_DataReceived();
                });
                InvokeDeviceStatuChangedEvent(new DeviceStautsChangedEventArgs("UDP Client已开启。"));
            }
        }

        public override void Close()
        {
            if (clientSocket != null)
            {
                clientSocket.Close();
                isKeepThreadAlive = false;
                InvokeDeviceStatuChangedEvent(new DeviceStautsChangedEventArgs("UDP Client已关闭。"));
            }
        }

        public override void Write(byte[] content)
        {
            if (clientSocket != null)
            {
                try
                {
                    InvokeDeviceDataTransferedMessageEvent(new DeviceDataTransferedEventArgs (MessageType.Send, content));
                    clientSocket.Send(content, content.Length, remoteIpep);
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
                    inBuffer = clientSocket.Receive(ref remoteIpep);
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

using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.IO.Ports;

namespace WYW.Modbus.Clients
{
    public class TCPClient : ClientBase
    {
        private Socket clientSocket;
        private readonly IPEndPoint ipep;
        private bool isKeepThreadAlive = false;
        private string serverIP;
        private readonly byte[] inBuffer = new byte[2048]; // socket接收的数据，临时变量

        public TCPClient(string ip, int port)
        {
            serverIP = ip;
            ipep = new IPEndPoint(IPAddress.Parse(ip), port);
            clientSocket = new Socket(ipep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
        }

        #region  公共方法
        public override void Open()
        {
            if (IsOpen)
                return;
            isKeepThreadAlive = true;
            if (clientSocket != null)
            {
                IsOpen = true;
                Task.Run(delegate
                {
                    while (isKeepThreadAlive)
                    {
                        Thread.Sleep(1000);
                        if (IsEstablished)
                        {
                            continue;
                        }
                        if (!TryPing(serverIP))
                        {
                            continue;
                        }
                        try
                        {
                            if (clientSocket == null)
                            {
                                clientSocket = new Socket(ipep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                            }
                            clientSocket.Connect(ipep);
                            IsEstablished = true;
                        }
                        // 目标主机未开启socket
                        catch (SocketException ex)
                        {
                            IsEstablished = false;
                        }
                        // 目标主机主动断开socket
                        catch (InvalidOperationException ex)
                        {
                            IsEstablished = false;
                            clientSocket.Close();
                            clientSocket = null;
                        }
                    }
                });
            }
        }

        public override void Close()
        {
            IsOpen = false;
            IsEstablished = false;
            isKeepThreadAlive = false;
            clientSocket?.Close();
        }
        public override void ClearReceiveBuffer()
        {
            if(clientSocket!=null)
            {
                if (!IsEstablished)
                {
                    return ;
                }
                try
                {
                    if (clientSocket.Poll(0, SelectMode.SelectRead))
                    {
                        clientSocket.Receive(inBuffer, inBuffer.Length, SocketFlags.None);
                    }
                }
                catch 
                {
                    IsEstablished = false;
                }
             
            }
        }
        public override bool Write(byte[] buffer)
        {
            if (!IsEstablished)
            {
                ErrorMessage = "Socket未建立连接";
                return false;
            }
            try
            {
                clientSocket.Send(buffer);
            }
            catch
            {
                IsEstablished = false;
                return false;
            }
            return true;
        }

        public override bool Read(ref List<byte> receiveBuffer)
        {
            if (!IsEstablished)
            {
                ErrorMessage = "Socket未建立连接";
                return false;
            }
            try
            {
                if (clientSocket.Poll(0, SelectMode.SelectRead))
                {
                    var receivedCount = clientSocket.Receive(inBuffer, inBuffer.Length, SocketFlags.None);
                    if (receivedCount > 0) //如果接收的消息为空 阻塞当前循环  
                    {
                        receiveBuffer.AddRange(inBuffer.Take(receivedCount));
                    }
                }
            }
            catch (Exception ex)
            {
                IsEstablished = false;
            }
            return true;
        }

        #endregion

        #region 私有函数
        private bool TryPing(string ip)
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    var replay = ping.Send(ip, 100);
                    if (replay != null && replay.Status == IPStatus.Success)
                    {
                        return true;
                    }
                }
            }
            catch
            {

            }

            return false;
        }
        #endregion
    }
}

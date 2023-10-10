using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace WYW.Communication.TransferLayer
{
    public class TCPClient : TransferBase
    {
        private Socket clientSocket;
        private readonly System.Net.IPEndPoint ipep;
        private readonly byte[] inBuffer;
        private string serverIP;
        

        public TCPClient(string ip, int port, int receiveBufferSize = 4096)
        {
            serverIP = ip;
            inBuffer = new byte[receiveBufferSize];
            ipep = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(ip), port);
            //将网络端点表示为IP地址和端口 用于socket侦听时绑定   
            clientSocket = new Socket(ipep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
        }

        #region 实现虚方法
        public override void Open()
        {
            if (IsOpen)
                return;
            if (clientSocket != null)
            {
                IsOpen = true;
                Task.Run(()=>
                {
                    while (IsOpen)
                    {
                        if(TryPing(serverIP))
                        {
                            try
                            {
                                if(clientSocket==null)
                                {
                                    clientSocket = new Socket(ipep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                                }
                                clientSocket.Connect(ipep);
                                OnStatusChanged($"Socket Client已建立连接。本地节点：{clientSocket.LocalEndPoint}");
                                IsEstablished = true;
                               
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
                                IsEstablished = false;
                                clientSocket.Close();
                                clientSocket = null;
                                OnStatusChanged($"远程主机主动断开连接。{ex.Message}");
                            }
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
            IsEstablished = IsOpen = false;              
            OnStatusChanged("Socket Client已主动关闭。");
        }

        public override void Write(byte[] content)
        {
            if (clientSocket != null)
            {
                try
                {
                    OnDataTransmited(content);
                    clientSocket.Send(content);
                }
                catch (Exception ex)
                {
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
                    if (clientSocket.Poll(0, SelectMode.SelectRead))
                    {
                        // 如果接收的消息为空 阻塞当前循环
                        var receivedCount = clientSocket.Receive(inBuffer, inBuffer.Length, SocketFlags.None);
                        if (receivedCount > 0)
                        {
                            OnDataReceived(inBuffer.Take(receivedCount).ToArray());
                        }
                    }
                }
                catch(ObjectDisposedException ex) 
                {
                    clientSocket=null;
                    OnStatusChanged($"接收数据异常，{ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    // 连接中断了
                    IsEstablished = false;
                    OnStatusChanged($"接收数据异常，{ex.Message}，{ex.GetType().Name}");
                    break; // 退出本次接收线程
                }
                Thread.Sleep(1);
            }
        }

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
    }
}

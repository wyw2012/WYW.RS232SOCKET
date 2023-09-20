using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows;
using MessageBox = WYW.UI.Controls.MessageBoxWindow;
using MessageControl = WYW.UI.Controls.MessageBoxControl;
using System.Net.Sockets;
using System.Windows.Controls;
using WYW.RS232SOCKET.Models;
using WYW.Communication;
using System.Net.NetworkInformation;
using System.Net;
using System.Reflection;

namespace WYW.RS232SOCKET.ViewModels
{
    internal class AuxiliaryToolViewModel : ViewModelBase
    {

        private CancellationTokenSource tokenSource;
        private delegate void ProgressCallback(double progress);
        public AuxiliaryToolViewModel()
        {
            string hostName = Dns.GetHostName();
            var ip = Dns.GetHostAddresses(hostName).Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).Select(x => x.ToString()).ToArray();

            foreach (var item in ip)
            {
                var array = item.Split('.');
                IPPrefixList.Add(string.Join(".", array.Take(3).ToArray()));
            }

        }
        protected override void BindingCommand()
        {
            QueryTimerResolutionCommand = new RelayCommand(QueryTimerResolution);
            CheckSleepEffectCommand = new RelayCommand(CheckSleepEffect);
            SetHighTimerResolutionCommand = new RelayCommand<ToggleButton>(SetHighTimerResolution);


            StopCommand = new RelayCommand(Stop);
            StartScanPortCommand = new RelayCommand(StartScanPort);
            StartPingCommand = new RelayCommand(StartPing);
            StartScanIPCommand = new RelayCommand(StartScanIP);

        }
        public DeviceController Controller { get; } = Ioc.Controller;

        public RelayCommand StopCommand { get; private set; }

        private void Stop()
        {
            tokenSource?.Cancel();
        }

        #region 时钟分辨率

        public RelayCommand QueryTimerResolutionCommand { get; private set; }
        public RelayCommand CheckSleepEffectCommand { get; private set; }
        public RelayCommand<ToggleButton> SetHighTimerResolutionCommand { get; private set; }

        private void QueryTimerResolution()
        {
            UInt32 minResolution, maxResolution, currentResolution;
            WinAPI.NtQueryTimerResolution(out maxResolution, out minResolution, out currentResolution);
            var message = $"最大计时间隔：{maxResolution / 10000.0}ms\r\n最小计时间隔：{minResolution / 10000.0}ms\r\n当前计时间隔：{currentResolution / 10000.0}ms\r\n";
            MessageBox.Success(message);
        }

        private void CheckSleepEffect()
        {
            Stopwatch timer = new Stopwatch();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 10; i++)
            {
                timer.Reset();
                timer.Start();
                Thread.Sleep(1);
                timer.Stop();
                sb.Append(Math.Round(timer.ElapsedTicks / 10000.0, 1).ToString("N1"));
                if (i < 9)
                {
                    sb.Append(", ");
                }

            }
            MessageBox.Success($"测试10组数据的延迟分别为：“{sb.ToString()}”ms");
        }

        private void SetHighTimerResolution(ToggleButton toggleButton)
        {
            if (toggleButton != null && toggleButton.IsChecked == true)
            {

                var result = WinAPI.NtSetTimerResolution(5000, true, out UInt32 currentResolution);
                if (result == 0)
                {
                    MessageBox.Success($"设置成功，当前时间精度为：{currentResolution / 10000.0}ms");
                }
                else
                {
                    MessageBox.Error($"设置失败，当前时间精度为：{currentResolution / 10000.0}ms");
                }
            }
            else
            {
                var result = WinAPI.NtSetTimerResolution(0, false, out UInt32 currentResolution);
                if (result == 0)
                {
                    MessageBox.Success($"停止成功，当前时间精度为：{currentResolution / 10000.0}ms");
                }
                else
                {
                    MessageBox.Success($"停止失败，当前时间精度为：{currentResolution / 10000.0}ms");
                }
            }
        }

        #endregion

        #region IP扫描
        private string scanIPPrefix;

        /// <summary>
        /// 
        /// </summary>
        public string ScanIPPrefix { get => scanIPPrefix; set => SetProperty(ref scanIPPrefix, value); }

        public List<string> IPPrefixList { get; set; } = new List<string>();


        private string scanIPResult;

        /// <summary>
        /// 
        /// </summary>
        public string ScanIPResult { get => scanIPResult; set => SetProperty(ref scanIPResult, value); }

        public RelayCommand StartScanIPCommand { get; private set; }


        private void StartScanIP()
        {
            if (string.IsNullOrEmpty(scanIPPrefix))
            {
                MessageBox.Error("请先选择一个IP地址段");
                return;
            }
            Task.Run(async () =>
            {
                try
                {
                    IsRunning = true;
                    ScanIPResult = "";
                    Controller.Config.Status.Progress = 0;
                    Controller.Config.Status.ProgressBarVisibility = Visibility.Visible;
                    tokenSource = new CancellationTokenSource();
                    DateTime dateTime = DateTime.Now;
                    List<int> onlineList=new List<int>();
                    for (int i = 0; i < 256; i++)
                    {
                        using (Ping ping = new Ping())
                        {
                            ping.PingCompleted += (s, e) =>
                            {
                                if (e.Reply.Status == IPStatus.Success)
                                {
                                    onlineList.Add(e.Reply.Address.ToString().Split('.').Select(x => int.Parse(x)).ToArray()[3]);
                                }
                            };
                            var replay =  ping.SendPingAsync($"{ScanIPPrefix}.{i}");
                        }
                    }
                    while((DateTime.Now-dateTime).TotalSeconds<4)
                    {
                        Controller.Config.Status.Progress = (DateTime.Now - dateTime).TotalSeconds*100 / 4;
                        Thread.Sleep(100);
                    }
                    Controller.Config.Status.Progress = 100;
                    ScanIPResult = string.Join(",",onlineList.OrderBy(x=>x));
                    MessageBox.Success($"扫描完成，在线主机数量{onlineList.Count}，共耗时{Math.Round((DateTime.Now - dateTime).TotalSeconds, 1)}秒");

                }
                catch (Exception ex)
                {
                    MessageBox.Error(ex.Message);
                }
                finally
                {
                    IsRunning = false;
                    Controller.Config.Status.Progress = 0;
                    Controller.Config.Status.ProgressBarVisibility = Visibility.Collapsed;
                }
            });
        }


        private bool TryPing(string ip)
        {
            using (Ping ping = new Ping())
            {
                var replay =  ping.Send(ip,2000);
                if (replay != null && replay.Status == IPStatus.Success)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region 端口扫描
        private string scanIP = "127.0.0.1";

        /// <summary>
        /// 
        /// </summary>
        public string ScanIP { get => scanIP; set => SetProperty(ref scanIP, value); }

        private int startPort = 1;

        /// <summary>
        /// 
        /// </summary>
        public int StartPort
        {
            get => startPort;
            set
            {
                if (value < 0)
                    value = 0;
                SetProperty(ref startPort, value);
            }
        }

        private int stopPort = 65535;

        /// <summary>
        /// 
        /// </summary>
        public int StopPort
        {
            get => stopPort;
            set
            {
                if (value > 65535)
                    value = 65535;
                SetProperty(ref stopPort, value);
            }
        }


        private string scanResult;

        /// <summary>
        /// 
        /// </summary>
        public string ScanResult { get => scanResult; set => SetProperty(ref scanResult, value); }

        private int scanTimeout=10;

        /// <summary>
        /// 扫描超时
        /// </summary>
        public int ScanTimeout { get => scanTimeout; set => SetProperty(ref scanTimeout, value); }

        public RelayCommand StartScanPortCommand { get; private set; }
        private void StartScanPort()
        {
            if (!ScanIP.IsIPV4())
            {
                MessageBox.Error("目标地址不是有效IP地址");
                return;
            }
            if (StartPort >= StopPort)
            {
                MessageBox.Error("起始端口号不能大于终止端口号");
                return;
            }
            if(!TryPing(scanIP))
            {
                MessageBox.Error("无法Ping通该IP地址");
                return;
            }
            Task.Run(() =>
            {
                try
                {
                    IsRunning = true;
                    ScanResult = "";
                    Controller.Config.Status.Progress = 0;
                    Controller.Config.Status.ProgressBarVisibility = Visibility.Visible;
                    tokenSource = new CancellationTokenSource();
                    DateTime dateTime = DateTime.Now;
                    List<int> onlineList = new List<int>();
                    // 为了节约时间进行分片处理，每片200个端口号
                    int sliceLength = 200;
                    var sliceCount = (stopPort - startPort) / sliceLength + 1;
                    Task<List<int>>[] tasks=new Task<List<int>>[sliceCount];

                    for (int i = 0; i < sliceCount; i++)
                    {
                        int remain = i == sliceCount - 1 ? (stopPort - startPort) % sliceLength + 1 : sliceLength;
                        var start = startPort + i * sliceLength;
                        var end = start + remain;
                        tasks[i] = TryScanPort(scanIP, start, end, ScanTimeout,tokenSource.Token, null);

                    }

                    //Task.Run(()=>
                    //{
                    //    while(!tokenSource.IsCancellationRequested)
                    //    {
                    //        Thread.Sleep(100);
                    //        if(tasks.Count(x=>x.IsCompleted)== tasks.Length)
                    //        {
                    //            break;
                    //        }
                    //        Controller.Config.Status.Progress = tasks.Count(x => x.IsCompleted)*100 / tasks.Length;
                    //    }
                    //});
                    Task.WaitAll(tasks);
                    foreach (var task in tasks)
                    {
                        onlineList.AddRange(task.Result);
                    }
                    ScanResult = string.Join(",", onlineList.OrderBy(x => x));
                    MessageBox.Success($"扫描完成，共耗时{Math.Round((DateTime.Now - dateTime).TotalSeconds, 1)}秒");

                }
                catch (Exception ex)
                {
                    MessageBox.Error(ex.Message);
                }
                finally
                {
                    IsRunning = false;
                    Controller.Config.Status.Progress = 0;
                    Controller.Config.Status.ProgressBarVisibility = Visibility.Collapsed;
                }
            });

        }
        private Task<List<int>> TryScanPort(string ip, int startPort, int stopPort, int timeout, CancellationToken token, ProgressCallback callback)
        {
            
            List<int> result = new List<int>();
            var task = Task.Run(() =>
             {
                 for (int i = startPort; i < stopPort; i++)
                 {
                     if (token == null || token.IsCancellationRequested)
                         break;
                     if (TryConnect(ip, i, timeout))
                     {
                         result.Add(i);
                     }
                     var progress = (i - startPort + 1) * 100.0 / (stopPort - startPort);
                     callback?.Invoke(progress);

                 }
                 return result;
             });
            return task;
        }
        private bool TryConnect(string ip, int port,int timeout=100)
        {
            try
            {
                var ipep = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(ip), port);
                using (var clientSocket = new Socket(ipep.AddressFamily, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp))
                {
                    return clientSocket.BeginConnect(ipep, null, null).AsyncWaitHandle.WaitOne(timeout, true);
                }
            }
            catch
            {

            }
            return false;
        }
        #endregion

        #region Ping测试
        private string pingIP = "192.168.1.1";

        /// <summary>
        /// 
        /// </summary>
        public string PingIP { get => pingIP; set => SetProperty(ref pingIP, value); }

        private int pingBytesLength = 32;

        /// <summary>
        /// 
        /// </summary>
        public int PingBytesLength
        {
            get => pingBytesLength;
            set
            {
                if (value > 65500)
                {
                    value = 65500;
                }
                SetProperty(ref pingBytesLength, value);
            }
        }

        private int pingCount = 1000;

        /// <summary>
        /// 
        /// </summary>
        public int PingCount { get => pingCount; set => SetProperty(ref pingCount, value); }

        private string pingResult;

        /// <summary>
        /// 
        /// </summary>
        public string PingResult { get => pingResult; set => SetProperty(ref pingResult, value); }

        public RelayCommand StartPingCommand { get; private set; }

        private void StartPing()
        {
            Task.Run(() =>
            {
                Ping ping = new Ping();
                PingReply pingReply = null;
                int totalResponse = 0;
                double totalDelay = 0;
                // 创建随机字符串
                var buffer = new byte[pingBytesLength];
                for (int i = 0; i < pingBytesLength; i++)
                {
                    buffer[i] = (byte)((int)'a' + i % 23);
                }
                try
                {
                    IsRunning = true;
                    PingResult = "";
                    Controller.Config.Status.Progress = 0;
                    Controller.Config.Status.ProgressBarVisibility = Visibility.Visible;
                    tokenSource = new CancellationTokenSource();
                    DateTime dateTime = DateTime.Now;
                    for (int i = 0; i < PingCount; i++)
                    {
                        if (tokenSource.IsCancellationRequested)
                        {
                            break;
                        }
                        pingReply = ping.Send(PingIP, 2000, buffer); // 2000ms超时
                        if (pingReply != null && pingReply.Status == IPStatus.Success)
                        {
                            totalResponse += 1;
                            totalDelay += pingReply.RoundtripTime;
                        }
                        else
                        {

                            PingResult += $"{pingReply?.Status}，";
                        }
                        Controller.Config.Status.Progress = (i + 1) * 100.0 / PingCount;
                        Thread.Sleep(1);
                    }
                    string message = $"共耗时：{Math.Round((DateTime.Now - dateTime).TotalSeconds, 1)}秒\r\n" +
                                     $"平均延迟：{Math.Round(totalDelay / totalResponse, 2)}ms\r\n" +
                                     $"丢包次数：{PingCount - totalResponse}\r\n" +
                                     $"丢包率：{((PingCount - totalResponse) / (double)PingCount):P2}";
                    PingResult = $"{message}";
                    MessageControl.Success("测试完成");

                }
                catch (Exception ex)
                {
                    MessageBox.Error(ex.Message);
                }
                finally
                {
                    ping?.Dispose();
                    IsRunning = false;
                    Controller.Config.Status.Progress = 0;
                    Controller.Config.Status.ProgressBarVisibility = Visibility.Collapsed;
                }
            });
        }

        #endregion
    }
}

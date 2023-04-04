using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WYW.RS232SOCKET.Models;
using WYW.UI.Controls;

namespace WYW.RS232SOCKET.ViewModels
{
    partial class MainWindowViewModel
    {

        private List<byte[]> sendQueue = new List<byte[]>(); // 循环发送队列，所有的发送数据必须加入发送队列

        #region 命令
        public RelayCommand ClearCommand { get; private set; }
        public RelayCommand SendCommand { get; private set; }
        public RelayCommand StopSendCommand { get; private set; }
        public RelayCommand OpenFileCommand { get; private set; }
        public RelayCommand<object> CopyTextCommand { get; private set; }

        private void Send()
        {
            try
            {

                CheckSendText();
                sendThread = new Thread(() =>
                {
                    Config.Status.IsSending = true;
                    do
                    {
                        if (!IsOpen || !Config.Status.IsSending)
                            break;
                        Config.Status.Progress = 0;
                        for (int i = 0; i < sendQueue.Count; i++)
                        {
                            if (!IsOpen || !Config.Status.IsSending)
                                break;
                            Device.SendBytes(sendQueue[i]);
                            Config.Status.Progress = ((i + 1) * 100) / sendQueue.Count;
                            if (i != sendQueue.Count - 1 || Config.Send.IsCyclic)
                            {
                                Thread.Sleep(Config.Send.CyclicInterval);
                            }

                        }
                    } while (Config.Send.IsCyclic);
                    Config.Status.IsSending = false;
                });
                sendThread.Start();
            }
            catch (Exception ex)
            {
                Config.Status.IsSending = false;
                MessageBoxWindow.Warning(ex.Message, "警告");
            }


        }
        private void StopSend()
        {
            Config.Status.IsSending = false;
        }
        private void Clear()
        {
            lock(displayLocker)
            {
                Config.Display.DisplayItems.Clear();

            }
            Config.Status.TotalReceived = 0;
            Config.Status.TotalSended = 0;
            Config.Status.Progress = 0;
        }
        private void OpenFile()
        {
            Config.Send.SendText = "";
            // 命令执行在IsChecked之后，所以根据改变后的状态进行判断
            if (!Config.Send.IsSendFile)
            {
                return;
            }

            var ofd = new OpenFileDialog
            {
                Filter = "文本文档|*.txt",
                FilterIndex = 1,
                RestoreDirectory = true,
            };
            if (ofd.ShowDialog() == true)
            {
                Config.Send.SendText = ofd.FileName;
            }
            else
            {
                Config.Send.IsSendFile = false;
            }
        }
        private void CopyText(object content)
        {
            if (content != null && content is IList items)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in items)
                {
                    sb.AppendLine(item.ToString());
                }
                Clipboard.SetDataObject(sb.ToString());
            }
        }

        #endregion

        #region 私有函数
        /// <summary>
        /// 验证发送文本是否满足格式要求，满足格式要求的文本加入到sendQueue中
        /// </summary>
        /// <exception cref="Exception"></exception>
        private void CheckSendText()
        {
            if (device == null)
            {
                throw new Exception("请先点击“开始”按钮。");
            }
            if (string.IsNullOrEmpty(Config.Send.SendText))
            {
                throw new Exception("发送栏无数据。");
            }
            sendQueue.Clear();
            string[] items;
            if (Config.Send.IsSendFile)
            {
                items = File.ReadAllLines(Config.Send.SendText);
            }
            else
            {
                items = Config.Send.SendText.Split(Environment.NewLine.ToCharArray(), options: StringSplitOptions.RemoveEmptyEntries);
            }

            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item))
                {
                    continue;
                }
                var temp = new List<byte>();
                switch (Config.Send.ProtocolType)
                {
                    case ProtocolType.Hex:
                        var chars = Regex.Replace(item, @"\s", ""); // 剔除空格
                        if (chars.Length % 2 == 1)
                        {
                            throw new Exception($"字符串长度需要是偶数");
                        }
                        var hexs = Regex.Split(chars, @"(?<=\G.{2})(?!$)");   // 两两分组
                        try
                        {
                            temp.AddRange(hexs.Select(x => Convert.ToByte(x, 16)).ToArray());
                        }
                        catch
                        {
                            throw new Exception($"字符串无法转换成十六进制，");
                        }
                        break;
                    case ProtocolType.UTF8:
                        temp.AddRange(Encoding.UTF8.GetBytes(item));
                        break;
                    case ProtocolType.UTF8_CR:
                        temp.AddRange(Encoding.UTF8.GetBytes(item));
                        temp.AddRange(Encoding.UTF8.GetBytes("\r"));
                        break;
                    case ProtocolType.UTF8_CRLF:
                        temp.AddRange(Encoding.UTF8.GetBytes(item));
                        temp.AddRange(Encoding.UTF8.GetBytes("\r\n"));
                        break;
                    case ProtocolType.UTF8_CheckSum:
                        temp.AddRange(Encoding.UTF8.GetBytes(item));
                        temp.Add((byte)temp.Sum(x => x));
                        break;
                }
                sendQueue.Add(temp.ToArray());
            }
        }
        #endregion
    }
}

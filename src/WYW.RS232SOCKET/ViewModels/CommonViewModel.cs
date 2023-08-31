using GemBox.Spreadsheet;
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
using WYW.Communication;
using WYW.RS232SOCKET.Models;
using WYW.UI.Controls;
using MessageBox = WYW.UI.Controls.MessageBoxWindow;
using MessageControl = WYW.UI.Controls.MessageBoxControl;
using WYW.Communication;

namespace WYW.RS232SOCKET.ViewModels
{
    partial class CommonViewModel : ViewModelBase
    {
        private CancellationTokenSource token = null;

        public CommonViewModel(DeviceController controller)
        {
            Controller = controller;
        }
        #region 属性
        public DeviceController Controller { get; }
        private bool isExpanded = true;

        /// <summary>
        /// 
        /// </summary>
        public bool IsExpanded { get => isExpanded; set => SetProperty(ref isExpanded, value); }

        #endregion

        #region 命令
        protected override void BindingCommand()
        {
            SendCommand = new RelayCommand(Send);
            StopSendCommand = new RelayCommand(StopSend);
            ClearCommand = new RelayCommand(Clear);
            OpenFileCommand = new RelayCommand(OpenFile);
            CopyTextCommand = new RelayCommand<object>(CopyText);
            OpenResponseFileCommand = new RelayCommand(OpenResponseFile);
            DownloadTemplateCommand = new RelayCommand(DownloadTemplate);
        }
        public RelayCommand ClearCommand { get; private set; }
        public RelayCommand SendCommand { get; private set; }
        public RelayCommand StopSendCommand { get; private set; }
        public RelayCommand OpenFileCommand { get; private set; }
        public RelayCommand<object> CopyTextCommand { get; private set; }
        public RelayCommand OpenResponseFileCommand { get; private set; }

        public RelayCommand DownloadTemplateCommand { get; private set; }
        private void Send()
        {
            MessageControl.Clear();
            Task.Run(() =>
            {
                try
                {
                    var sendQueue = CheckSendText();
                    if (sendQueue.Count > 0)
                    {
                        Controller.Status.ProgressBarVisibility = Visibility.Visible;
                    }
                    IsRunning = true;

                    token = new CancellationTokenSource();
                    do
                    {
                        for (int i = 0; i < sendQueue.Count; i++)
                        {
                            if (Controller.Device == null || !Controller.Device.Client.IsOpen)
                            {
                                return;
                            }
                            if (token?.Token.IsCancellationRequested == true)
                            {
                                MessageControl.Success("手动取消成功");
                                return;
                            }

                            Controller.Device.SendBytes(sendQueue[i]);
                            Controller.Status.Progress = ((i + 1) * 100) / sendQueue.Count;
                            if (i != sendQueue.Count - 1 || Controller.Send.IsCyclic)
                            {
                                Thread.Sleep(Controller.Send.CyclicInterval);
                            }

                        }
                    } while (Controller.Send.IsCyclic);
                }
                catch (Exception ex)
                {
                    MessageBoxWindow.Warning(ex.Message, "警告");
                }
                finally
                {
                    IsRunning = false;
                    Controller.Status.ProgressBarVisibility = Visibility.Collapsed;
                }
            }).ContinueWith(ProcessWhenTaskCompleted);




        }
        private void StopSend()
        {
            token?.Cancel();
        }
        private void Clear()
        {
            MessageControl.Clear();
            Controller.Clear();
        }
        private void OpenFile()
        {
            // 命令执行在IsChecked之后，所以根据改变后的状态进行判断
            if (!Controller.Send.IsSendFile)
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
                Controller.Send.SendText = ofd.FileName;
            }
            else
            {
                Controller.Send.IsSendFile = false;
            }
        }



        private void DownloadTemplate()
        {
            var ofd = new SaveFileDialog
            {
                Filter = "Excel|*.xlsx",
                FilterIndex = 1,
                RestoreDirectory = true,
                FileName = "应答模板"
            };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    DownloadResponseFile(ofd.FileName);
                    MessageControl.Success("下载成功");
                }
                catch (Exception ex)
                {
                    MessageBox.Error(ex.Message);
                }
            }
        }

        private void OpenResponseFile()
        {

            if (!Controller.Receive.IsAutoResponse)
            {
                return;
            }
            var ofd = new OpenFileDialog
            {
                Filter = "Excel|*.xlsx",
                FilterIndex = 1,
                RestoreDirectory = true,
            };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    Controller.Receive.Response.Clear();
                    ValicateResponseFile(ofd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBoxWindow.Error(ex.Message);
                    Controller.Receive.IsAutoResponse = false;
                }

            }
            else
            {
                Controller.Receive.IsAutoResponse = false;
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
        private List<byte[]> CheckSendText()
        {
            List<byte[]> sendQueue = new List<byte[]>();

            if (string.IsNullOrEmpty(Controller.Send.SendText))
            {
                throw new Exception("发送栏无数据。");
            }
            sendQueue.Clear();
            string[] items;
            if (Controller.Send.IsSendFile)
            {
                items = File.ReadAllLines(Controller.Send.SendText);
            }
            else
            {
                items = Controller.Send.SendText.Split(Environment.NewLine.ToCharArray(), options: StringSplitOptions.RemoveEmptyEntries);
            }

            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item))
                {
                    continue;
                }
                var temp = new List<byte>();
                switch (Controller.Send.ProtocolType)
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
                    case ProtocolType.UTF8_LF:
                        temp.AddRange(Encoding.UTF8.GetBytes(item));
                        temp.AddRange(Encoding.UTF8.GetBytes("\n"));
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
            return sendQueue;
        }
        private void DownloadResponseFile(string fileName)
        {
            SpreadsheetInfo.SetLicense("E02V-XUB1-52LA-994F");
            var ef = ExcelFile.Load(new MemoryStream(Properties.Resources.ResponseTemplate));
            ef.Save(fileName);
        }
        private void ValicateResponseFile(string fileName)
        {
            SpreadsheetInfo.SetLicense("E02V-XUB1-52LA-994F");
            var ef = ExcelFile.Load(fileName);
            var ws = ef.Worksheets[0];

            Controller.Receive.ProtocolType = (ProtocolType)Enum.Parse(typeof(ProtocolType), ws.Cells[1, 1].Value.ToString());

            if (ws.Rows.Count < 4)
            {
                throw new Exception("应答模板中至少需要一条应答记录");
            }
            for (int i = 3; i < ws.Rows.Count; i++)
            {


                var key = ws.Cells[i, 0].Value?.ToString();
                var value = ws.Cells[i, 1].Value?.ToString();
                if (string.IsNullOrEmpty(key))
                {
                    throw new Exception($"第{i + 1}行的“接收内容”不能为空");
                }
                if (string.IsNullOrEmpty(value))
                {
                    throw new Exception($"第{i + 1}行的“响应内容”不能为空");
                }

                var keyArray = new List<byte>();
                var valueArray = new List<byte>();
                switch (Controller.Receive.ProtocolType)
                {
                    case ProtocolType.Hex:
                        var chars = Regex.Replace(key, @"\s", ""); // 剔除空格
                        if (chars.Length % 2 == 1)
                        {
                            throw new Exception($"第{i + 1}行接收字符串长度需要是偶数");
                        }
                        var hexs = Regex.Split(chars, @"(?<=\G.{2})(?!$)");   // 两两分组
                        try
                        {
                            keyArray.AddRange(hexs.Select(x => Convert.ToByte(x, 16)).ToArray());
                        }
                        catch
                        {
                            throw new Exception($"第{i + 1}行接收字符串无法转换成十六进制，");
                        }

                        chars = Regex.Replace(value, @"\s", ""); // 剔除空格
                        if (chars.Length % 2 == 1)
                        {
                            throw new Exception($"第{i + 1}行应答字符串长度需要是偶数");
                        }
                        hexs = Regex.Split(chars, @"(?<=\G.{2})(?!$)");   // 两两分组
                        try
                        {
                            valueArray.AddRange(hexs.Select(x => Convert.ToByte(x, 16)).ToArray());
                        }
                        catch
                        {
                            throw new Exception($"第{i + 1}行应答字符串无法转换成十六进制，");
                        }
                        break;
                    case ProtocolType.UTF8:
                        keyArray.AddRange(Encoding.UTF8.GetBytes(key));
                        valueArray.AddRange(Encoding.UTF8.GetBytes(value));
                        break;
                    case ProtocolType.UTF8_CR:
                        keyArray.AddRange(Encoding.UTF8.GetBytes(key));
                        keyArray.AddRange(Encoding.UTF8.GetBytes("\r"));

                        valueArray.AddRange(Encoding.UTF8.GetBytes(value));
                        valueArray.AddRange(Encoding.UTF8.GetBytes("\r"));
                        break;
                    case ProtocolType.UTF8_LF:
                        keyArray.AddRange(Encoding.UTF8.GetBytes(key));
                        keyArray.AddRange(Encoding.UTF8.GetBytes("\n"));

                        valueArray.AddRange(Encoding.UTF8.GetBytes(value));
                        valueArray.AddRange(Encoding.UTF8.GetBytes("\n"));
                        break;
                    case ProtocolType.UTF8_CRLF:
                        keyArray.AddRange(Encoding.UTF8.GetBytes(key));
                        keyArray.AddRange(Encoding.UTF8.GetBytes("\r\n"));

                        valueArray.AddRange(Encoding.UTF8.GetBytes(value));
                        valueArray.AddRange(Encoding.UTF8.GetBytes("\r\n"));
                        break;
                    case ProtocolType.UTF8_CheckSum:
                        keyArray.AddRange(Encoding.UTF8.GetBytes(key));
                        keyArray.Add((byte)keyArray.Sum(x => x));

                        valueArray.AddRange(Encoding.UTF8.GetBytes(value));
                        valueArray.Add((byte)keyArray.Sum(x => x));
                        break;
                }


                if (!Controller.Receive.Response.ContainsKey(keyArray.ToHexString()))
                {

                    Controller.Receive.Response.Add(keyArray.ToHexString(), valueArray.ToArray());
                }

            }
        }
        #endregion
    }
}
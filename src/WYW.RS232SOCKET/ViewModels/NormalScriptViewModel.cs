using GemBox.Spreadsheet;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WYW.Communication;
using WYW.RS232SOCKET.Common;
using WYW.RS232SOCKET.Models;
using MessageBox = WYW.UI.Controls.MessageBoxWindow;
using MessageControl = WYW.UI.Controls.MessageBoxControl;

namespace WYW.RS232SOCKET.ViewModels
{
    internal class NormalScriptViewModel : ViewModelBase
    {
        private CancellationTokenSource token = null;
        public NormalScriptViewModel()
        {

        }

        #region 属性
        public DeviceController Controller { get; } = Ioc.Controller;
        private int selectedIndex;

        /// <summary>
        /// 
        /// </summary>
        public int SelectedIndex { get => selectedIndex; set => SetProperty(ref selectedIndex, value); }

        private ObservableCollection<NormalScriptModel> scriptItems = new ObservableCollection<NormalScriptModel>();

        /// <summary>
        /// 
        /// </summary>
        public ObservableCollection<NormalScriptModel> ScriptItems { get => scriptItems; set => SetProperty(ref scriptItems, value); }

        private int targetCircleCount = 1;

        /// <summary>
        /// 
        /// </summary>
        public int TargetCircleCount { get => targetCircleCount; set => SetProperty(ref targetCircleCount, value); }


        private TextProtocolType protocolType = TextProtocolType.UTF8;

        /// <summary>
        /// 
        /// </summary>
        public TextProtocolType ProtocolType { get => protocolType; set => SetProperty(ref protocolType, value); }


        private bool isExpanded = true;

        /// <summary>
        /// 
        /// </summary>
        public bool IsExpanded { get => isExpanded; set => SetProperty(ref isExpanded, value); }

        #endregion

        #region 命令

        protected override void BindingCommand()
        {
            StartCommand = new RelayCommand(Start);
            StopCommand = new RelayCommand(Stop);
            AddCommand = new RelayCommand(Add);
            DeleteCommand = new RelayCommand(Delete);
            OpenFileCommand = new RelayCommand(OpenFile);
            SaveFileCommand = new RelayCommand(SaveFile);
        }

        public RelayCommand StartCommand { get; private set; }
        public RelayCommand StopCommand { get; private set; }
        public RelayCommand AddCommand { get; private set; }
        public RelayCommand DeleteCommand { get; private set; }
        public RelayCommand OpenFileCommand { get; private set; }
        public RelayCommand SaveFileCommand { get; private set; }
        private void Start()
        {
            MessageControl.Clear();
            try
            {
                ValicateForm();
            }
            catch (Exception ex)
            {
                MessageBox.Warning(ex.Message);
                return;
            }
            if (MessageBox.Question("请确保参数配置正确且设备处于安全状态，点击“是”继续，点击“否”退出。") != MessageBoxResult.Yes)
            {
                return;
            }
            token = new CancellationTokenSource();

            Task.Run(() =>
            {
                try
                {
                    IsRunning = true;
                    Controller.Device.ProtocolType = (ProtocolType)((int)ProtocolType);
                    Controller.Config.Status.ProgressBarVisibility = Visibility.Visible;
                    for (int i = 0; i < TargetCircleCount; i++)
                    {
                        for (int j = 0; j < ScriptItems.Count; j++)
                        {
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }
                            var startTime = DateTime.Now;
                            SelectedIndex = j;
                            var result = Controller.Device.SendCommand(ScriptItems[j].Command, ScriptItems[j].IsNeedResponse);
                            if (ScriptItems[j].IsNeedResponse && result.IsSuccess)
                            {
                                ScriptItems[j].ResponseContent = result.Response.FriendlyText;
                            }
                            Controller.Config.Status.Progress = (j + 1) * 100.0 / ScriptItems.Count / TargetCircleCount + i * 100.0 / TargetCircleCount;
                            int remainTime = (int)(ScriptItems[j].SleepTime - (DateTime.Now - startTime).TotalMilliseconds);
                            if (remainTime > 0)
                            {
                                Thread.Sleep(remainTime);
                            }
                        }
                    }
                    MessageBox.Success("执行完成");
                }
                catch (Exception ex)
                {
                    MessageBox.Error(ex.Message);
                }
                finally
                {
                    IsRunning = false;
                    Controller.Config.Status.ProgressBarVisibility = Visibility.Collapsed;
                }

            }, token.Token).ContinueWith(ProcessWhenTaskCompleted);
        }
        private void Stop()
        {
            if (token == null)
            {
                return;
            }
            if (token.IsCancellationRequested)
            {
                if (IsRunning)
                {
                    MessageBox.Show("正在停止测试，请耐心等待流程执行完成。", "提示", isAutoClose: true, showDialog: false);
                }
                else
                {
                    MessageBox.Success("测试已停止", "提示");
                }
                return;
            }

            token.Cancel();
        }

        private void Add()
        {
            MessageControl.Clear();
            var parameter = new NormalScriptModel() { ID = ScriptItems.Count == 0 ? 1 : ScriptItems.Max(x => x.ID) + 1 };
            ScriptItems.Add(parameter);
        }

        private void Delete()
        {
            MessageControl.Clear();
            if (SelectedIndex == -1)
            {
                MessageBox.Warning("请先选择一条记录");
                return;
            }
            if (MessageBox.Question($"是否删除编号为{ScriptItems[SelectedIndex].ID}的记录？") == MessageBoxResult.Yes)
            {
                ScriptItems.RemoveAt(SelectedIndex);
            }
        }


        private void OpenFile()
        {
            MessageControl.Clear();
            var fd = new OpenFileDialog()
            {
                Filter = "Excel File (*.xlsx)|*.xlsx",  //  设置文件类型 
                FilterIndex = 1,                        // 设置默认文件类型显示顺序 
                RestoreDirectory = true,                // 保存对话框是否记忆上次打开的目录 
            };
            if (fd.ShowDialog() == true)
            {
                try
                {
                    var items = ImportFromExcel(fd.FileName);
                    ScriptItems.Clear();
                    foreach (var item in items)
                    {
                        ScriptItems.Add(item);
                    }
                    MessageControl.Success("加载成功");
                }
                catch (Exception ex)
                {
                    MessageControl.Error($"加载失败，原因：{ex.Message}");
                }
            }
        }

        private void SaveFile()
        {
            MessageControl.Clear();
            var sfd = new SaveFileDialog()
            {
                Filter = "Excel File (*.xlsx)|*.xlsx",  //  设置文件类型 
                FilterIndex = 1,                        // 设置默认文件类型显示顺序 
                RestoreDirectory = true,                // 保存对话框是否记忆上次打开的目录 
                FileName = $"脚本模板{DateTime.Now:yyyyMMdd}",
            };
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    ValicateForm();
                    ExportToExcel(sfd.FileName, ScriptItems);
                    MessageControl.Success("保存成功");
                }
                catch (Exception ex)
                {
                    MessageBox.Error(ex.Message);
                }
            }
        }

        #endregion

        #region  私有方法
        private void ValicateForm()
        {
            if (ScriptItems.Count == 0)
            {
                throw new Exception($"参数集合不能为空");
            }

            foreach (var item in ScriptItems)
            {
                if (string.IsNullOrEmpty(item.Command))
                {
                    throw new Exception($"编号为{item.ID}的指令内容不能为空");
                }
                if (protocolType == TextProtocolType.Hex)
                {
                    try
                    {
                        item.Command.ToHexArray();
                    }
                    catch
                    {
                        throw new Exception($"编号为{item.ID}的指令不符合{ProtocolType}协议");
                    }
                }

            }
        }

        private void ExportToExcel(string fileName, IEnumerable<NormalScriptModel> items)
        {
            SpreadsheetInfo.SetLicense(ExcelHelper.License);
            var ef = ExcelFile.Load(new MemoryStream(Properties.Resources.NormalScriptTemplate));
            var ws = ef.Worksheets[0];

            // 添加数据
            int index = 3;
            foreach (var item in items)
            {

                ws.Cells[index, 0].Value = item.ID;
                ws.Cells[index, 1].Value = item.IsNeedResponse;
                ws.Cells[index, 2].Value = item.Command;
                ws.Cells[index, 3].Value = item.SleepTime;
                index++;
            }
            ef.Save(fileName);
        }

        private IEnumerable<NormalScriptModel> ImportFromExcel(string fileName)
        {
            List<NormalScriptModel> list = new List<NormalScriptModel>();
            SpreadsheetInfo.SetLicense(ExcelHelper.License);
            var ef = ExcelFile.Load(fileName);
            var ws = ef.Worksheets[0];
            int count = ws.Rows.Count;
            ProtocolType = (TextProtocolType)Enum.Parse(typeof(TextProtocolType), ws.Cells[1, 1].Value.ToString());

            if (ws.Rows.Count < 4)
            {
                throw new Exception("应答模板中至少需要一条应答记录");
            }
            for (int i = 3; i < ws.Rows.Count; i++)
            {
                try
                {
                    NormalScriptModel parameter = new NormalScriptModel();
                    parameter.ID = int.Parse(ws.Cells[i, 0].Value?.ToString());
                    parameter.IsNeedResponse = bool.Parse(ws.Cells[i, 1].Value.ToString());
                    parameter.Command = ws.Cells[i, 2].Value.ToString();
                    parameter.SleepTime = int.Parse(ws.Cells[i, 3].Value.ToString());
                    list.Add(parameter);
                }
                catch (Exception ex)
                {
                    throw new Exception($"第{i + 1}行数据异常，{ex.Message}");
                }

            }

            return list;
        }

        #endregion
    }
}

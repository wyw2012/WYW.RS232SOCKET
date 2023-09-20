using GemBox.Spreadsheet;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WYW.Communication;
using WYW.Communication.Models;
using WYW.RS232SOCKET.Common;
using WYW.RS232SOCKET.Models;
using MessageBox = WYW.UI.Controls.MessageBoxWindow;
using MessageControl = WYW.UI.Controls.MessageBoxControl;

namespace WYW.RS232SOCKET.ViewModels
{
    internal class ModbusScriptViewModel : ViewModelBase
    {
        private CancellationTokenSource token = null;
        public ModbusScriptViewModel()
        {

        }

        #region 属性
        public DeviceController Controller { get; } = Ioc.Controller;
        private int selectedIndex;

        /// <summary>
        /// 
        /// </summary>
        public int SelectedIndex { get => selectedIndex; set => SetProperty(ref selectedIndex, value); }

        private ObservableCollection<ModbusScriptModel> scriptItems = new ObservableCollection<ModbusScriptModel>();

        /// <summary>
        /// 
        /// </summary>
        public ObservableCollection<ModbusScriptModel> ScriptItems { get => scriptItems; set => SetProperty(ref scriptItems, value); }

        private int targetCircleCount = 1;

        /// <summary>
        /// 
        /// </summary>
        public int TargetCircleCount { get => targetCircleCount; set => SetProperty(ref targetCircleCount, value); }


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
                    ModbusMaster master = Controller.Device as ModbusMaster;
                    Controller.Config.Status.ProgressBarVisibility = Visibility.Visible;
                    for (int i = 0; i < TargetCircleCount; i++)
                    {
                        for (int j = 0; j < ScriptItems.Count; j++)
                        {
                            ScriptItems[j].Status = "";
                            //if (ScriptItems[j].OperationType== OperationType.Read)
                            //{
                            //    ScriptItems[j].Value = "";
                            //}
                        }

                        for (int j = 0; j < ScriptItems.Count; j++)
                        {
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }
                            var startTime = DateTime.Now;
                            SelectedIndex = j;

                            var result = master.ReadWriteRegister(Controller.Config.Modbus.SlaveID, ScriptItems[j]);
                            if (result.IsSuccess)
                            {
                                ScriptItems[j].Status = "成功";
                            }
                            else
                            {
                                ScriptItems[j].Status = "失败";
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
            var parameter = new ModbusScriptModel() { ID = ScriptItems.Count == 0 ? 1 : ScriptItems.Max(x => x.ID) + 1 };
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
            ScriptItems.RemoveAt(SelectedIndex);

        }


        private void OpenFile()
        {
            MessageControl.Clear();
            var fd = new OpenFileDialog()
            {
                Filter = "Script File (*.script)|*.script",  //  设置文件类型 
                FilterIndex = 1,                        // 设置默认文件类型显示顺序 
                RestoreDirectory = true,                // 保存对话框是否记忆上次打开的目录 
            };
            if (fd.ShowDialog() == true)
            {
                try
                {
                    var items=  JsonHelper.ReadJson<IEnumerable<ModbusScriptModel>>(fd.FileName);
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
                Filter = "Script File (*.script)|*.script",  //  设置文件类型 
                FilterIndex = 1,                        // 设置默认文件类型显示顺序 
                RestoreDirectory = true,                // 保存对话框是否记忆上次打开的目录 
                FileName = $"Mobus脚本模板{DateTime.Now:yyyyMMdd}",
            };
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    ValicateForm();
                    JsonHelper.SaveJson(ScriptItems, sfd.FileName);
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

            Register.ValicateAddress(ScriptItems);
            Register.ValicateValue(ScriptItems);
        }



        #endregion
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using WYW.RS232SOCKET.Common;
using WYW.RS232SOCKET.Models;
using MessageBox = WYW.UI.Controls.MessageBoxWindow;
using MessageControl = WYW.UI.Controls.MessageBoxControl;
using Microsoft.Win32;
using WYW.Communication;
using WYW.Communication.Models;
using System.Threading;

namespace WYW.RS232SOCKET.ViewModels
{
    partial class ModbusViewModel : ViewModelBase
    {
        private CancellationTokenSource token = null;
        public ModbusViewModel()
        {
      
        }
        #region  属性
        public DeviceController Controller { get; } = Ioc.Controller;
        private bool isSelectAll = true;
        private int selectedIndex = -1;
        private bool isExpanded = true;
        private string registerStartAddress = "0";
        private int registerCount = 10;
        /// <summary>
        /// 是否全部选中
        /// </summary>
        public bool IsSelectAll
        {
            get => isSelectAll;
            set
            {
                SetProperty(ref isSelectAll, value);
                foreach (var item in Controller.RegisterCollection)
                {
                    item.IsChecked = value;
                }
            }
        }

        /// <summary>
        /// 当前选择的索引，用于删除寄存器
        /// </summary>
        public int SelectedIndex { get => selectedIndex; set => SetProperty(ref selectedIndex, value); }

        /// <summary>
        /// Expender控件是否扩展
        /// </summary>
        public bool IsExpanded { get => isExpanded; set => SetProperty(ref isExpanded, value); }

        /// <summary>
        /// 寄存器起始地址
        /// </summary>
        public string RegisterStartAddress { get => registerStartAddress; set => SetProperty(ref registerStartAddress, value); }

        /// <summary>
        /// 寄存器数量
        /// </summary>
        public int RegisterCount { get => registerCount; set => SetProperty(ref registerCount, value); }

        #endregion

        #region 命令

        protected override void BindingCommand()
        {
            CreateRegisterCommand = new RelayCommand(CreateRegister);
            AddRegisterCommand = new RelayCommand(AddRegister);
            DeleteRegisterCommand = new RelayCommand(DeleteRegister);
            LoadTemplateCommand = new RelayCommand(LoadTemplate);
            SaveTemplateCommand = new RelayCommand(SaveTemplate);
            ReadRegisterCommand = new RelayCommand(ReadRegister);
            WriteRegisterCommand = new RelayCommand(WriteRegister);
            StopReadWriteCommand = new RelayCommand(StopReadWrite);
        }

        public RelayCommand CreateRegisterCommand { get; private set; }
        public RelayCommand AddRegisterCommand { get; private set; }
        public RelayCommand DeleteRegisterCommand { get; private set; }
        public RelayCommand LoadTemplateCommand { get; private set; }
        public RelayCommand SaveTemplateCommand { get; private set; }
        public RelayCommand ReadRegisterCommand { get; private set; }
        public RelayCommand WriteRegisterCommand { get; private set; }
        public RelayCommand StopReadWriteCommand { get; private set; }

        private void CreateRegister()
        {
            MessageControl.Clear();
            if (RegisterCount < 1)
            {
                MessageBox.Warning("寄存器个数至少为1");
                return;
            }
            try
            {
                Controller.RegisterCollection.Clear();
                int startAddress = 0;
                if (RegisterStartAddress.StartsWith("0x"))
                {
                    startAddress = Convert.ToInt32(RegisterStartAddress, 16);
                }
                else
                {
                    startAddress = Convert.ToInt32(RegisterStartAddress, 10);
                }


                for (int i = 0; i < RegisterCount; i++)
                {
                    Controller.RegisterCollection.Add(new Register(i + startAddress));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Error(ex.Message);
            }

        }
        private void AddRegister()
        {
            var address = Controller.RegisterCollection.LastOrDefault()?.Address+1??1;

            Controller.RegisterCollection.Add(new Register(address));
        }
        private void DeleteRegister()
        {
            if (SelectedIndex > -1)
            {
                Controller.RegisterCollection.RemoveAt(SelectedIndex);
            }
            else
            {
                MessageBox.Warning("请先选中整条记录");
            }
        }
        private void LoadTemplate()
        {
            MessageControl.Clear();
            var ofd = new OpenFileDialog();
            ofd.Filter = "Excel File (*.xlsx)|*.xlsx"; //设置文件类型 
            ofd.FilterIndex = 1; //设置默认文件类型显示顺序 
            ofd.RestoreDirectory = true; //保存对话框是否记忆上次打开的目录 
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    Controller.RegisterCollection.Clear();
                    var index = ExcelHelper.FindSheetIndex(ofd.FileName, WYW.Communication.Properties.Resources.RegisterTemplate);

                    if (index == -1)
                    {
                        MessageBox.Error("加载的文件不符合模板，请使用符合该软件的模板");
                        return;
                    }
                    var table = ExcelHelper.ExcelToDataTable(ofd.FileName, index);

                    var items = Register.GetRegisters(table, false);
                    foreach (var item in items)
                    {
                        Controller.RegisterCollection.Add(item);
                    }
                    MessageControl.Success("加载成功");
                }
                catch (Exception ex)
                {
                    MessageBox.Error(ex.Message);
                    Controller.RegisterCollection.Clear();
                }
            }
        }
        private void SaveTemplate()
        {
            MessageControl.Clear();
            if (Controller.RegisterCollection.Count <= 0)
            {
                MessageBox.Error("寄存器数量为0，请先创建寄存器。");
                return;
            }
            try
            {
                Register.ValicateAddress(Controller.RegisterCollection);
            }
            catch (Exception ex)
            {
                MessageBox.Warning($"保存模板失败，{ex.Message}");
                return;
            }
            var sfd = new SaveFileDialog();
            sfd.Filter = "Excel File (*.xlsx)|*.xlsx"; //设置文件类型 
            sfd.FilterIndex = 1; //设置默认文件类型显示顺序 
            sfd.FileName = $"寄存器{DateTime.Now:yyyyMMdd}";
            sfd.RestoreDirectory = true; //保存对话框是否记忆上次打开的目录 
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    var template = Communication.Properties.Resources.RegisterTemplate;
                    ExcelHelper.DataTableToExcel(Register.ToDataTable(Controller.RegisterCollection), sfd.FileName, template);
                    MessageControl.Success("保存成功");
                }
                catch (Exception ex)
                {
                    MessageBox.Error(ex.Message);
                }
            }
        }
        private void ReadRegister()
        {
            MessageControl.Clear();
            Controller.Clear();
            Task.Run(() =>
            {
                token = new CancellationTokenSource();
                IsRunning = true;
                if (Controller.Config.Send.IsCyclic)
                {
                    IsExpanded = false;
                }

                ModbusMaster master = Controller.Device as ModbusMaster;
                do
                {
                    if (Controller.Device == null || !Controller.Device.Client.IsOpen)
                    {
                        MessageControl.Warning("设备已关闭");
                        return;
                    }
                    if (token?.Token.IsCancellationRequested == true)
                    {
                        MessageControl.Success("手动取消成功");
                        return;
                    }
                    try
                    {
                        Controller.Config.Display.LastReceive = "";
                        master.ReadRegisterCollection(Controller.Config.Modbus.SlaveID, Controller.RegisterCollection, Controller.Config.Send.ResponseTimeout, token, Controller.Config.Send.IsCyclic);
                        if (!Controller.Config.Send.IsCyclic)
                        {
                            MessageControl.Success("读寄存器成功");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageControl.Error(ex.Message);
                    }


                    Thread.Sleep(Controller.Config.Send.CyclicInterval);


                }
                while (Controller.Config.Send.IsCyclic);

            }).ContinueWith(ProcessWhenTaskCompleted);
        }

        private void WriteRegister()
        {
            MessageControl.Clear();
            Controller.Clear();

            Task.Run(() =>
            {
                token = new CancellationTokenSource();
                IsRunning = true;
                if (Controller.Config.Send.IsCyclic)
                {
                    IsExpanded = false;
                }

                ModbusMaster master = Controller.Device as ModbusMaster;

                do
                {
                    if (Controller.Device == null || !Controller.Device.Client.IsOpen)
                    {
                        MessageControl.Warning("设备已关闭");
                        return;
                    }
                    if (token?.Token.IsCancellationRequested == true)
                    {
                        MessageControl.Success("手动取消成功");
                        return;
                    }
                    try
                    {
                        Controller.Config.Display.LastReceive = "";
                        master.WriteRegisterCollection(Controller.Config.Modbus.SlaveID, Controller.RegisterCollection, Controller.Config.Send.ResponseTimeout, Controller.Config.Modbus.IsSupportMultiWriteCommand, token, Controller.Config.Send.IsCyclic);
                        if (!Controller.Config.Send.IsCyclic)
                        {
                            MessageControl.Success("写寄存器成功");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageControl.Error(ex.Message);
                    }

                    Thread.Sleep(Controller.Config.Send.CyclicInterval);

                }
                while (Controller.Config.Send.IsCyclic);

            }).ContinueWith(ProcessWhenTaskCompleted);

        }

        private void StopReadWrite()
        {
            token?.Cancel();
        }
        #endregion

        protected override void ProcessWhenTaskCompleted(Task task)
        {
            IsExpanded = true;
            base.ProcessWhenTaskCompleted(task);
        }
    }
}

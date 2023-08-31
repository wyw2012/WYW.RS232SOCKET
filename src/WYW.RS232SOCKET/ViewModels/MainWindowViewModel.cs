using System;
using WYW.RS232SOCKET.Models;
using WYW.UI.Controls;
using WYW.Communication.TransferLayer;
using WYW.Communication.Protocol;
using System.Threading;
using System.Windows;
using WYW.Communication;
using System.Linq;
using System.Collections.Generic;
using MessageControl = WYW.UI.Controls.MessageBoxControl;

namespace WYW.RS232SOCKET.ViewModels
{
    partial class MainWindowViewModel : ViewModelBase
    {

        private List<byte[]> sendQueue = new List<byte[]>(); // 循环发送队列，所有的发送数据必须加入发送队列

        public MainWindowViewModel()
        {
            Controller = new DeviceController();
            ModbusViewModel = new ModbusViewModel(Controller);
            CommonViewModel = new CommonViewModel(Controller);
        }

        #region 属性
        public DeviceController Controller { get; }
        public ModbusViewModel ModbusViewModel { get; }

        public CommonViewModel CommonViewModel { get; }
        #endregion

        #region 命令

        protected override void BindingCommand()
        {
            OpenCommand = new RelayCommand(Open);
            CloseCommand = new RelayCommand(Close);
        }

        public RelayCommand OpenCommand { get; private set; }
        public RelayCommand CloseCommand { get; private set; }

        private void Open()
        {
            MessageControl.Clear();
            try
            {
                Controller.Open();
            }
            catch (Exception ex)
            {
                MessageBoxWindow.Error(ex.Message, "错误");
            }
        }

        private void Close()
        {
            MessageControl.Clear();
            try
            {
                Controller.Close();
            }
            catch (Exception ex)
            {
                MessageBoxWindow.Error(ex.Message, "错误");
            }
        }

        #endregion
    }
}

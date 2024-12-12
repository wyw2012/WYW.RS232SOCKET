using System;
using WYW.RS232SOCKET.Models;
using WYW.UI.Controls;
using System.Collections.Generic;
using MessageBox = WYW.UI.Controls.MessageBoxWindow;
using MessageControl = WYW.UI.Controls.MessageBoxControl;
using System.Windows.Controls.Primitives;
using System.Windows;
using System.Diagnostics;
using System.Threading;
using System.Text;

namespace WYW.RS232SOCKET.ViewModels
{
    partial class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            Ioc.Controller = new DeviceController();
            Controller= Ioc.Controller;
            ModbusViewModel = new ModbusViewModel();
            CommonViewModel = new CommonViewModel();
            AuxiliaryToolViewModel=new AuxiliaryToolViewModel();
            ProtocolConvertViewModel = new ProtocolConvertViewModel();
            NormalScriptViewModel = new NormalScriptViewModel();
            ModbusScriptViewModel=new ModbusScriptViewModel();
            NumberConvertViewModel=new NumberConvertViewModel();
        }

        #region 属性
        public DeviceController Controller { get; }
        public ModbusViewModel ModbusViewModel { get; }
        public CommonViewModel CommonViewModel { get; }
        public AuxiliaryToolViewModel AuxiliaryToolViewModel { get; }
        public ProtocolConvertViewModel ProtocolConvertViewModel { get; }
        public NormalScriptViewModel NormalScriptViewModel { get; }

        public ModbusScriptViewModel ModbusScriptViewModel { get; }
        public NumberConvertViewModel NumberConvertViewModel { get; }
        #endregion

        #region 命令

        protected override void BindingCommand()
        {
            OpenCommand = new RelayCommand(Open);
            CloseCommand = new RelayCommand(Close);
            QueryTimerResolutionCommand = new RelayCommand(QueryTimerResolution);
            CheckSleepEffectCommand = new RelayCommand(CheckSleepEffect);
            SetHighTimerResolutionCommand = new RelayCommand<ToggleButton>(SetHighTimerResolution);
        }

        public RelayCommand OpenCommand { get; private set; }
        public RelayCommand CloseCommand { get; private set; }
        public RelayCommand QueryTimerResolutionCommand { get; private set; }
        public RelayCommand CheckSleepEffectCommand { get; private set; }
        public RelayCommand<ToggleButton> SetHighTimerResolutionCommand { get; private set; }
        private void Open()
        {
            MessageControl.Clear();
            try
            {
                Controller.Open();
                Controller.Device.IsHighAccuracyTimer = Controller.IsHighAccuracyTimer;
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
    }
}

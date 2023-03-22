using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using WYW.UI.Controls;

namespace WYW.RS232SOCKET
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
       
        protected override void OnStartup(StartupEventArgs e)
        {
            uint minResolution, maxResolution, currentResolution;
            WinAPI.NtQueryTimerResolution(out maxResolution, out minResolution, out currentResolution);
            if (currentResolution > 10000)//如果定时器最小分辨率大于1ms
            {
                var result = WinAPI.NtSetTimerResolution(5000, true, out currentResolution); // 设置精度为0.5ms
                if(result!=0)
                {
                    Debug.WriteLine($"设置时间精度失败，当前时间精度为：{currentResolution / 10000.0}ms");
                }
            }
            this.DispatcherUnhandledException += App_DispatcherUnhandledException; // UI线程
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException; // 非UI线程
            base.OnStartup(e);
        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                MessageBoxWindow.Error(e.Exception.Message);
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBoxWindow.Error(ex.Message);
            }
        }

        // 非UI线程只写日志，不弹出对话框
        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                if (e.ExceptionObject is Exception ex)
                {
                    MessageBoxWindow.Error(ex.Message);
                }
            }
            catch (Exception ex)
            {
                MessageBoxWindow.Error(ex.Message);
            }
        }

    }
}

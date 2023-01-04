﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace WYW.RS232SOCKET
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
       
        protected override void OnStartup(StartupEventArgs e)
        {
            //AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            //{
            //    Console.WriteLine($"**************{args.Name}");
            //    string resourceName = $"WYW.RS232SOCKET.Lib.WYW.UI.dll";
            //    using (var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            //    {
            //        byte[] buffer = new byte[stream.Length];
            //        stream.Read(buffer, 0, buffer.Length);
            //        return Assembly.Load(buffer);
            //    }
            //};
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
            base.OnStartup(e);
        }
    }
}

﻿using System.Runtime.InteropServices;

namespace WYW.RS232SOCKET
{
    class WinAPI
    {
        /// <summary>
        /// 获取定时器精度
        /// </summary>
        /// <param name="MaximumResolution">最大分辨率，单位100ns</param>
        /// <param name="MinimumResolution">最小分辨率，单位100ns</param>
        /// <param name="ActualResolution">当前分辨率，单位100ns</param>
        /// <returns></returns>
        [DllImport("NTDLL.dll", SetLastError = true)]
        public static extern int NtQueryTimerResolution(out uint MaximumResolution, out uint MinimumResolution, out uint ActualResolution);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="RequestedResolution">The desired timer resolution. Must be within the legal range of system timer values supported by NT. On standard x86 systems this is 1-10 milliseconds. Values that are within the acceptable range are rounded to the next highest millisecond boundary by the standard x86 HAL. This parameter is ignored if the Set parameter is FALSE.</param>
        /// <param name="SetResolution">This is TRUE if a new timer resolution is being requested, and FALSE if the application is indicating it no longer needs a previously implemented resolution.</param>
        /// <param name="ActualResolution">The timer resolution in effect after the call is returned in this parameter.</param>
        /// <returns></returns>
        [DllImport("NTDLL.dll", SetLastError = true)]
        public static extern int NtSetTimerResolution(uint RequestedResolution, bool SetResolution, out uint ActualResolution);

        /// <summary>
        /// 用于得到高精度计时器（如果存在这样的计时器）的值。如果安装的硬件不支持高精度计时器,函数将返回false。
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll ")]
        public static extern bool QueryPerformanceCounter(out long tick);
        /// <summary>
        /// 返回硬件支持的高精度计数器的频率，如果安装的硬件不支持高精度计时器,函数将返回false。
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll ")]
        public static extern bool QueryPerformanceFrequency(out long tick);

        /// <summary>
        /// 请求高精度计时器
        /// </summary>
        /// <param name="t">时间精度，单位ms</param>
        /// <returns>0成功</returns>
        [DllImport("winmm")]
        public static extern int timeBeginPeriod(int t);

        /// <summary>
        /// 停止高精度计时器
        /// </summary>
        /// <param name="t">时间精度，单位ms</param>
        /// <returns>0成功</returns>
        [DllImport("winmm")]
        public static extern uint timeEndPeriod(int t);

    }
}

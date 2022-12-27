using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WYW.RS232SOCKET.Events
{
    /// <summary>
    /// 设备状态通知
    /// </summary>
    class DeviceStautsChangedEventArgs:EventArgs
    {
        public DeviceStautsChangedEventArgs(string message)
        {
            CreateTime = DateTime.Now;
            Message = message;
        }
        public DateTime CreateTime { get; }
        public string Message { get; }

        public override string ToString()
        {
            return $"[{CreateTime:HH:mm:ss.fff}] [MSG] {Message}";
        }
    }
}

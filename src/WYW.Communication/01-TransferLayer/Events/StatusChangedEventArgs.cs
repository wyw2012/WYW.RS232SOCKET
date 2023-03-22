using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WYW.Communication.TransferLayer
{
    /// <summary>
    /// 状态改变对象
    /// </summary>
    public class StatusChangedEventArgs:EventArgs
    {
        public StatusChangedEventArgs(string message)
        {
            Message= message;
        }
        public string Message { get; }
        public DateTime CreateTime { get; } = DateTime.Now;

        public override string ToString()
        {
            return $"[{CreateTime:yyyy-MM-dd HH:mm:ss.fff}] {Message}";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WYW.Communication.TransferLayer
{
    public class DataTransmitedEventArgs : EventArgs
    {
        public DataTransmitedEventArgs(byte[] data)
        {
            Data = data;
        }
        public byte[] Data { get; }
        public DateTime CreateTime { get; } = DateTime.Now;

        public override string ToString()
        {
            return $"[{CreateTime:yyyy-MM-dd HH:mm:ss.fff}] [Tx] {Data.ToHexString()}";
        }
        public string ToUTF8()
        {
            return $"[{CreateTime:yyyy-MM-dd HH:mm:ss.fff}] [Tx] {Data.ToUTF8()}";
        }
    }
}

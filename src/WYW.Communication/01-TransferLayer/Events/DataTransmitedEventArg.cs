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
        /// <summary>
        /// 设备标识符，用于区分设备
        /// </summary>
        internal string DeviceID { get; set; }
        public byte[] Data { get; }
        public DateTime CreateTime { get; } = DateTime.Now;

        public override string ToString()
        {
            if (DeviceID == null)
            {
                return $"[{CreateTime:yyyy-MM-dd HH:mm:ss.fff}] [Tx] {Data.ToHexString()}";
            }
            else
            {
                return $"[{CreateTime:yyyy-MM-dd HH:mm:ss.fff}] [Tx] [{DeviceID}] {Data.ToHexString()}";
            }
        }
        public string ToUTF8()
        {
            return $"[{CreateTime:yyyy-MM-dd HH:mm:ss.fff}] [Tx] {Data.ToUTF8()}";
        }
    }
}

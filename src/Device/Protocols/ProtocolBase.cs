using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WYW.RS232SOCKET.Protocol
{
    abstract class ProtocolBase
    {
        #region  属性
        /// <summary>
        /// 协议帧的完整字节，包含帧头、帧尾、校验位等
        /// </summary>
        public byte[] FullBytes { get; protected set; }
        /// <summary>
        /// 协议帧的有效内容部分，各个协议的定义不同
        /// </summary>
        public byte[] Content { get; protected set; }
        public string FriendlyText { get; protected set; }

        public byte NodeID { get; protected set; }

        public ModbusCommand Command { get; protected set; }
        #endregion

        #region  内部成员
        /// <summary>
        /// 指令的标识符，用于验证发送的指令与接收的指令是否匹配
        /// </summary>
        internal string Tag { get; set; }

        internal DateTime CreateTime { get; } = DateTime.Now;

        /// <summary>
        /// 比较两个对象的标识符是否一致
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal bool IsMatch(ProtocolBase obj)
        {
            if(Tag==obj.Tag)
            {
                return true;
            }
            return false;
        }
        protected byte[] GetBytesBigEndian(UInt16 value)
        {
            byte[] result = new byte[2];
            result[0] = (byte)(value >> 8);
            result[1] = (byte)(value & 0xFF);
            return result;
        }

        protected UInt16 GetUInt16BigEndian(byte[] value, int startIndex = 0)
        {
            int result = 0;
            if (value.Length + startIndex >= 2)
            {
                result = (value[startIndex] << 8) + value[startIndex + 1];
            }
            return (UInt16)result;
        }
        #endregion

    }
}

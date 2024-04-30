using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WYW.Modbus.Protocols
{
    /// <summary>
    /// 协议基类
    /// </summary>
 public   abstract class ProtocolBase
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
        /// <summary>
        /// 用于显示的字符内容
        /// </summary>
        public string FriendlyText { get; protected set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; internal set; } =DateTime.Now;
        #endregion

        #region  内部成员
        /// <summary>
        /// 指令的标识符，用于验证发送的指令与接收的指令是否匹配，如果为null，则表示接收到了异常帧
        /// </summary>
        internal string Tag { get; set; }=String.Empty;

        internal abstract int MinLength { get; }
        internal abstract List<ProtocolBase> GetResponse(List<byte> buffer);
        /// <summary>
        /// 比较两个对象的标识符是否一致
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal  bool IsMatch(ProtocolBase obj)
        {
            if (obj.Tag==null)
            {
                return true;
            }
            if (Tag==obj.Tag)
            {
                return true;
            }
            return false;
        }
        #endregion

        /// <summary>
        /// FullBytes以十六进制字符串显示
        /// </summary>
        /// <returns></returns>
        public string ToHexString()
        {
            return FullBytes.ToHexString();
        }

    }
}

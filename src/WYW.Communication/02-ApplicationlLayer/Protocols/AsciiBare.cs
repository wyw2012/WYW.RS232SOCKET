using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WYW.Communication.Protocol
{
    /// <summary>
    /// 无任何头、尾以及校验位的ASCII字符串
    /// </summary>
    public class AsciiBare : ProtocolBase
    {
        #region 子类必须实现的
        /// <summary>
        /// 用于发送
        /// </summary>
        /// <param name="command"></param>
        public AsciiBare(string command)
        {
            FullBytes = Encoding.UTF8.GetBytes(command);
            Content = Encoding.UTF8.GetBytes(command);
            FriendlyText = command;
        }
        /// <summary>
        /// 用于接收
        /// </summary>
        /// <param name="fullBytes"></param>
        internal AsciiBare(byte[] fullBytes)
        {
            FullBytes = fullBytes;
            Content = fullBytes;
            FriendlyText = Encoding.UTF8.GetString(Content);
        }
        internal static List<ProtocolBase> Analyse(List<byte> buffer)
        {
            List<ProtocolBase> result = new List<ProtocolBase>();
            result.Add(new AsciiBare(buffer.ToArray()));
            buffer.Clear();
            return result;
        }

        #endregion
    }
}

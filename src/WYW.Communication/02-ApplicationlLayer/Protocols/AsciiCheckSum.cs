using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WYW.Communication.Protocol
{
    /// <summary>
    /// 最后字节为校验位的ASCII协议
    /// </summary>
    public class AsciiCheckSum : ProtocolBase
    {
        #region 子类必须实现的
        private static int MinLength = 2;
        /// <summary>
        /// 用于发送
        /// </summary>
        /// <param name="command"></param>
        public AsciiCheckSum(string command)
        {
            List<byte> list = new List<byte>();
            list.AddRange(Encoding.UTF8.GetBytes(command));
            list.Add((byte)list.Sum(x=>x));
            FullBytes = list.ToArray();
            Content = Encoding.UTF8.GetBytes(command);
            FriendlyText = command;
        }
        /// <summary>
        /// 用于接收
        /// </summary>
        /// <param name="fullBytes"></param>
        internal AsciiCheckSum(byte[] fullBytes)
        {
            FullBytes = fullBytes;
            Content = fullBytes.SubBytes(0, fullBytes.Length - 1);
            FriendlyText = Encoding.UTF8.GetString(Content);
        }
        public static List<ProtocolBase> Analyse(List<byte> buffer)
        {
            List<ProtocolBase> result = new List<ProtocolBase>();
            if(buffer.Count< MinLength)
            {
                return result;
            }
            int startIndex = 0;
            for (int i = 1; i < buffer.Count; i++)
            {
                if (buffer[i] == (byte)buffer.Take(i-startIndex).Sum(x=>x))
                {
                    int length = i + 1 - startIndex;
                    var fullPacket = new byte[length];
                    buffer.CopyTo(startIndex, fullPacket, 0, length);
                    result.Add(new AsciiCheckSum(fullPacket));
                    buffer.RemoveRange(startIndex, length);
                    if (buffer.Count < MinLength)
                    {
                        break;
                    }
                    i = startIndex - 1; // 减一的目的是保证i=i;
                }
            }
            return result;
        }

        #endregion
    }
}

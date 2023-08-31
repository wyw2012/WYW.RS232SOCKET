using System.Collections.Generic;
using System.Text;

namespace WYW.Communication.Protocol
{
    /// <summary>
    /// 带回车的ASCII协议
    /// </summary>
    public class AsciiCR : ProtocolBase
    {
        #region 子类必须实现的
        private static int MinLength = 2;
        private static byte ProtocolTrail = 0x0D;
        /// <summary>
        /// 用于发送
        /// </summary>
        /// <param name="command"></param>
        public AsciiCR(string command)
        {
            List<byte> list = new List<byte>();
            list.AddRange(Encoding.UTF8.GetBytes(command));
            list.Add(ProtocolTrail);
            FullBytes = list.ToArray();
            Content = Encoding.UTF8.GetBytes(command);
            FriendlyText = command;
        }
        /// <summary>
        /// 用于接收
        /// </summary>
        /// <param name="fullBytes"></param>
        internal AsciiCR(byte[] fullBytes)
        {
            FullBytes = fullBytes;
            Content = fullBytes.SubBytes(0, fullBytes.Length - 1);
            FriendlyText = Encoding.UTF8.GetString(Content);
        }
        public static List<ProtocolBase> Analyse(List<byte> buffer)
        {
            List<ProtocolBase> result = new List<ProtocolBase>();

            int startIndex = 0;
            for (int i = 0; i < buffer.Count; i++)
            {
                if (buffer[i] == ProtocolTrail)
                {
                    int length = i + 1 - startIndex;
                    var fullPacket = new byte[length];
                    buffer.CopyTo(startIndex, fullPacket, 0, length);
                    result.Add(new AsciiCR(fullPacket));
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

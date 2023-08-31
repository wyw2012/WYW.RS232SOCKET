using System.Collections.Generic;
using System.Text;

namespace WYW.Communication.Protocol
{
    /// <summary>
    /// 带回车换行的ASCII协议
    /// </summary>
    public class AsciiCRLF : ProtocolBase
    {
        #region 子类必须实现的
        private static int MinLength = 3;
        private static byte[] ProtocolTrail = { 0x0D, 0x0A };
        /// <summary>
        /// 用于发送
        /// </summary>
        /// <param name="command"></param>
        public AsciiCRLF(string command)
        {
            List<byte> list = new List<byte>();
            list.AddRange(Encoding.UTF8.GetBytes(command));
            list.AddRange(ProtocolTrail);
            FullBytes = list.ToArray();
            Content = Encoding.UTF8.GetBytes(command);
            FriendlyText = command;
        }
        /// <summary>
        /// 用于接收
        /// </summary>
        /// <param name="fullBytes"></param>
        internal AsciiCRLF(byte[] fullBytes)
        {
            FullBytes = fullBytes;
            Content = fullBytes.SubBytes(0, fullBytes.Length - 2);
            FriendlyText = Encoding.UTF8.GetString(Content);
        }
        public static List<ProtocolBase> Analyse(List<byte> buffer)
        {
            List<ProtocolBase> result = new List<ProtocolBase>();

            int startIndex = 0;
            for (int i = 0; i < buffer.Count - 1; i++)
            {
                if (buffer[i] == ProtocolTrail[0] && buffer[i + 1] == ProtocolTrail[1])
                {
                    int length = i + 2 - startIndex;
                    var fullPacket = new byte[length];
                    buffer.CopyTo(startIndex, fullPacket, 0, length);
                    result.Add(new AsciiCRLF(fullPacket));
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

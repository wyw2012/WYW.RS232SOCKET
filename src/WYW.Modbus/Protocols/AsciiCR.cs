using System.Collections.Generic;
using System.Text;

namespace WYW.Modbus.Protocols
{
    internal class AsciiCR : ProtocolBase
    {

        private byte ProtocolTrail = 0x0D;
        public AsciiCR(string command)
        {
            List<byte> list = new List<byte>();
            list.AddRange(Encoding.UTF8.GetBytes(command));
            list.Add(ProtocolTrail);
            FullBytes = list.ToArray();
            Content = Encoding.UTF8.GetBytes(command);
            FriendlyText = command;
        }
        public AsciiCR(byte[] fullBytes)
        {
            FullBytes = fullBytes;
            Content = fullBytes.SubBytes(0, fullBytes.Length - 1);
            FriendlyText = Encoding.UTF8.GetString(Content); 
        }

        internal override int MinLength => 2;

        internal override List<ProtocolBase> GetResponse(List<byte> buffer)
        {
            List<ProtocolBase> list = new List<ProtocolBase>();
            int startIndex = 0;
            for (int i = 0; i < buffer.Count; i++)
            {
                if (buffer[i] == ProtocolTrail)
                {
                    int length = i + 1 - startIndex;
                    var fullPacket = new byte[length];
                    buffer.CopyTo(startIndex, fullPacket, 0, length);
                    list.Add(new AsciiCR(fullPacket));
                    buffer.RemoveRange(startIndex, length);
                    if (buffer.Count < MinLength)
                    {
                        break;
                    }
                    i = startIndex - 1; // 减一的目的是保证i=i;
                }
            }
            return list;
        }
    }
}

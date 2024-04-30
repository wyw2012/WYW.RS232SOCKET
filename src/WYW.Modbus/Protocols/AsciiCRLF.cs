using System.Collections.Generic;
using System.Text;

namespace WYW.Modbus.Protocols
{
    internal class AsciiCRLF : ProtocolBase
    {
        private byte[] ProtocolTrail = { 0x0D, 0x0A };
        public AsciiCRLF(string command)
        {
            List<byte> list = new List<byte>();
            list.AddRange(Encoding.UTF8.GetBytes(command));
            list.AddRange(ProtocolTrail);
            FullBytes = list.ToArray();
            Content = Encoding.UTF8.GetBytes(command);
            FriendlyText = command;
        }
        public AsciiCRLF(byte[] fullBytes)
        {
            FullBytes = fullBytes;
            Content = fullBytes.SubBytes(0, fullBytes.Length - 2);
            FriendlyText = Encoding.UTF8.GetString(Content);
        }
        internal override int MinLength => 3;
        internal override List<ProtocolBase> GetResponse(List<byte> buffer)
        {
            List<ProtocolBase> list = new List<ProtocolBase>();
            int startIndex = 0;
            for (int i = 0; i < buffer.Count - 1; i++)
            {
                if (buffer[i] == ProtocolTrail[0] && buffer[i + 1] == ProtocolTrail[1])
                {
                    int length = i + 2 - startIndex;
                    var fullPacket = new byte[length];
                    buffer.CopyTo(startIndex, fullPacket, 0, length);
                    list.Add(new AsciiCRLF(fullPacket));
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WYW.RS232SOCKET.Protocol
{
    class ModbusTCP : ProtocolBase
    {
        #region 子类必须实现的
        private static readonly int MinLength = 8;
        internal ModbusTCP(byte nodeID, ModbusCommand cmd, byte[] content, UInt16 transactionID = 0)
        {
            TransactionID = (UInt16)transactionID;
            int length = (content != null ? content.Length : 0) + 2;
            List<byte> list = new List<byte>();
            list.AddRange(GetBytesBigEndian(TransactionID));
            list.AddRange(GetBytesBigEndian(0)); // 2个字节，固定为0
            list.AddRange(GetBytesBigEndian((UInt16)length));
            list.Add(nodeID);
            list.Add((byte)cmd);
            list.AddRange(content);
            FullBytes = list.ToArray();
            Content = content;
            FriendlyText = FullBytes.ToHexString();
            Tag = $"{nodeID:X2}{(byte)cmd:X2}"; // 节点ID+指令码
            NodeID = nodeID;
            Command = cmd;

        }
        /// <summary>
        /// 用于接收
        /// </summary>
        /// <param name="fullBytes"></param>
        internal ModbusTCP(byte[] fullBytes)
        {
            FullBytes = fullBytes;
            FriendlyText = FullBytes.ToHexString();
            Content = fullBytes.SubBytes(8, fullBytes.Length - 8);
            Tag = $"{fullBytes[6]:X2}{fullBytes[7]:X2}"; // 节点ID+指令码
            NodeID = fullBytes[6];
            Command = (ModbusCommand)fullBytes[7];
            TransactionID = (UInt16)((fullBytes[0] << 8) + fullBytes[1]);
        }
        internal static List<ProtocolBase> Analyse(List<byte> buffer)
        {
            List<ProtocolBase> result = new List<ProtocolBase>();
            int startIndex = 0;
            for (int i = 0; i < buffer.Count - 3; i++)
            {
                // 固定头的第3字节和第4字节为0，第1字节和第2字节固定，可随机
                if (buffer[i + 2] == 0 && buffer[i + 3] == 0)
                {
                    if (i + MinLength > buffer.Count)
                        break;
                    int length = ((buffer[i + 4] << 8) + buffer[i + 5]) + 6;
                    if (i + length > buffer.Count)
                        break;
                    startIndex = i;
                    var fullPacket = new byte[length];
                    buffer.CopyTo(startIndex, fullPacket, 0, length);
                    result.Add(new ModbusTCP(fullPacket));
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

        /// <summary>
        /// 事务处理标识，2个字节，这里用UInt16表示，大端对其
        /// </summary>
        public UInt16 TransactionID { get; private set; }
    }
}

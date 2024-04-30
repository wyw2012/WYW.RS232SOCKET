using System;
using System.Collections.Generic;
using System.Linq;

namespace WYW.Modbus.Protocols
{
    class ModbusTCP : ModbusBase
    {

        #region 子类必须实现的
        public ModbusTCP(byte slaveID, ModbusCommand cmd, byte[] content, UInt16 transactionID = 0)
        {
            TransactionID = (UInt16)transactionID;
            int length = (content != null ? content.Length : 0) + 2;
            List<byte> list = new List<byte>();
            list.AddRange(BitConverter.GetBytes(TransactionID));
            list.AddRange(new byte[] { 0, 0 }); // 2个字节，固定为0
            list.AddRange(BitConverter.GetBytes((UInt16)length).Reverse());
            list.Add(slaveID);
            list.Add((byte)cmd);
            list.AddRange(content);
            FullBytes = list.ToArray();
            FriendlyText = ToHexString();
            Content = content;
            Tag = $"{slaveID:X2}{(byte)cmd:X2}"; // 节点ID+指令码
            SlaveID = slaveID;
            Command = cmd;
        }

        /// <summary>
        /// 用于接收
        /// </summary>
        /// <param name="fullBytes"></param>
        public ModbusTCP(byte[] fullBytes)
        {
            FullBytes = fullBytes;
            FriendlyText = ToHexString();
            if (FullBytes.Length == 9) // 异常帧
            {
                Content = fullBytes.SubBytes(7, 1); // 故障码
                Tag = null;
            }
            else
            {
                Content = fullBytes.SubBytes(8, fullBytes.Length - 8);
                Tag = $"{fullBytes[6]:X2}{fullBytes[7]:X2}"; // 节点ID+指令码
                SlaveID = fullBytes[6];
                Command = (ModbusCommand)fullBytes[7];
                TransactionID = (UInt16)((fullBytes[0] << 8) + fullBytes[1]);
            }
        }
        #endregion

        #region 属性
        /// <summary>
        /// 事务处理标识，2个字节，这里用UInt16表示，大端对齐
        /// </summary>
        public UInt16 TransactionID { get; private set; }

        internal override int MinLength => 8;

        internal override List<ProtocolBase> GetResponse(List<byte> buffer)
        {
            List<ProtocolBase> list = new List<ProtocolBase>();
            int startIndex = 0;
            for (int i = 0; i <= buffer.Count - 5; i++)
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
                    list.Add(new ModbusTCP(fullPacket));
                    buffer.Clear();
                    break;
                }
            }
            return list;
        }


        #endregion

    }
}

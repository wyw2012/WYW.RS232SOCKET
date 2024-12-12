using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;

namespace WYW.Modbus.Protocols
{
    class ModbusTCP : ModbusBase
    {
        private bool isReceiveFrame;
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
            SlaveID = slaveID;
            Command = cmd;
            switch (Command)
            {
                case ModbusCommand.ReadMoreHoldingRegisters:
                case ModbusCommand.ReadMoreInputResiters:
                    // 节点ID+指令码+寄存器个数*2
                    Tag = $"{SlaveID:X2}{(byte)Command:X2}{(BitConverterHelper.ToUInt16(Content, 2) * 2):X2}";
                    break;
                case ModbusCommand.WriteOneHoldingRegister:
                case ModbusCommand.WriteOneCoil:
                    // 节点ID+指令码+寄存器地址
                    Tag = $"{SlaveID:X2}{(byte)Command:X2}{BitConverterHelper.ToUInt16(Content, 0):X2}";
                    break;
                case ModbusCommand.WriteMoreHoldingRegisters:
                    // 节点ID+指令码+寄存器起始地址+寄存器个数
                    Tag = $"{SlaveID:X2}{(byte)Command:X2}{(BitConverterHelper.ToUInt32(Content, 0)):X4}";
                    break;
                default:
                    // 节点ID+指令码
                    Tag = $"{SlaveID:X2}{(byte)Command:X2}";
                    break;
            };

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
                SlaveID = fullBytes[6];
                Command = (ModbusCommand)fullBytes[7];
                TransactionID = (UInt16)((fullBytes[0] << 8) + fullBytes[1]);
                Tag = $"{SlaveID:X2}{(byte)Command:X2}"; // 节点ID+指令码;
                switch (Command)
                {
                    case ModbusCommand.ReadMoreHoldingRegisters:
                    case ModbusCommand.ReadMoreInputResiters:
                        // 节点ID+指令码+字节长度
                        Tag = $"{SlaveID:X2}{(byte)Command:X2}{Content[0]:X2}";
                        break;
                    case ModbusCommand.WriteOneHoldingRegister:
                    case ModbusCommand.WriteOneCoil:
                        // 节点ID+指令码+寄存器地址
                        if(Content.Length>=2)
                        {
                            Tag = $"{SlaveID:X2}{(byte)Command:X2}{BitConverterHelper.ToUInt16(Content, 0):X2}";
                        }
                        break;
                    case ModbusCommand.WriteMoreHoldingRegisters:
                        // 节点ID+指令码+寄存器地址+寄存器个数
                        if (Content.Length >= 4)
                        {
                            Tag = $"{SlaveID:X2}{(byte)Command:X2}{BitConverterHelper.ToUInt32(Content, 0):X4}";
                        }
                        break;

                }
            }
            isReceiveFrame = true;
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace WYW.Modbus.Protocols
{
    class ModbusRTU : ModbusBase
    {
        #region 子类必须实现的
        public ModbusRTU(byte slaveID, ModbusCommand cmd, byte[] content)
        {
            List<byte> list = new List<byte>
            {
                slaveID,
                (byte)cmd
            };
            list.AddRange(content);
            list.AddRange(GetModbusCRC(list.ToArray()));
            FullBytes = list.ToArray();
            Content = content;
            Tag = $"{slaveID:X2}{(byte)cmd:X2}";  // 节点ID+指令码
            SlaveID = slaveID;
            Command = cmd;
            FriendlyText = ToHexString();
        }

        /// <summary>
        /// 用于接收
        /// </summary>
        /// <param name="fullBytes"></param>
        public ModbusRTU(byte[] fullBytes)
        {
            FullBytes = fullBytes;
            FriendlyText = ToHexString();
            if (fullBytes.Length == 4) // 异常帧
            {
                Content = fullBytes.SubBytes(2, 1); // 故障码
                Tag = null;
            }
            else
            {
                Content = fullBytes.SubBytes(2, fullBytes.Length - 4);
                Tag = $"{fullBytes[0]:X2}{fullBytes[1]:X2}";  // 节点ID+指令码
                SlaveID = fullBytes[0];
                Command = (ModbusCommand)fullBytes[1];
            }
        }
        #endregion

        internal override int MinLength => 4;

        internal override List<ProtocolBase> GetResponse(List<byte> buffer)
        {
            List<ProtocolBase> list = new List<ProtocolBase>();
            if (buffer.Count >= MinLength)
            {
                int length = buffer.Count;
                if (length == 8 || length == 6 || length % 2 == 1)
                {
                    var crc = GetModbusCRC(buffer.Take(buffer.Count - 2).ToArray());
                    if (crc[0] == buffer[buffer.Count - 2] &&
                        crc[1] == buffer[buffer.Count - 1])
                    {
                        var fullPacket = new byte[length];
                        buffer.CopyTo(0, fullPacket, 0, length);
                        list.Add(new ModbusRTU(fullPacket));
                        buffer.Clear();
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// 获取CRC校验
        /// </summary>
        /// <param name="byteData"></param>
        /// <returns></returns>
        private byte[] GetModbusCRC(byte[] byteData)
        {
            byte[] result = new byte[2];
            UInt16 wCrc = 0xFFFF;
            for (int i = 0; i < byteData.Length; i++)
            {
                wCrc ^= Convert.ToUInt16(byteData[i]);
                for (int j = 0; j < 8; j++)
                {
                    if ((wCrc & 0x0001) == 1)
                    {
                        wCrc >>= 1;
                        wCrc ^= 0xA001;//异或多项式
                    }
                    else
                    {
                        wCrc >>= 1;
                    }
                }
            }
            result[1] = (byte)((wCrc & 0xFF00) >> 8);//高位在后
            result[0] = (byte)(wCrc & 0x00FF);       //低位在前
            return result;
        }
    }
}

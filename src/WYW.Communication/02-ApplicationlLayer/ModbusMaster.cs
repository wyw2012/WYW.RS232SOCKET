using WYW.Communication.Protocol;
using System;
using System.Collections.Generic;
using WYW.Communication.TransferLayer;
using System.Net.Sockets;

namespace WYW.Communication.ApplicationlLayer
{
    /// <summary>
    /// Modbus主站
    /// </summary>
    public class ModbusMaster : Device
    {

        public ModbusMaster(TransferBase client) : base(client)
        {
            if (client is RS232Client)
            {
                ProtocolType = ProtocolType.ModbusRTU;
            }
            else if (client is TCPClient)
            {
                ProtocolType = ProtocolType.ModbusTCP;
            }
            else if (client is TCPServer)
            {
                ProtocolType = ProtocolType.ModbusTCP;
            }
            else
            {
                throw new ArgumentException($"对象类型不能为{client.GetType().Name}");
            }
        }

        #region 公共方法

        /// <summary>
        /// 读线圈，功能码0x01
        /// </summary>
        /// <param name="slaveID">节点ID</param>
        /// <param name="startAddress">开始地址</param>
        /// <param name="count">寄存器数量</param>
        /// <param name="coil">线圈状态返回值</param>
        /// <param name="maxSendCount">最大重发次数</param>
        /// <param name="responseTimeout">单次发送超时时间</param>
        /// <returns></returns>
        public bool ReadCoils(int slaveID, UInt16 startAddress, UInt16 count, out bool[] coil, int maxSendCount = 1, int responseTimeout = 200)
        {
            coil = new bool[count];
            List<byte> content = new List<byte>();
            content.AddRange(BigEndianBitConverter.GetBytes(startAddress));
            content.AddRange(BigEndianBitConverter.GetBytes(count));
            var result = SendCommand(slaveID, ModbusCommand.ReadMoreCoils, content.ToArray(), maxSendCount, responseTimeout);
            if (result.IsSuccess)
            {
                if (result.Response != null)
                {
                    if (result.Response.Content.Length == result.Response.Content[0] + 1 &&
                        result.Response.Content[0] == Math.Ceiling(count / 8.0))
                    {
                        // 逐字节判断
                        for (int i = 1; i < result.Response.Content.Length; i++)
                        {
                            for (int j = 0; j < 8; j++)
                            {
                                if ((i - 1) * 8 + j > count - 1)
                                {
                                    break;
                                }
                                if ((result.Response.Content[i] & (1 << j)) != 0)
                                {
                                    coil[(i - 1) * 8 + j] = true;
                                }
                            }

                        }
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 读多个保持寄存器，功能码0x03
        /// </summary>
        /// <param name="slaveID">节点编号</param>
        /// <param name="startAddress">开始地址</param>
        /// <param name="count">寄存器数量</param>
        /// <param name="value">寄存器值数组返回</param>
        /// <param name="maxSendCount">最大重发次数</param>
        /// <param name="responseTimeout">单次发送超时时间</param>
        /// <returns></returns>
        public bool ReadHoldingRegisters(int slaveID, UInt16 startAddress, UInt16 count, out UInt16[] value, int maxSendCount = 1, int responseTimeout = 200)
        {
            value = new ushort[count];
            List<byte> content = new List<byte>();
            content.AddRange(BigEndianBitConverter.GetBytes(startAddress));
            content.AddRange(BigEndianBitConverter.GetBytes(count));
            var result = SendCommand(slaveID, ModbusCommand.ReadMoreHoldingRegisters, content.ToArray(), maxSendCount, responseTimeout);
            if (result.IsSuccess)
            {
                if (result.Response != null)
                {
                    if (result.Response.Content.Length == count * 2 + 1)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            value[i] = BigEndianBitConverter.ToUInt16(result.Response.Content, 1 + i * 2);
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 读多个保持寄存器，功能码0x03
        /// </summary>
        /// <param name="slaveID">节点编号</param>
        /// <param name="startAddress">开始地址</param>
        /// <param name="count">寄存器数量</param>
        /// <param name="byteArray">字节数组，长度是寄存器的2倍</param>
        /// <param name="maxSendCount">最大重发次数</param>
        /// <param name="responseTimeout">单次发送超时时间</param>
        /// <returns></returns>
        public bool ReadHoldingRegisters(int slaveID, UInt16 startAddress, UInt16 count, out byte[] byteArray, int maxSendCount = 1, int responseTimeout = 200)
        {
            byteArray = new byte[count * 2];
            List<byte> content = new List<byte>();
            content.AddRange(BigEndianBitConverter.GetBytes(startAddress));
            content.AddRange(BigEndianBitConverter.GetBytes(count));
            var result = SendCommand(slaveID, ModbusCommand.ReadMoreHoldingRegisters, content.ToArray(), maxSendCount, responseTimeout);
            if (result.IsSuccess)
            {
                if (result.Response != null)
                {
                    if (result.Response.Content.Length == count * 2 + 1)
                    {
                        byteArray = result.Response.Content.SubBytes(1, result.Response.Content.Length - 1);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 读多个输入寄存器，功能码0x04
        /// </summary>
        /// <param name="slaveID">节点编号</param>
        /// <param name="startAddress">开始地址</param>
        /// <param name="count">寄存器数量</param>
        /// <param name="value">寄存器值数组返回</param>
        /// <param name="maxSendCount">最大重发次数</param>
        /// <param name="responseTimeout">单次发送超时时间</param>
        /// <returns></returns>
        public bool ReadInputRegisters(int slaveID, UInt16 startAddress, UInt16 count, out UInt16[] value, int maxSendCount = 1, int responseTimeout = 200)
        {
            value = new ushort[count];
            List<byte> content = new List<byte>();
            content.AddRange(BigEndianBitConverter.GetBytes(startAddress));
            content.AddRange(BigEndianBitConverter.GetBytes(count));
            var result = SendCommand(slaveID, ModbusCommand.ReadMoreInputResiters, content.ToArray(), maxSendCount, responseTimeout);
            if (result.IsSuccess)
            {
                if (result.Response != null)
                {
                    if (result.Response.Content.Length == count * 2 + 1)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            value[i] = BigEndianBitConverter.ToUInt16(result.Response.Content, 1 + i * 2);
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 写单个保持寄存器，功能码0x06
        /// </summary>
        /// <param name="slaveID">节点编号</param>
        /// <param name="address">寄存器地址</param>
        /// <param name="value">寄存器值</param>
        /// <param name="maxSendCount">最大重发次数</param>
        /// <param name="responseTimeout">单次发送超时时间</param
        /// <returns></returns>
        public bool WriteHoldingRegister(int slaveID, UInt16 address, UInt16 value, int maxSendCount = 1, int responseTimeout = 200)
        {
            List<byte> content = new List<byte>();
            content.AddRange(BigEndianBitConverter.GetBytes(address));
            content.AddRange(BigEndianBitConverter.GetBytes(value));
            var result = SendCommand(slaveID, ModbusCommand.WriteOneHoldingRegister, content.ToArray(), maxSendCount, responseTimeout);
            return result.IsSuccess;
        }

        /// <summary>
        /// 写多个线圈，功能码0x0F
        /// </summary>
        /// <param name="slaveID">节点ID</param>
        /// <param name="startAddress">开始地址</param>
        /// <param name="coil">线圈状态</param>
        /// <param name="maxSendCount">最大重发次数</param>
        /// <param name="responseTimeout">单次发送超时时间</param>
        /// <returns></returns>
        public bool WriteCoils(int slaveID, UInt16 startAddress, bool[] coil, int maxSendCount = 1, int responseTimeout = 200)
        {
            List<byte> content = new List<byte>();
            content.AddRange(BigEndianBitConverter.GetBytes(startAddress));
            content.AddRange(BigEndianBitConverter.GetBytes((UInt16)coil.Length));
            content.Add(2);
            int value = 0;
            for (int i = 0; i < coil.Length; i++)
            {
                if (i < 8)
                {
                    if (coil[i])
                    {
                        value |= (1 << (i + 8));
                    }
                }
                else
                {
                    if (coil[i])
                    {
                        value |= (1 << (i - 8));
                    }
                }
            }
            content.AddRange(BigEndianBitConverter.GetBytes((UInt16)value));
            var result = SendCommand(slaveID, ModbusCommand.WriteMoreCoils, content.ToArray(), maxSendCount, responseTimeout);
            return result.IsSuccess;
        }

        /// <summary>
        /// 写多个保持寄存器，功能码0x10
        /// </summary>
        /// <param name="slaveID">节点编号</param>
        /// <param name="startAddress">开始地址</param>
        /// <param name="value">寄存器值数组</param>
        /// <param name="maxSendCount">最大重发次数</param>
        /// <param name="responseTimeout">单次发送超时时间</param>
        /// <returns></returns>
        public bool WriteHoldingRegisters(int slaveID, UInt16 startAddress, UInt16[] value, int maxSendCount = 1, int responseTimeout = 200)
        {
            List<byte> content = new List<byte>();
            content.AddRange(BigEndianBitConverter.GetBytes(startAddress));
            content.AddRange(BigEndianBitConverter.GetBytes((UInt16)value.Length));
            content.Add((byte)(value.Length * 2));
            foreach (var item in value)
            {
                content.AddRange(BigEndianBitConverter.GetBytes(item));
            }
            var result = SendCommand(slaveID, ModbusCommand.WriteMoreHoldingRegisters, content.ToArray(), maxSendCount, responseTimeout);
            return result.IsSuccess;
        }

        /// <summary>
        /// 写多个保持寄存器，功能码0x10
        /// </summary>
        /// <param name="slaveID">节点编号</param>
        /// <param name="startAddress">开始地址</param>
        /// <param name="value">寄存器值字节数组，长度是2的倍数</param>
        /// <param name="maxSendCount">最大重发次数</param>
        /// <param name="responseTimeout">单次发送超时时间</param>
        /// <returns></returns>
        public bool WriteHoldingRegisters(int slaveID, UInt16 startAddress, byte[] value, int maxSendCount = 1, int responseTimeout = 200)
        {
            if (value.Length % 2 == 1)
            {
                return false;
            }
            List<byte> content = new List<byte>();
            content.AddRange(BigEndianBitConverter.GetBytes(startAddress));
            content.AddRange(BigEndianBitConverter.GetBytes((UInt16)(value.Length / 2)));
            content.Add((byte)(value.Length));
            content.AddRange(value);

            var result = SendCommand(slaveID, ModbusCommand.WriteMoreHoldingRegisters, content.ToArray(), maxSendCount, responseTimeout);
            return result.IsSuccess;
        }

        /// <summary>
        /// 读和写保持寄存器，功能码0x17
        /// </summary>
        /// <param name="slaveID">节点编号</param>
        /// <param name="startReadAddress">起始读取地址</param>
        /// <param name="readCount">读取的寄存器数量</param>
        /// <param name="startWriteAddress">起始写地址</param>
        /// <param name="writeValues">写的寄存器数组</param>
        /// <param name="readValues">读取寄存器返回值</param>
        /// <param name="maxSendCount">最大重发次数</param>
        /// <param name="responseTimeout">单次发送超时时间</param>
        /// <returns></returns>
        public bool ReadAndWriteHoldingRegisters(int slaveID, UInt16 startReadAddress, UInt16 readCount, UInt16 startWriteAddress, UInt16[] writeValues, out UInt16[] readValues, int maxSendCount = 1, int responseTimeout = 200)
        {
            readValues = new ushort[readCount];
            List<byte> content = new List<byte>();
            content.AddRange(BigEndianBitConverter.GetBytes(startReadAddress));
            content.AddRange(BigEndianBitConverter.GetBytes(readCount));
            content.AddRange(BigEndianBitConverter.GetBytes(startWriteAddress));
            content.AddRange(BigEndianBitConverter.GetBytes((UInt16)writeValues.Length));
            content.Add((byte)(writeValues.Length * 2));
            foreach (var item in writeValues)
            {
                content.AddRange(BigEndianBitConverter.GetBytes(item));
            }
            var result = SendCommand(slaveID, ModbusCommand.ReadWriteHoldingRegisters, content.ToArray(), maxSendCount, responseTimeout);
            if (result.IsSuccess)
            {
                if (result.Response != null)
                {
                    if (result.Response.Content.Length == readCount * 2 + 1)
                    {
                        for (int i = 0; i < readCount; i++)
                        {
                            readValues[i] = BigEndianBitConverter.ToUInt16(result.Response.Content, 1 + i * 2);
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion
        #region 私有函数
        private ExecutionResult SendCommand(int slaveID, ModbusCommand cmd, byte[] content, int maxSendCount = 1, int responseTimeout = 200)
        {
            ProtocolBase obj = null;
            if (ProtocolType == ProtocolType.ModbusRTU)
            {
                obj = new ModbusRTU((byte)slaveID, cmd, content);
            }
            else
            {
                obj = new ModbusTCP((byte)slaveID, cmd, content);
            }
            return SendProtocol(obj, true, maxSendCount, responseTimeout);
        }
        #endregion
    }
}

using WYW.Communication.Protocol;
using System;
using System.Collections.Generic;
using WYW.Communication.TransferLayer;
using WYW.Communication.Models;
using System.Linq;
using System.Threading;

namespace WYW.Communication
{
    /// <summary>
    /// Modbus主站
    /// </summary>
    public class ModbusMaster : Device
    {
        protected ModbusMaster() { }
        public ModbusMaster(TransferBase client, ModbusProtocolType protocolType = ModbusProtocolType.Auto) : base(client)
        {
        
            switch (protocolType)
            {
                case ModbusProtocolType.ModbusRTU:
                    ProtocolType = ProtocolType.ModbusRTU;
                    break;
                case ModbusProtocolType.ModbusTCP:
                    ProtocolType = ProtocolType.ModbusTCP;
                    break;
                case ModbusProtocolType.Auto:
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
                    break;
            }
        }



        #region 公共方法
        #region 静态方法
        /// <summary>
        /// 创建心跳对象，使用读某个保持/输入/线圈寄存器判断心跳是否正常
        /// </summary>
        /// <param name="slaveID">从站地址</param>
        /// <param name="address">读取寄存器的地址</param>
        /// <returns></returns>
        public ProtocolBase CreateHeartBeatContent(byte slaveID, int address, int count = 1, RegisterType registerType = RegisterType.保持寄存器, UInt16 transactionID = 0)
        {
            List<byte> content = new List<byte>();
            content.AddRange(BitConverterHelper.GetBytes((UInt16)address, EndianType.BigEndian));
            content.AddRange(BitConverterHelper.GetBytes((UInt16)count, EndianType.BigEndian));
            ModbusCommand cmd = ModbusCommand.ReadMoreHoldingRegisters;
            switch (registerType)
            {
                case RegisterType.保持寄存器:
                    cmd = ModbusCommand.ReadMoreHoldingRegisters;
                    break;
                case RegisterType.输入寄存器:
                    cmd = ModbusCommand.ReadMoreInputResiters;
                    break;
                case RegisterType.离散量输入:
                    cmd = ModbusCommand.ReadMoreDiscreteInputRegisters;
                    break;
                case RegisterType.线圈:
                    cmd = ModbusCommand.ReadMoreCoils;
                    break;
            }
            if (ProtocolType == ProtocolType.ModbusRTU)
            {
                return new ModbusRTU(slaveID, cmd, content.ToArray());
            }
            else
            {
                return new ModbusTCP(slaveID, cmd, content.ToArray(), transactionID);
            }

        }
        #endregion

        /// <summary>
        /// 读离散输入量，功能码0x02
        /// </summary>
        /// <param name="slaveID">节点ID</param>
        /// <param name="startAddress">开始地址</param>
        /// <param name="count">寄存器数量</param>
        /// <param name="discreteInput">离散输入量状态返回值</param>
        /// <param name="maxSendCount">最大重发次数</param>
        /// <param name="responseTimeout">单次发送超时时间</param>
        /// <returns></returns>
        public ExecutionResult ReadDiscreteInputRegisters(int slaveID, UInt16 startAddress, UInt16 count, out bool[] discreteInput, int maxSendCount = 1, int responseTimeout = 300)
        {
            discreteInput = new bool[count];
            List<byte> content = new List<byte>();
            content.AddRange(BitConverterHelper.GetBytes(startAddress, EndianType.BigEndian));
            content.AddRange(BitConverterHelper.GetBytes(count, EndianType.BigEndian));
            var result = SendCommand(slaveID, ModbusCommand.ReadMoreDiscreteInputRegisters, content.ToArray(), true, maxSendCount, responseTimeout);
            if (result.IsSuccess)
            {
                if (result.Response != null)
                {
                    if (result.Response.Content.Length > 0 &&
                        result.Response.Content.Length == result.Response.Content[0] + 1 &&
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
                                    discreteInput[(i - 1) * 8 + j] = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = "返回字节长度不满足协议要求";
                        //DeviceStatus = DeviceStatus.Warning;
                    }
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "返回内容为空";
                    //DeviceStatus = DeviceStatus.Warning;
                }
            }
            return result;
        }


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
        public ExecutionResult ReadCoils(int slaveID, UInt16 startAddress, UInt16 count, out bool[] coil, int maxSendCount = 1, int responseTimeout = 300)
        {
            coil = new bool[count];
            List<byte> content = new List<byte>();
            content.AddRange(BitConverterHelper.GetBytes(startAddress, EndianType.BigEndian));
            content.AddRange(BitConverterHelper.GetBytes(count, EndianType.BigEndian));
            var result = SendCommand(slaveID, ModbusCommand.ReadMoreCoils, content.ToArray(), true, maxSendCount, responseTimeout);
            if (result.IsSuccess)
            {
                if (result.Response != null)
                {
                    if (result.Response.Content.Length > 0 &&
                        result.Response.Content.Length == result.Response.Content[0] + 1 &&
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
                    }
                    else
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = "返回字节长度不满足协议要求";
                        //DeviceStatus = DeviceStatus.Warning;
                    }
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "返回内容为空";
                    //DeviceStatus = DeviceStatus.Warning;
                }
            }
            return result;
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
        public ExecutionResult ReadHoldingRegisters(int slaveID, UInt16 startAddress, UInt16 count, out UInt16[] value, int maxSendCount = 1, int responseTimeout = 300)
        {
            value = new ushort[count];
            List<byte> content = new List<byte>();
            content.AddRange(BitConverterHelper.GetBytes(startAddress, EndianType.BigEndian));
            content.AddRange(BitConverterHelper.GetBytes(count, EndianType.BigEndian));
            var result = SendCommand(slaveID, ModbusCommand.ReadMoreHoldingRegisters, content.ToArray(), true, maxSendCount, responseTimeout);
            if (result.IsSuccess)
            {
                if (result.Response != null)
                {
                    if (result.Response.Content.Length == count * 2 + 1)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            value[i] = BitConverterHelper.ToUInt16(result.Response.Content, 1 + i * 2, endianType: EndianType.BigEndian);
                        }
                    }
                    else
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = "返回字节长度不满足协议要求";
                        //DeviceStatus = DeviceStatus.Warning;
                    }
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "返回内容为空";
                    //DeviceStatus = DeviceStatus.Warning;
                }
            }

            return result;
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
        public ExecutionResult ReadHoldingRegisters(int slaveID, UInt16 startAddress, UInt16 count, out byte[] byteArray, int maxSendCount = 1, int responseTimeout = 300)
        {
            byteArray = new byte[count * 2];
            List<byte> content = new List<byte>();
            content.AddRange(BitConverterHelper.GetBytes(startAddress, EndianType.BigEndian));
            content.AddRange(BitConverterHelper.GetBytes(count, EndianType.BigEndian));
            var result = SendCommand(slaveID, ModbusCommand.ReadMoreHoldingRegisters, content.ToArray(), true, maxSendCount, responseTimeout);
            if (result.IsSuccess)
            {
                if (result.Response != null)
                {
                    if (result.Response.Content.Length == count * 2 + 1)
                    {
                        byteArray = result.Response.Content.SubBytes(1, result.Response.Content.Length - 1);
                    }
                    else
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = "返回字节长度不满足协议要求";
                        //DeviceStatus = DeviceStatus.Warning;
                    }
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "返回内容为空";
                    //DeviceStatus = DeviceStatus.Warning;
                }
            }
            return result;
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
        public ExecutionResult ReadInputRegisters(int slaveID, UInt16 startAddress, UInt16 count, out UInt16[] value, int maxSendCount = 1, int responseTimeout = 300)
        {
            value = new ushort[count];
            List<byte> content = new List<byte>();
            content.AddRange(BitConverterHelper.GetBytes(startAddress, EndianType.BigEndian));
            content.AddRange(BitConverterHelper.GetBytes(count, EndianType.BigEndian));
            var result = SendCommand(slaveID, ModbusCommand.ReadMoreInputResiters, content.ToArray(), true, maxSendCount, responseTimeout);
            if (result.IsSuccess)
            {
                if (result.Response != null)
                {
                    if (result.Response.Content.Length == count * 2 + 1)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            value[i] = BitConverterHelper.ToUInt16(result.Response.Content, 1 + i * 2, endianType: EndianType.BigEndian);
                        }
                    }
                    else
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = "返回字节长度不满足协议要求";
                        //DeviceStatus = DeviceStatus.Warning;
                    }
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "返回内容为空";
                    //DeviceStatus = DeviceStatus.Warning;
                }
            }

            return result;
        }
        /// <summary>
        /// 读多个输入寄存器，功能码0x04
        /// </summary>
        /// <param name="slaveID">节点编号</param>
        /// <param name="startAddress">开始地址</param>
        /// <param name="count">寄存器数量</param>
        /// <param name="byteArray">字节数组，长度是寄存器的2倍</param>
        /// <param name="maxSendCount">最大重发次数</param>
        /// <param name="responseTimeout">单次发送超时时间</param>
        /// <returns></returns>
        public ExecutionResult ReadInputRegisters(int slaveID, UInt16 startAddress, UInt16 count, out byte[] byteArray, int maxSendCount = 1, int responseTimeout = 300)
        {
            byteArray = new byte[count * 2];
            List<byte> content = new List<byte>();
            content.AddRange(BitConverterHelper.GetBytes(startAddress, EndianType.BigEndian));
            content.AddRange(BitConverterHelper.GetBytes(count, EndianType.BigEndian));
            var result = SendCommand(slaveID, ModbusCommand.ReadMoreInputResiters, content.ToArray(),true, maxSendCount, responseTimeout);
            if (result.IsSuccess)
            {
                if (result.Response != null)
                {
                    if (result.Response.Content.Length == count * 2 + 1)
                    {
                        byteArray = result.Response.Content.SubBytes(1, result.Response.Content.Length - 1);
                    }
                    else
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = "返回字节长度不满足协议要求";
                        //DeviceStatus = DeviceStatus.Warning;
                    }
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "返回内容为空";
                    //DeviceStatus = DeviceStatus.Warning;
                }
            }

            return result;
        }

        /// <summary>
        /// 写单个线圈，功能码0x05
        /// </summary>
        /// <param name="slaveID">节点ID</param>
        /// <param name="address">开始地址</param>
        /// <param name="value">线圈状态</param>
        /// <param name="maxSendCount">最大重发次数</param>
        /// <param name="responseTimeout">单次发送超时时间</param>
        public ExecutionResult WriteCoil(int slaveID, UInt16 address, bool value,  bool isNeedResponse = true,int maxSendCount = 1, int responseTimeout = 300)
        {
            List<byte> content = new List<byte>();
            content.AddRange(BitConverterHelper.GetBytes(address, EndianType.BigEndian));
            if (value)
            {
                content.AddRange(new byte[] { 0xFF, 0x00 });
            }
            else
            {
                content.AddRange(new byte[] { 0x00, 0x00 });
            }
            var result = SendCommand(slaveID, ModbusCommand.WriteOneCoil, content.ToArray(), isNeedResponse, maxSendCount, responseTimeout);
            if (!result.IsSuccess)
            {
                //DeviceStatus = DeviceStatus.Warning;
            }
            return result;
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
        public ExecutionResult WriteHoldingRegister(int slaveID, UInt16 address, UInt16 value, bool isNeedResponse = true, int maxSendCount = 1, int responseTimeout = 300)
        {
            List<byte> content = new List<byte>();
            content.AddRange(BitConverterHelper.GetBytes(address, EndianType.BigEndian));
            content.AddRange(BitConverterHelper.GetBytes(value, EndianType.BigEndian));
            return SendCommand(slaveID, ModbusCommand.WriteOneHoldingRegister, content.ToArray(), isNeedResponse, maxSendCount, responseTimeout);
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
        public ExecutionResult WriteCoils(int slaveID, UInt16 startAddress, bool[] coil, bool isNeedResponse = true, int maxSendCount = 1, int responseTimeout = 300)
        {
            List<byte> content = new List<byte>();
            content.AddRange(BitConverterHelper.GetBytes(startAddress, EndianType.BigEndian));
            content.AddRange(BitConverterHelper.GetBytes((UInt16)coil.Length, EndianType.BigEndian));
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
            content.AddRange(BitConverterHelper.GetBytes((UInt16)value, EndianType.BigEndian));
            return SendCommand(slaveID, ModbusCommand.WriteMoreCoils, content.ToArray(), isNeedResponse, maxSendCount, responseTimeout);
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
        public ExecutionResult WriteHoldingRegisters(int slaveID, UInt16 startAddress, UInt16[] value,  bool isNeedResponse = true,int maxSendCount = 1, int responseTimeout = 300)
        {
            List<byte> content = new List<byte>();
            content.AddRange(BitConverterHelper.GetBytes(startAddress, EndianType.BigEndian));
            content.AddRange(BitConverterHelper.GetBytes((UInt16)value.Length, EndianType.BigEndian));
            content.Add((byte)(value.Length * 2));
            foreach (var item in value)
            {
                content.AddRange(BitConverterHelper.GetBytes(item, EndianType.BigEndian));
            }
            return SendCommand(slaveID, ModbusCommand.WriteMoreHoldingRegisters, content.ToArray(),isNeedResponse, maxSendCount, responseTimeout);

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
        public ExecutionResult WriteHoldingRegisters(int slaveID, UInt16 startAddress, byte[] value, bool isNeedResponse = true, int maxSendCount = 1, int responseTimeout = 300)
        {
            if (value.Length % 2 == 1)
            {
                return ExecutionResult.Failed(Properties.Message.ArrayLengthNotOdd);
            }
            List<byte> content = new List<byte>();
            content.AddRange(BitConverterHelper.GetBytes(startAddress, EndianType.BigEndian));
            content.AddRange(BitConverterHelper.GetBytes((UInt16)(value.Length / 2), EndianType.BigEndian));
            content.Add((byte)(value.Length));
            content.AddRange(value);

            return SendCommand(slaveID, ModbusCommand.WriteMoreHoldingRegisters, content.ToArray(), isNeedResponse, maxSendCount, responseTimeout);
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
        public ExecutionResult ReadAndWriteHoldingRegisters(int slaveID, UInt16 startReadAddress, UInt16 readCount, UInt16 startWriteAddress, UInt16[] writeValues, out UInt16[] readValues, int maxSendCount = 1, int responseTimeout = 300)
        {
            readValues = new ushort[readCount];
            List<byte> content = new List<byte>();
            content.AddRange(BitConverterHelper.GetBytes(startReadAddress, EndianType.BigEndian));
            content.AddRange(BitConverterHelper.GetBytes(readCount, EndianType.BigEndian));
            content.AddRange(BitConverterHelper.GetBytes(startWriteAddress, EndianType.BigEndian));
            content.AddRange(BitConverterHelper.GetBytes((UInt16)writeValues.Length, EndianType.BigEndian));
            content.Add((byte)(writeValues.Length * 2));
            foreach (var item in writeValues)
            {
                content.AddRange(BitConverterHelper.GetBytes(item, EndianType.BigEndian));
            }
            var result = SendCommand(slaveID, ModbusCommand.ReadWriteHoldingRegisters, content.ToArray(), true, maxSendCount, responseTimeout);
            if (result.IsSuccess)
            {
                if (result.Response != null)
                {
                    if (result.Response.Content.Length == readCount * 2 + 1)
                    {
                        for (int i = 0; i < readCount; i++)
                        {
                            readValues[i] = BitConverterHelper.ToUInt16(result.Response.Content, 1 + i * 2, endianType: EndianType.BigEndian);
                        }

                    }
                    else
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = "返回字节长度不满足协议要求";
                        //DeviceStatus = DeviceStatus.Warning;
                    }
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "返回内容为空";
                    //DeviceStatus = DeviceStatus.Warning;
                }
            }

            return result;
        }
        /// <summary>
        /// 监控寄存器的值是否与目标值相匹配
        /// </summary>
        /// <param name="slaveID">节点编号</param>
        /// <param name="address">地址</param>
        /// <param name="targetValue">目标值</param>
        /// <param name="registerType">寄存器类型，默认保持寄存器</param>
        /// <param name="timeout">超时时间，单位ms</param>
        /// <returns></returns>
        public bool MonitorRegister(int slaveID, UInt16 address, UInt16 targetValue, RegisterType registerType = RegisterType.保持寄存器, int timeout = 2000)
        {
            UInt16[] registerValues;
            bool[] coilValues;
            DateTime dateTime = DateTime.Now;
            ExecutionResult result = null;
            while ((DateTime.Now - dateTime).TotalMilliseconds < timeout)
            {
                switch (registerType)
                {
                    case RegisterType.离散量输入:
                        result = ReadDiscreteInputRegisters(slaveID, address, 1, out coilValues, responseTimeout: 100);
                        if (result.IsSuccess && coilValues[0] == (targetValue != 0))
                        {
                            return true;
                        }
                        break;
                    case RegisterType.线圈:
                        result = ReadCoils(slaveID, address, 1, out coilValues, responseTimeout: 100);
                        if (result.IsSuccess && coilValues[0] == (targetValue != 0))
                        {
                            return true;
                        }
                        break;
                    case RegisterType.输入寄存器:
                        result = ReadInputRegisters(slaveID, address, 1, out registerValues, responseTimeout: 100);
                        if (result.IsSuccess && registerValues[0] == targetValue)
                        {
                            return true;
                        }
                        break;
                    case RegisterType.保持寄存器:
                        result = ReadHoldingRegisters(slaveID, address, 1, out registerValues, responseTimeout: 100);
                        if (result.IsSuccess && registerValues[0] == targetValue)
                        {
                            return true;
                        }
                        break;
                }
                Thread.Sleep(1);
            }

            return false;
        }
        #endregion

        #region 寄存器数组操作
        /// <summary>
        /// 读取多个寄存器几个
        /// </summary>
        /// <param name="slaveID"></param>
        /// <param name="registers"></param>
        /// <param name="responseTimeout"></param>
        /// <param name="tokenSource"></param>
        /// <param name="isIgnoreFailed"></param>
        /// <exception cref="Exception"></exception>
        public void ReadRegisterCollection(int slaveID, IEnumerable<Register> registers, int responseTimeout = 300, CancellationTokenSource tokenSource = null, bool isIgnoreFailed = false)
        {
            if (!registers.Any(x => x.IsChecked == true))
            {
                throw new Exception("请先勾选需要读取的寄存器");
            }
            Register.ValicateAddress(registers.Where(x => x.IsChecked));
            // 获取需要读取的寄存器起始地址和数量
            List<List<Register>> list = new List<List<Register>>();
            ExecutionResult result;
            #region 读取保持寄存器
            var selectedRegisters = registers.Where(x => x.IsChecked && x.RegisterType == RegisterType.保持寄存器 && x.WriteType != RegisterWriteType.只写).OrderBy(x => x.Address).ToArray();
            int startIndex = 0;
            list.Clear();
            byte[] fullBytes;
            if (selectedRegisters.Length > 0)
            {
                for (int i = 0; i < selectedRegisters.Length; i++)
                {
                    if (i == selectedRegisters.Length - 1 ||
                        selectedRegisters[i].Address + selectedRegisters[i].RegisterCount != selectedRegisters[i + 1].Address)
                    {
                        List<Register> temp = new List<Register>();
                        for (int j = startIndex; j <= i; j++)
                        {
                            temp.Add(selectedRegisters[j]);
                        }
                        list.Add(temp);
                        startIndex = i + 1;
                    }
                }

                foreach (var item in list)
                {
                    if (tokenSource?.Token.IsCancellationRequested == true)
                    {
                        return;
                    }
                    int startAddress = (UInt16)item.Min(x => x.Address);
                    int count = item.Sum(x => x.RegisterCount);
                    result = ReadHoldingRegisters(slaveID, (UInt16)startAddress, (UInt16)count, out fullBytes, responseTimeout: responseTimeout);
                    if (result.IsSuccess)
                    {
                        int index = 0;
                        for (int i = 0; i < item.Count; i++)
                        {
                            item[i].Value = item[i].GetValue(fullBytes, index);
                            index += (item[i].RegisterCount * 2);
                        }
                    }
                    else
                    {
                        if (!isIgnoreFailed)
                        {
                            throw new Exception($"读取起始地址从{startAddress}到{startAddress + count - 1}的保持寄存器失败，原因：{result.ErrorMessage}");

                        }
                    }
                }
            }

            #endregion

            #region 读取输入寄存器
            selectedRegisters = registers.Where(x => x.IsChecked && x.RegisterType == RegisterType.输入寄存器).OrderBy(x => x.Address).ToArray();
            startIndex = 0;
            list.Clear();
            if (selectedRegisters.Length > 0)
            {
                for (int i = 0; i < selectedRegisters.Length; i++)
                {
                    if (i == selectedRegisters.Length - 1 ||
                        selectedRegisters[i].Address + selectedRegisters[i].RegisterCount != selectedRegisters[i + 1].Address)
                    {
                        List<Register> temp = new List<Register>();
                        for (int j = startIndex; j <= i; j++)
                        {
                            temp.Add(selectedRegisters[j]);
                        }
                        list.Add(temp);
                        startIndex = i + 1;
                    }
                }
                foreach (var item in list)
                {
                    if (tokenSource?.Token.IsCancellationRequested == true)
                    {
                        return;
                    }
                    int startAddress = (UInt16)item.Min(x => x.Address);
                    int count = item.Sum(x => x.RegisterCount);
                    result = ReadInputRegisters(slaveID, (UInt16)startAddress, (UInt16)count, out fullBytes, responseTimeout: responseTimeout);
                    if (result.IsSuccess)
                    {
                        int index = 0;
                        for (int i = 0; i < item.Count; i++)
                        {
                            item[i].Value = item[i].GetValue(fullBytes, index);
                            index += (item[i].RegisterCount * 2);
                        }
                    }
                    else
                    {
                        if (!isIgnoreFailed)
                        {
                            throw new Exception($"读取起始地址从{startAddress}到{startAddress + count - 1}输入寄存器失败，原因：{result.ErrorMessage}");

                        }
                    }
                }
            }

            #endregion

            #region 读取离散输入量
            selectedRegisters = registers.Where(x => x.IsChecked && x.RegisterType == RegisterType.离散量输入).OrderBy(x => x.Address).ToArray();
            startIndex = 0;
            list.Clear();
            if (selectedRegisters.Length > 0)
            {
                for (int i = 0; i < selectedRegisters.Length; i++)
                {
                    if (i == selectedRegisters.Length - 1 ||
                        selectedRegisters[i].Address + selectedRegisters[i].RegisterCount != selectedRegisters[i + 1].Address)
                    {
                        List<Register> temp = new List<Register>();
                        for (int j = startIndex; j <= i; j++)
                        {
                            temp.Add(selectedRegisters[j]);
                        }
                        list.Add(temp);
                        startIndex = i + 1;
                    }
                }
                foreach (var item in list)
                {
                    if (tokenSource?.Token.IsCancellationRequested == true)
                    {
                        return;
                    }
                    int startAddress = (UInt16)item.Min(x => x.Address);
                    int count = item.Sum(x => x.RegisterCount);
                    result = ReadDiscreteInputRegisters(slaveID, (UInt16)startAddress, (UInt16)count, out bool[] values, responseTimeout: responseTimeout);
                    if (result.IsSuccess)
                    {
                        for (int i = 0; i < item.Count; i++)
                        {
                            item[i].Value = values[i] ? "1" : "0";
                        }
                    }
                    else
                    {
                        if (!isIgnoreFailed)
                        {
                            throw new Exception($"读取起始地址从{startAddress}到{startAddress + count - 1}离散量输入失败，原因：{result.ErrorMessage}");

                        }
                    }
                }
            }

            #endregion

            #region 读取线圈
            selectedRegisters = registers.Where(x => x.IsChecked && x.RegisterType == RegisterType.线圈 && x.WriteType != RegisterWriteType.只写).OrderBy(x => x.Address).ToArray();
            startIndex = 0;
            list.Clear();
            if (selectedRegisters.Length > 0)
            {
                for (int i = 0; i < selectedRegisters.Length; i++)
                {
                    if (i == selectedRegisters.Length - 1 ||
                        selectedRegisters[i].Address + selectedRegisters[i].RegisterCount != selectedRegisters[i + 1].Address)
                    {
                        List<Register> temp = new List<Register>();
                        for (int j = startIndex; j <= i; j++)
                        {
                            temp.Add(selectedRegisters[j]);
                        }
                        list.Add(temp);
                        startIndex = i + 1;
                    }
                }
                foreach (var item in list)
                {
                    if (tokenSource?.Token.IsCancellationRequested == true)
                    {
                        return;
                    }
                    int startAddress = (UInt16)item.Min(x => x.Address);
                    int count = item.Sum(x => x.RegisterCount);
                    result = ReadCoils(slaveID, (UInt16)startAddress, (UInt16)count, out bool[] values, responseTimeout: responseTimeout);
                    if (result.IsSuccess)
                    {
                        for (int i = 0; i < item.Count; i++)
                        {
                            item[i].Value = values[i] ? "1" : "0";
                        }
                    }
                    else
                    {
                        if (!isIgnoreFailed)
                        {
                            throw new Exception($"读取起始地址从{startAddress}到{startAddress + count - 1}线圈失败，原因：{result.ErrorMessage}");

                        }
                    }
                }
            }

            #endregion
        }
        /// <summary>
        /// 写多个寄存器集合
        /// </summary>
        /// <param name="slaveID"></param>
        /// <param name="registers"></param>
        /// <param name="responseTimeout">每条指令的超时时间，单位ms</param>
        /// <param name="isSupportMultiWriteCommand">是否支持写多个寄存器或线圈指令，例如0x10、0x0F</param>
        /// <exception cref="Exception"></exception>
        public void WriteRegisterCollection(int slaveID, IEnumerable<Register> registers, int responseTimeout = 300, bool isSupportMultiWriteCommand = true, CancellationTokenSource tokenSource = null, bool isIgnoreFailed = false)
        {
            if (!registers.Any(x => x.IsChecked == true))
            {
                throw new Exception("请先勾选需要读取的寄存器");
            }
            Register.ValicateAddress(registers.Where(x => x.IsChecked));
            Register.ValicateValue(registers.Where(x => x.IsChecked));
            ExecutionResult result;
            // 一条一条写
            if (!isSupportMultiWriteCommand)
            {
                var items = registers.Where(x => x.IsChecked && x.WriteType != RegisterWriteType.只读);
                foreach (var item in items)
                {
                    if (tokenSource?.Token.IsCancellationRequested == true)
                    {
                        return;
                    }
                    switch (item.RegisterType)
                    {
                        case RegisterType.保持寄存器:
                            var values = BitConverterHelper.ToUInt16Array(item.GetBytes(), endianType: (EndianType)item.EndianType);
                            foreach (var value in values)
                            {
                                result = WriteHoldingRegister(slaveID, (UInt16)item.Address, value, responseTimeout: responseTimeout);
                                if (!result.IsSuccess && !isIgnoreFailed)
                                {
                                    throw new Exception($"写地址为{item.Address}保持寄存器失败，原因：{result.ErrorMessage}");
                                }
                            }
                            break;
                        case RegisterType.线圈:
                            result = WriteCoil(slaveID, (UInt16)item.Address, item.Value == "1", responseTimeout: responseTimeout);
                            if (!result.IsSuccess && !isIgnoreFailed)
                            {
                                throw new Exception($"写地址为{item.Address}线圈失败，原因：{result.ErrorMessage}");
                            }
                            break;
                    }
                }
            }
            // 使用多指令批量写
            else
            {
                // 获取需要写的寄存器
                List<List<Register>> list = new List<List<Register>>();

                #region 写保持寄存器
                var selectedRegisters = registers.Where(x => x.IsChecked && x.RegisterType == RegisterType.保持寄存器 && x.WriteType != RegisterWriteType.只读).OrderBy(x => x.Address).ToArray();
                int startIndex = 0;
                list.Clear();
                if (selectedRegisters.Length > 0)
                {
                    for (int i = 0; i < selectedRegisters.Length; i++)
                    {
                        if (i == selectedRegisters.Length - 1 ||
                            selectedRegisters[i].Address + selectedRegisters[i].RegisterCount != selectedRegisters[i + 1].Address)
                        {
                            List<Register> temp = new List<Register>();
                            for (int j = startIndex; j <= i; j++)
                            {
                                temp.Add(selectedRegisters[j]);
                            }
                            list.Add(temp);
                            startIndex = i + 1;
                        }
                    }

                    foreach (var item in list)
                    {
                        if (tokenSource?.Token.IsCancellationRequested == true)
                        {
                            return;
                        }
                        int startAddress = (UInt16)item.Min(x => x.Address);
                        int count = item.Sum(x => x.RegisterCount);
                        List<byte> sendArray = new List<byte>();
                        foreach (var reg in item)
                        {
                            sendArray.AddRange(reg.GetBytes());
                        }
                        result = WriteHoldingRegisters(slaveID, (UInt16)startAddress, sendArray.ToArray(), responseTimeout: responseTimeout);
                        if (!result.IsSuccess && !isIgnoreFailed)
                        {
                            throw new Exception($"写起始地址从{startAddress}到{startAddress + count - 1}保持寄存器失败，原因：{result.ErrorMessage}");
                        }
                    }
                }


                #endregion

                #region 写线圈
                selectedRegisters = registers.Where(x => x.IsChecked && x.RegisterType == RegisterType.线圈 && x.WriteType != RegisterWriteType.只读).OrderBy(x => x.Address).ToArray();
                startIndex = 0;
                list.Clear();
                if (selectedRegisters.Length > 0)
                {
                    for (int i = 0; i < selectedRegisters.Length; i++)
                    {
                        if (i == selectedRegisters.Length - 1 ||
                            selectedRegisters[i].Address + selectedRegisters[i].RegisterCount != selectedRegisters[i + 1].Address)
                        {
                            List<Register> temp = new List<Register>();
                            for (int j = startIndex; j <= i; j++)
                            {
                                temp.Add(selectedRegisters[j]);
                            }
                            list.Add(temp);
                            startIndex = i + 1;
                        }
                    }

                    foreach (var item in list)
                    {
                        if (tokenSource?.Token.IsCancellationRequested == true)
                        {
                            return;
                        }
                        int startAddress = (UInt16)item.Min(x => x.Address);
                        int count = item.Sum(x => x.RegisterCount);
                        List<bool> sendArray = new List<bool>();
                        foreach (var reg in item)
                        {
                            sendArray.Add((reg.Value == "1"));
                        }
                        result = WriteCoils(slaveID, (UInt16)startAddress, sendArray.ToArray(), responseTimeout: responseTimeout);
                        if (!result.IsSuccess && !isIgnoreFailed)
                        {
                            throw new Exception($"写起始地址从{startAddress}到{startAddress + count - 1}线圈失败，原因：{result.ErrorMessage}");
                        }
                    }
                }

                #endregion
            }

        }

        public ExecutionResult ReadWriteRegister(int slaveID, Register register, int responseTimeout = 300)
        {
            byte[] bytes;
            bool[] boolValues;
            ExecutionResult result = ExecutionResult.Failed();
            switch (register.RegisterType)
            {
                case RegisterType.保持寄存器:
                    if (register.OperationType == OperationType.Read)
                    {
                        result = ReadHoldingRegisters(slaveID, (UInt16)register.Address, (UInt16)register.RegisterCount, out bytes, responseTimeout: responseTimeout);
                        if (result.IsSuccess)
                        {
                            register.Value = register.GetValue(bytes,0);
                        }
                    }
                    else
                    {
                        result = WriteHoldingRegisters(slaveID, (UInt16)register.Address, register.GetBytes(), responseTimeout: responseTimeout);
                    }
                    break;
                case RegisterType.输入寄存器:
                    result = ReadInputRegisters(slaveID, (UInt16)register.Address, (UInt16)register.RegisterCount, out bytes, responseTimeout: responseTimeout);
                    if (result.IsSuccess)
                    {
                        register.Value = register.GetValue(bytes, 0);
                    }
                    break;
                case RegisterType.线圈:
                    if (register.OperationType == OperationType.Read)
                    {
                        result = ReadCoils(slaveID, (UInt16)register.Address, 1, out boolValues, responseTimeout: responseTimeout);
                        if (result.IsSuccess)
                        {
                            register.Value = boolValues[0] ? "1" : "0";
                        }
                    }
                    else
                    {
                        result = WriteCoil(slaveID, (UInt16)register.Address, register.Value == "1", responseTimeout: responseTimeout);
                    }
                    break;
                case RegisterType.离散量输入:
                    result = ReadDiscreteInputRegisters(slaveID, (UInt16)register.Address, 1, out boolValues, responseTimeout: responseTimeout);
                    if (result.IsSuccess)
                    {
                        register.Value = boolValues[0] ? "1" : "0";
                    }
                    break;
            }
            return result;
        }
        #endregion

        #region 私有函数
        private ExecutionResult SendCommand(int slaveID, ModbusCommand cmd, byte[] content,bool isNeedResponse=true,  int maxSendCount = 1, int responseTimeout = 300)
        {
            if (!IsConnected)
            {
                return ExecutionResult.Failed(Properties.Message.CommunicationUnconnected);
            }
            ProtocolBase obj = null;
            if (ProtocolType == ProtocolType.ModbusRTU)
            {
                obj = new ModbusRTU((byte)slaveID, cmd, content);
            }
            else
            {
                obj = new ModbusTCP((byte)slaveID, cmd, content);
            }
            var result = SendProtocol(obj, isNeedResponse, maxSendCount, responseTimeout);
            if (result.IsSuccess && result.Response != null)
            {
                // 报错
                if (result.Response.Content.Length == 1)
                {
                    //DeviceStatus = DeviceStatus.Warning;
                    // 报错码
                    switch (result.Response.Content[0])
                    {
                        case 0x01:
                            result.IsSuccess = false;
                            result.ErrorMessage = "功能码不支持";
                            break;
                        case 0x02:
                            result.IsSuccess = false;
                            result.ErrorMessage = "数据地址不合法";
                            break;
                        case 0x03:
                            result.IsSuccess = false;
                            result.ErrorMessage = "数据不合法";
                            break;
                        case 0x04:
                            result.IsSuccess = false;
                            result.ErrorMessage = "从设备故障";
                            break;
                        case 0x06:
                            result.IsSuccess = false;
                            result.ErrorMessage = "从设备忙";
                            break;
                        case 0x08:
                            result.IsSuccess = false;
                            result.ErrorMessage = "存储器奇偶性差错";
                            break;
                        case 0x0B:
                            result.IsSuccess = false;
                            result.ErrorMessage = "网关目标设备未响应";
                            break;
                        case 0x20:
                            result.IsSuccess = false;
                            result.ErrorMessage = "保护报警";
                            break;
                        case 0x40:
                            result.IsSuccess = false;
                            result.ErrorMessage = "CRC校验错误";
                            break;
                        default:
                            result.IsSuccess = false;
                            result.ErrorMessage = $"Modbus错误，错误码：0x{result.Response.Content[0].ToString("X2")}";
                            break;
                    }
                }
            }

            return result;
        }
        #endregion
    }
}

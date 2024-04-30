using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using WYW.Modbus.Clients;
using WYW.Modbus.Protocols;

namespace WYW.Modbus
{
    public class ModbusMaster : DeviceBase
    {
        public ModbusMaster()
        {

        }
        public ModbusMaster(ClientBase client, ProtocolType protocol) : base(client, protocol)
        {
        }

        #region 公共属性


        /// <summary>
        /// ModbusTCP事务处理标识，2个字节，这里用UInt16表示，大端对齐。ModbusTCP使用
        /// </summary>
        public UInt16 TransactionID { get; set; }

        #endregion

        #region 公共方法
        /// <summary>
        /// 读离散输入量，功能码0x02
        /// </summary>
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
                }
            }
            else
            {
                result.IsSuccess = false;
                result.ErrorMessage = "返回内容为空";
            }
            return result;
        }


        /// <summary>
        /// 读多个保持寄存器，功能码0x03
        /// </summary>
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
                }
            }
            return result;
        }

        /// <summary>
        /// 读多个保持寄存器，功能码0x03
        /// </summary>
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
                if (result.Response.Content.Length == count * 2 + 1)
                {
                    byteArray = result.Response.Content.SubBytes(1, result.Response.Content.Length - 1);
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "返回字节长度不满足协议要求";
                }
            }
            return result;
        }

        /// <summary>
        /// 读多个InputRegister，功能码0x04
        /// </summary>
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
                }
            }

            return result;
        }
        /// <summary>
        /// 读多个InputRegister，功能码0x04
        /// </summary>

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
            var result = SendCommand(slaveID, ModbusCommand.ReadMoreInputResiters, content.ToArray(), true, maxSendCount, responseTimeout);
            if (result.IsSuccess)
            {
                if (result.Response.Content.Length == count * 2 + 1)
                {
                    byteArray = result.Response.Content.SubBytes(1, result.Response.Content.Length - 1);
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "返回字节长度不满足协议要求";
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
        public ExecutionResult WriteCoil(int slaveID, UInt16 address, bool value, bool isNeedRespnse = true, int maxSendCount = 1, int responseTimeout = 300)
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
            return SendCommand(slaveID, ModbusCommand.WriteOneCoil, content.ToArray(), isNeedRespnse, maxSendCount, responseTimeout);
        }
        /// <summary>
        /// 写单个保持寄存器，功能码0x06
        /// </summary>

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
        public ExecutionResult WriteCoils(int slaveID, UInt16 startAddress, bool[] coil, bool isNeedRespnse = true, int maxSendCount = 1, int responseTimeout = 300)
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
            return SendCommand(slaveID, ModbusCommand.WriteMoreCoils, content.ToArray(), isNeedRespnse, maxSendCount, responseTimeout);
        }

        /// <summary>
        /// 写多个保持寄存器，功能码0x10
        /// </summary>
        /// <param name="startAddress">开始地址</param>
        /// <param name="value">寄存器值数组</param>
        /// <param name="maxSendCount">最大重发次数</param>
        /// <param name="responseTimeout">单次发送超时时间</param>
        /// <returns></returns>
        public ExecutionResult WriteHoldingRegisters(int slaveID, UInt16 startAddress, UInt16[] value, bool isNeedResponse = true, int maxSendCount = 1, int responseTimeout = 300)
        {
            List<byte> content = new List<byte>();
            content.AddRange(BitConverterHelper.GetBytes(startAddress, EndianType.BigEndian));
            content.AddRange(BitConverterHelper.GetBytes((UInt16)value.Length, EndianType.BigEndian));
            content.Add((byte)(value.Length * 2));
            foreach (var item in value)
            {
                content.AddRange(BitConverterHelper.GetBytes(item, EndianType.BigEndian));
            }
            return SendCommand(slaveID, ModbusCommand.WriteMoreHoldingRegisters, content.ToArray(), isNeedResponse, maxSendCount, responseTimeout);

        }

        /// <summary>
        /// 写多个保持寄存器，功能码0x10
        /// </summary>
        /// <param name="startAddress">开始地址</param>
        /// <param name="value">寄存器值字节数组，长度是2的倍数</param>
        /// <param name="maxSendCount">最大重发次数</param>
        /// <param name="responseTimeout">单次发送超时时间</param>
        /// <returns></returns>
        public ExecutionResult WriteHoldingRegisters(int slaveID, UInt16 startAddress, byte[] value, bool isNeedRespnse = true, int maxSendCount = 1, int responseTimeout = 300)
        {
            if (value.Length % 2 == 1)
            {
                return ExecutionResult.Failed("字节长度必须是偶数", null);
            }
            List<byte> content = new List<byte>();
            content.AddRange(BitConverterHelper.GetBytes(startAddress, EndianType.BigEndian));
            content.AddRange(BitConverterHelper.GetBytes((UInt16)(value.Length / 2), EndianType.BigEndian));
            content.Add((byte)(value.Length));
            content.AddRange(value);

            return SendCommand(slaveID, ModbusCommand.WriteMoreHoldingRegisters, content.ToArray(), isNeedRespnse, maxSendCount, responseTimeout);
        }


        /// <summary>
        /// 监控寄存器的值是否与目标值相匹配
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="targetValue">目标值</param>
        /// <param name="registerType">寄存器类型，默认HoldingRegister</param>
        /// <param name="timeout">超时时间，单位ms</param>
        /// <returns></returns>
        public bool MonitorRegister(int slaveID, UInt16 address, UInt16 targetValue, RegisterType registerType = RegisterType.HoldingRegister, int timeout = 2000)
        {
            UInt16[] registerValues;
            bool[] coilValues;
            DateTime dateTime = DateTime.Now;
            ExecutionResult result = null;
            while ((DateTime.Now - dateTime).TotalMilliseconds < timeout)
            {
                switch (registerType)
                {
                    case RegisterType.DiscreteInputRegister:
                        result = ReadDiscreteInputRegisters(slaveID, address, 1, out coilValues, responseTimeout: 100);
                        if (result.IsSuccess && coilValues[0] == (targetValue != 0))
                        {
                            return true;
                        }
                        break;
                    case RegisterType.Coil:
                        result = ReadCoils(slaveID, address, 1, out coilValues, responseTimeout: 100);
                        if (result.IsSuccess && coilValues[0] == (targetValue != 0))
                        {
                            return true;
                        }
                        break;
                    case RegisterType.InputRegister:
                        result = ReadInputRegisters(slaveID, address, 1, out registerValues, responseTimeout: 100);
                        if (result.IsSuccess && registerValues[0] == targetValue)
                        {
                            return true;
                        }
                        break;
                    case RegisterType.HoldingRegister:
                        result = ReadHoldingRegisters(slaveID, address, 1, out registerValues, responseTimeout: 100);
                        if (result.IsSuccess && registerValues[0] == targetValue)
                        {
                            return true;
                        }
                        break;
                }
                Thread.Sleep(10);
            }

            return false;
        }
        #endregion

        #region 心跳
        /// <summary>
        /// 创建心跳对象，使用读某个保持/输入/线圈寄存器判断心跳是否正常
        /// </summary>
        /// <param name="slaveID">从站地址</param>
        /// <param name="address">读取寄存器的地址</param>
        /// <returns></returns>
        public ProtocolBase CreateHeartBeatContent(byte slaveID, int address, int count = 1, RegisterType registerType = RegisterType.HoldingRegister, UInt16 transactionID = 0)
        {
            List<byte> content = new List<byte>();
            content.AddRange(BitConverterHelper.GetBytes((UInt16)address, EndianType.BigEndian));
            content.AddRange(BitConverterHelper.GetBytes((UInt16)count, EndianType.BigEndian));
            ModbusCommand cmd = ModbusCommand.ReadMoreHoldingRegisters;
            switch (registerType)
            {
                case RegisterType.HoldingRegister:
                    cmd = ModbusCommand.ReadMoreHoldingRegisters;
                    break;
                case RegisterType.InputRegister:
                    cmd = ModbusCommand.ReadMoreInputResiters;
                    break;
                case RegisterType.DiscreteInputRegister:
                    cmd = ModbusCommand.ReadMoreDiscreteInputRegisters;
                    break;
                case RegisterType.Coil:
                    cmd = ModbusCommand.ReadMoreCoils;
                    break;
            }
            if (Protocol == ProtocolType.ModbusRTU)
            {
                return new ModbusRTU(slaveID, cmd, content.ToArray());
            }
            else
            {
                return new ModbusTCP(slaveID, cmd, content.ToArray(), transactionID);
            }

        }
        #endregion

        #region 私有方法
        private ExecutionResult SendCommand(int slaveID, ModbusCommand cmd, byte[] content, bool isNeedResponse = true, int maxSendCount = 1, int responseTimeout = 300)
        {
            ModbusBase obj = null;
            if (Protocol == ProtocolType.ModbusRTU)
            {
                obj = new ModbusRTU((byte)slaveID, cmd, content);
            }
            else
            {
                obj = new ModbusTCP((byte)slaveID, cmd, content, TransactionID);
            }
            return SendProtocol(obj, isNeedResponse, maxSendCount, responseTimeout);
        }

        internal override ExecutionResult SendProtocol(ProtocolBase obj, bool isNeedResponse, int maxSendCount, int responseTimeout)
        {
            if (!Client.IsEstablished)
            {
                return ExecutionResult.Failed(Properties.Message.CommunicationUnconnected);
            }
            var receiveBuffer = new List<byte>();
            List<ProtocolBase> response = null;
            bool responseReceived = false;
            for (int i = 0; i < maxSendCount; i++)
            {
                DateTime startTime = DateTime.Now;
                if (LogEnabled)
                {
                    Logger.WriteLine(GetLogFolder(), $"[{obj.CreateTime:yyyy-MM-dd HH:mm:ss.fff}] [Tx] {obj.FriendlyText}");
                }
                if (IsPrintDebugInfo)
                {
                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Tx] {obj.FriendlyText}");
                }
                Client.Write(obj.FullBytes);
                if (!isNeedResponse)
                {
                    return ExecutionResult.Success(null);
                }
                while ((DateTime.Now - startTime).TotalMilliseconds < responseTimeout)
                {
                    Client.Read(ref receiveBuffer);
                    response = obj.GetResponse(receiveBuffer);
                    if (response.Count >= 1)
                    {
                        responseReceived = true;
                        break;
                    }
                    Thread.Sleep(1);
                }
                if (responseReceived)
                {
                    break;
                }
            }
            if (responseReceived)
            {
                if (LogEnabled)
                {
                    Logger.WriteLine(GetLogFolder(), $"[{response[0].CreateTime:yyyy-MM-dd HH:mm:ss.fff}] [Rx] {response[0].FriendlyText}");
                }
                if (IsPrintDebugInfo)
                {
                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Rx] {response[0].FriendlyText} ");
                }
                lastReceiveTime = DateTime.Now;
                IsConnected = true;
                // 报错
                if (response[0].Content.Length == 1)
                {
                    // 报错码
                    switch (response[0].Content[0])
                    {
                        case 0x01:
                            return ExecutionResult.Failed("功能码不支持", response[0]);
                        case 0x02:
                            return ExecutionResult.Failed("数据地址不合法", response[0]);
                        case 0x03:
                            return ExecutionResult.Failed("数据不合法", response[0]);
                        case 0x04:
                            return ExecutionResult.Failed("从设备故障", response[0]);
                        case 0x06:
                            return ExecutionResult.Failed("从设备忙", response[0]);
                        case 0x08:
                            return ExecutionResult.Failed("存储器奇偶性差错", response[0]);
                        case 0x0B:
                            return ExecutionResult.Failed("网关目标设备未响应", response[0]);
                        case 0x20:
                            return ExecutionResult.Failed("保护报警", response[0]);
                        case 0x40:
                            return ExecutionResult.Failed("CRC校验错误", response[0]);
                        default:
                            return ExecutionResult.Failed($"Modbus错误，错误码：0x{response[0].Content[0].ToString("X2")}", response[0]);
                    }
                }
                else
                {
                    return ExecutionResult.Success(response[0]);
                }
            }
            else
            {
                return ExecutionResult.Failed(Properties.Message.CommunicationTimeout);
            }
        }

        #endregion
    }
}

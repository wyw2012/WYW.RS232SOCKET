using WYW.Communication.Protocol;
using System;
using System.Collections.Generic;
using WYW.Communication.TransferLayer;

namespace WYW.Communication.ApplicationlLayer
{
    /// <summary>
    /// Modbus从站
    /// </summary>
    public class ModbusSlave : Device
    {

        public ModbusSlave(TransferBase client, int slaveID) : base(client)
        {
            SlaveID = slaveID;
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
        /// <summary>
        /// 从站节点ID
        /// </summary>
        public int SlaveID { get; }

        #region 公共方法
        /// <summary>
        /// 响应读保持寄存器指令
        /// </summary>
        /// <param name="value">寄存器值</param>
         /// <param name="transactionID">事务处理标识，2字节，大端对齐</param>
        public void ResponseReadMoreHoldingRegisters(UInt16[] value, UInt16 transactionID = 0)
        {
            List<byte> content = new List<byte>();
            content.AddRange(BigEndianBitConverter.GetBytes((UInt16)(value.Length * 2)));
            foreach (var item in value)
            {
                content.AddRange(BigEndianBitConverter.GetBytes(item));
            }
            SendCommand(ModbusCommand.ReadMoreHoldingRegisters, content.ToArray(), transactionID);

        }
        /// <summary>
        /// 响应写多个保持寄存器指令                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      
        /// </summary>
        /// <param name="startAddress">起始地址</param>
        /// <param name="count">寄存器数量</param>
        /// <param name="transactionID">事务处理标识，2字节，大端对齐</param>
        public void ResponseWriteMoreHoldingRegisters(UInt16 startAddress, UInt16 count, UInt16 transactionID = 0)
        {
            List<byte> content = new List<byte>();
            content.AddRange(BigEndianBitConverter.GetBytes(startAddress));
            content.AddRange(BigEndianBitConverter.GetBytes(count));
            SendCommand(ModbusCommand.WriteMoreHoldingRegisters, content.ToArray(), transactionID);
        }
        /// <summary>
        /// 响应写单个保持寄存器指令
        /// </summary>
        /// <param name="address">寄存器地址</param>
        /// <param name="value">寄存器值</param>
        /// <param name="transactionID">事务处理标识，2字节，大端对齐</param>
        public void ResponseWriteOneHoldingRegister(UInt16 address, UInt16 value, UInt16 transactionID = 0)
        {
            List<byte> content = new List<byte>();
            content.AddRange(BigEndianBitConverter.GetBytes(address));
            content.AddRange(BigEndianBitConverter.GetBytes(value));
            SendCommand(ModbusCommand.WriteOneHoldingRegister, content.ToArray(), transactionID);
        }
        /// <summary>
        /// 响应读写保持寄存器指令
        /// </summary>
        /// <param name="readCount">读寄存器的数量</param>
        /// <param name="writeValue">写寄存器的值数组</param>
        /// <param name="transactionID">事务处理标识，2字节，大端对齐</param>
        public void ResponseReadWriteHoldingRegisters(UInt16 readCount, UInt16[] writeValue, UInt16 transactionID = 0)
        {
            List<byte> content = new List<byte>();
            content.AddRange(BigEndianBitConverter.GetBytes((UInt16)(readCount*2)));
            foreach (var item in writeValue)
            {
                content.AddRange(BigEndianBitConverter.GetBytes(item));
            }
          
            SendCommand(ModbusCommand.ReadWriteHoldingRegisters, content.ToArray(), transactionID);
        }
        #endregion

        #region 私有函数
        private ExecutionResult SendCommand(ModbusCommand cmd, byte[] content, UInt16 transactionID)
        {
            ProtocolBase obj = null;
            if (ProtocolType == ProtocolType.ModbusRTU)
            {
                obj = new ModbusRTU((byte)SlaveID, cmd, content);
            }
            else
            {
                obj = new ModbusTCP((byte)SlaveID, cmd, content, transactionID);
            }
            return SendProtocol(obj, false);
        }
        #endregion
    }
}

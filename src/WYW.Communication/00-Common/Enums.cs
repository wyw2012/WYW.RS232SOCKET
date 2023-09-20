using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WYW.Communication
{
    /// <summary>
    /// 协议类型
    /// </summary>
    public enum ProtocolType
    {
        /// <summary>
        /// Hex
        /// </summary>
        HexBare = 0,
        /// <summary>
        /// 无任何校验的ASCII
        /// </summary>
        AsciiBare = 1,
        /// <summary>
        /// 带回车的ASCII协议
        /// </summary>
        AsciiCR = 2,
        /// <summary>
        /// 带换行的ASCII协议
        /// </summary>
        AsciiLF = 3,
        /// <summary>
        /// 带回车换行的ASCII
        /// </summary>
        AsciiCRLF = 4,
        /// <summary>
        /// 带一个字节校验位的ASCII
        /// </summary>
        AsciiCheckSum = 5,
        /// <summary>
        /// Modbus RTU协议
        /// </summary>
        ModbusRTU = 6,
        /// <summary>
        /// Modbus TCP协议
        /// </summary>
        ModbusTCP = 7,
    }
    /// <summary>
    /// Modbus指令
    /// </summary>
    public enum ModbusCommand
    {
        /// <summary>
        /// 读多个线圈
        /// </summary>
        ReadMoreCoils = 0x01,
        /// <summary>
        /// 写一个线圈
        /// </summary>
        WriteOneCoil = 0x05,
        /// <summary>
        /// 写多个线圈
        /// </summary>
        WriteMoreCoils = 0x0F,
        /// <summary>
        /// 读多个离散量输入寄存器
        /// </summary>
        ReadMoreDiscreteInputRegisters = 0x02,
        /// <summary>
        /// 读多个保持寄存器
        /// </summary>
        ReadMoreHoldingRegisters = 0x03,
        /// <summary>
        /// 读多个输出寄存器
        /// </summary>
        ReadMoreInputResiters = 0x04,
        /// <summary>
        /// 写一个保持寄存器
        /// </summary>
        WriteOneHoldingRegister = 0x06,
        /// <summary>
        /// 写多个保持寄存器
        /// </summary>
        WriteMoreHoldingRegisters = 0x10,
        /// <summary>
        /// 同时读写多个保持寄存器
        /// </summary>
        ReadWriteHoldingRegisters = 0x17,
    }
    public enum DeviceStatus
    {
        /// <summary>
        /// 未连接
        /// </summary>
        UnConnected = 0,
        /// <summary>
        /// 待机中
        /// </summary>
        Standy = 1,
        /// <summary>
        /// 运行中
        /// </summary>
        Running = 2,
        /// <summary>
        /// 警告，一般是发送超时，或者接收的数据不满足要求，自恢复
        /// </summary>
        Warning = 4,
        /// <summary>
        /// 错误，一般是设备故障，必须复位才能消除
        /// </summary>
        Error = 8,
    }
    public enum RegisterValueType
    {
        UInt16,
        Int16,
        UInt32,
        Int32,
        UInt64,
        Int64,
        Float,
        Double,
        UTF8,
    }
    /// <summary>
    /// 寄存器类型
    /// </summary>
    public enum RegisterType
    {
        保持寄存器,
        输入寄存器,
        离散量输入,
        线圈

    }
    public enum RegisterWriteType
    {
        读写,
        只读,
        只写,
    }
    /// <summary>
    /// 对齐方式
    /// </summary>
    public enum RegisterEndianType
    {
        /// <summary>
        /// 高位在后，例如：1 2 3 4
        /// </summary>
        小端模式 = 0,
        /// <summary>
        /// 高位在前，例如：4 3 2 1
        /// </summary>
        大端模式 = 1,

        ///// <summary>
        ///// 2 1 4 3
        ///// </summary>
        //大端反转,
        ///// <summary>
        /////  3 4 1 2
        ///// </summary>
        //小端反转
    }
    public enum ModbusProtocolType
    {
        /// <summary>
        /// 根据传输介质自动匹配协议
        /// </summary>
        Auto,
        ModbusRTU,
        ModbusTCP
    }
    /// <summary>
    /// 操作类型
    /// </summary>
    public enum OperationType
    {
        Write,
        Read,
    }
    /// <summary>
    /// 心跳事件触发条件
    /// </summary>
    public enum HeartbeatTriggerCondition
    {
        /// <summary>
        /// 当连接状态改变时
        /// </summary>
        Changed = 0,
        /// <summary>
        /// 始终触发，无论心跳是否建立
        /// </summary>
        Always = 1,
    }
}

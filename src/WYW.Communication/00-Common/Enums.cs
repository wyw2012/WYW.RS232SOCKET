using System;
using System.Collections.Generic;
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
        /// 带回车换行的ASCII
        /// </summary>
        AsciiCRLF = 3,
        /// <summary>
        /// 带一个字节校验位的ASCII
        /// </summary>
        AsciiCheckSum = 4,
        /// <summary>
        /// Modbus RTU协议
        /// </summary>
        ModbusRTU = 5,
        /// <summary>
        /// Modbus TCP协议
        /// </summary>
        ModbusTCP = 6,
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
}

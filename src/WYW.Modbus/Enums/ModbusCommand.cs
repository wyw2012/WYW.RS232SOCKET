using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WYW.Modbus
{
    enum ModbusCommand
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
}

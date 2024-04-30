using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WYW.Modbus
{
    public enum ProtocolType
    {
        /// <summary>
        /// 带回车\r的ASCII协议
        /// </summary>
        AsciiCR = 0,
        /// <summary>
        /// 带换行\n的ASCII协议
        /// </summary>
        AsciiLF = 1,
        /// <summary>
        /// 带回车换行\r\n的ASCII
        /// </summary>
        AsciiCRLF = 2,
        ModbusRTU = 3,
        ModbusTCP = 4,
    }
}

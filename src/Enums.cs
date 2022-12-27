using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WYW.RS232SOCKET
{
    enum ModbusCommand
    {
        ReadMoreRegisters = 0x03,
        WriteOneRegisters = 0x06,
        WriteMoreRegisters = 0x10,
        ReadWriteRegisters = 0x17,
    }

    enum CheckType
    {
        None=0,
        /// <summary>
        /// 文本末尾加回车
        /// </summary>
        CR =1,
        /// <summary>
        /// 文本末尾加回车换行
        /// </summary>
        CRLF=2,
        /// <summary>
        /// 文本末尾加一个字节的累加和
        /// </summary>
        CheckSum=3,
       

    }

}

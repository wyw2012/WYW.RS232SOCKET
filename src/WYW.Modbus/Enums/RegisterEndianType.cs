using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WYW.Modbus
{
    public enum RegisterEndianType
    {
        /// <summary>
        /// 高位在后，例如：1 2 3 4
        /// </summary>
        [Description("小端模式")]
        LittleEndian = 0,
        /// <summary>
        /// 高位在前，例如：4 3 2 1
        /// </summary>
        [Description("大端模式")]
        BigEndian = 1,
        ///// <summary>
        ///// 2 1 4 3
        ///// </summary>
        //大端反转,
        ///// <summary>
        /////  3 4 1 2
        ///// </summary>
        //小端反转
    }
}

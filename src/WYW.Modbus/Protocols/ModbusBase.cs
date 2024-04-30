using System;
using System.Collections.Generic;
using System.Linq;

namespace WYW.Modbus.Protocols
{
    abstract class ModbusBase:ProtocolBase
    {
        #region  公共属性
        /// <summary>
        /// 从站节点
        /// </summary>
        public byte SlaveID { get; protected set; }
        /// <summary>
        /// Modbus指令
        /// </summary>
        public ModbusCommand Command { get; protected set; }
        #endregion
    }
}

using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WYW.Modbus.Clients
{
    public abstract class ClientBase
    {
        #region 属性
        public bool IsOpen { get; protected set; }
        /// <summary>
        /// 是否已经建立连接
        /// </summary>
        public bool IsEstablished { get; protected set; }

        public string ErrorMessage { get; protected set; }

        public Logger Logger { get; set; }
  
        #endregion

        #region  公共方法
        public abstract void Open();
        public abstract void Close();
        public abstract bool Write(byte[] buffer);
        public abstract bool Read(ref List<byte> receiveBuffer);
        public abstract void ClearReceiveBuffer();

        #endregion
    }
}

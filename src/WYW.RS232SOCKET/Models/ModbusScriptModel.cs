using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WYW.Communication.Models;

namespace WYW.RS232SOCKET.Models
{
    internal class ModbusScriptModel:Register
    {
        private int id;

        /// <summary>
        /// 
        /// </summary>
        public int ID { get => id; set => SetProperty(ref id, value); }


        private int sleepTime;

        /// <summary>
        /// 
        /// </summary>
        public int SleepTime { get => sleepTime; set => SetProperty(ref sleepTime, value); }

        private string status;

        /// <summary>
        /// 状态，成功或者失败
        /// </summary>
        public string Status { get => status; set => SetProperty(ref status, value); }

    }
}

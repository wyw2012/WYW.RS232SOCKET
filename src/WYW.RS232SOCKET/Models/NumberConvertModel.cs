using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WYW.Communication;

namespace WYW.RS232SOCKET.Models
{
    internal class NumberConvertModel : ObservableObject
    {
        private int id;

        /// <summary>
        /// 
        /// </summary>
        public int ID { get => id; set => SetProperty(ref id, value); }

        private string _value="1";

        /// <summary>
        /// 
        /// </summary>
        public string Value { get => _value; set => SetProperty(ref _value, value); }


        private RegisterValueType valueType = RegisterValueType.UInt16;

        /// <summary>
        /// 
        /// </summary>
        public RegisterValueType ValueType { get => valueType; set => SetProperty(ref valueType, value); }

        private RegisterEndianType endianType = RegisterEndianType.小端模式;

        /// <summary>
        /// 
        /// </summary>
        public RegisterEndianType EndianType { get => endianType; set => SetProperty(ref endianType, value); }

    }
}

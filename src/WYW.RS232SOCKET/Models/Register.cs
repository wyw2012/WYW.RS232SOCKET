using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WYW.RS232SOCKET.Common;

namespace WYW.RS232SOCKET.Models
{
    internal class Register : ObservableObject
    {
        public Register()
        {

        }
        public Register(int address)
        {
            Address = address;
        }
        public Register(int address, int value)
        {
            Address = address;
            Value = value;
        }
        private bool isChecked=true;
        private RegisterValueType valueType;
        private RegisterWriteType writeType;
        private int address;
        private double _value;
        private string description;
        /// <summary>
        /// 
        /// </summary>
        public int Address
        {
            get { return address; }
            set
            {
                if (address != value)
                {
                    address = value;
                    OnPropertyChanged("Address");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public double Value
        {
            get { return _value; }
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged("Value");
                }
            }
        }

        /// <summary>
        /// 值类型，例如UInt16、Float
        /// </summary>
        public RegisterValueType ValueType
        {
            get => valueType;
            set
            {
                SetProperty(ref valueType, value);
                switch (valueType)
                {
                    case RegisterValueType.UInt32:
                    case RegisterValueType.Float:
                        RegisterCount = 2;
                        break;
                    case RegisterValueType.Double:
                        RegisterCount = 4;
                        break;
                    default:
                        RegisterCount = 1;
                        break;
                }
            }
        }

        /// <summary>
        /// 是否选中，选中后的可以进行读写操作
        /// </summary>
        public bool IsChecked
        {
            get => isChecked;
            set => SetProperty(ref isChecked, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public RegisterWriteType WriteType
        {
            get => writeType;
            set => SetProperty(ref writeType, value);
        }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }
        /// <summary>
        /// 占用的寄存器数量，一个寄存器占2个字节
        /// </summary>
        internal int RegisterCount { get; private set; } = 1;

        public byte[] ToBytes()
        {
            
            switch(ValueType)
            {
                case RegisterValueType.UInt16:
                    return BigEndianBitConverter.GetBytes((UInt16)Math.Round(Value));
                case RegisterValueType.UInt32:
                    return BigEndianBitConverter.GetBytes((UInt32)Math.Round(Value));
                case RegisterValueType.Float:
                    return BigEndianBitConverter.GetBytes((float)Value);
                case RegisterValueType.Double:
                    return BigEndianBitConverter.GetBytes((double)Value);
            }
            return new byte[0];
        }
    }
}

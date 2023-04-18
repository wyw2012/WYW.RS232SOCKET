using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            Value = value.ToString();
        }
        private bool isChecked=true;
        private RegisterValueType valueType= RegisterValueType.UInt16;
        private RegisterWriteType writeType= RegisterWriteType.读写;
        private RegisterEndianType endianType = RegisterEndianType.大端模式;
        private int address;
        private string _value="0";
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
        public string Value
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
                    case RegisterValueType.Int32:
                    case RegisterValueType.UInt32:
                    case RegisterValueType.Float:
                        RegisterCount = 2;
                        break;
                    case RegisterValueType.UInt64:
                    case RegisterValueType.Int64:
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
        /// 端类型
        /// </summary>
        public RegisterEndianType EndianType
        {
            get => endianType;
            set => SetProperty(ref endianType, value);
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

        public byte[] GetBytes()
        {
            
            switch(ValueType)
            {
                case RegisterValueType.Int16:
                    return BigEndianBitConverter.GetBytes(Int16.Parse(Value));
                case RegisterValueType.UInt16:
                    return BigEndianBitConverter.GetBytes(UInt16.Parse(Value));
                case RegisterValueType.Int32:
                    return BigEndianBitConverter.GetBytes(Int32.Parse(Value));
                case RegisterValueType.UInt32:
                    return BigEndianBitConverter.GetBytes(UInt32.Parse(Value));
                case RegisterValueType.Float:
                    return BigEndianBitConverter.GetBytes(float.Parse(Value));
                case RegisterValueType.Double:
                    return BigEndianBitConverter.GetBytes(double.Parse(Value));
                case RegisterValueType.Int64:
                    return BigEndianBitConverter.GetBytes(Int64.Parse(Value));
                case RegisterValueType.UInt64:
                    return BigEndianBitConverter.GetBytes(UInt64.Parse(Value));

            }
            return new byte[0];
        }

        public string GetValue(byte[] fullBytes,int startIndex)
        {

            switch (ValueType)
            {
                case RegisterValueType.Int16:
                    return BigEndianBitConverter.ToInt16(fullBytes, startIndex).ToString();
                case RegisterValueType.UInt16:
                    return BigEndianBitConverter.ToUInt16(fullBytes, startIndex).ToString();
                case RegisterValueType.Int32:
                    return BigEndianBitConverter.ToInt32(fullBytes, startIndex).ToString();
                case RegisterValueType.UInt32:
                    return BigEndianBitConverter.ToUInt32(fullBytes, startIndex).ToString();
                case RegisterValueType.Int64:
                    return BigEndianBitConverter.ToInt64(fullBytes, startIndex).ToString();
                case RegisterValueType.UInt64:
                    return BigEndianBitConverter.ToUInt64(fullBytes, startIndex).ToString();
                case RegisterValueType.Float:
                    return  BigEndianBitConverter.ToSingle(fullBytes, startIndex).ToString();
                case RegisterValueType.Double:
                    return  BigEndianBitConverter.ToDouble(fullBytes, startIndex).ToString();

            }
            return "0";
        }

    }
}

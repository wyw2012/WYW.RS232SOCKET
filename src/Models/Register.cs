using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WYW.RS232SOCKET.Models
{
    internal class Register: ObservableObject
    {
        public Register(int address, int value)
        {
            Address = address;
            Value = value;
        }
        private int address;
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
        private int _value;

        /// <summary>
        /// 
        /// </summary>
        public int Value
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

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WYW.Communication
{
    class BigEndianBitConverter
    {
        public static byte[] GetBytes(uint value)
        {
            return BitConverter.GetBytes(value).Reverse().ToArray();
        }
        public static byte[] GetBytes(ushort value)
        {
            return BitConverter.GetBytes(value).Reverse().ToArray();
        }

        public static byte[] GetBytes(float value)
        {
            return BitConverter.GetBytes(value).Reverse().ToArray();
        }

        public static byte[] GetBytes(long value)
        {
            return BitConverter.GetBytes(value).Reverse().ToArray();
        }
        public static byte[] GetBytes(ulong value)
        {
            return BitConverter.GetBytes(value).Reverse().ToArray();
        }
        public static byte[] GetBytes(short value)
        {
            return BitConverter.GetBytes(value).Reverse().ToArray();
        }

        public static byte[] GetBytes(double value)
        {
            return BitConverter.GetBytes(value).Reverse().ToArray();
        }
        public static byte[] GetBytes(char value)
        {
            return BitConverter.GetBytes(value).Reverse().ToArray();
        }
        public static byte[] GetBytes(int value)
        {
            return BitConverter.GetBytes(value).Reverse().ToArray();
        }

        public static byte[] GetBytes(bool value)
        {
            return BitConverter.GetBytes(value).Reverse().ToArray();
        }

        public static double ToDouble(byte[] value, int startIndex=0)
        {
            if (value.Length < startIndex + 8)
            {
                throw new ArgumentException();
            }
            var newArray = value.SubBytes(startIndex, 8).Reverse().ToArray();
            return BitConverter.ToDouble(newArray, 0);
        }

        public static short ToInt16(byte[] value, int startIndex=0)
        {
            if (value.Length < startIndex + 2)
            {
                throw new ArgumentException();
            }
            var newArray = value.SubBytes(startIndex, 2).Reverse().ToArray();
            return BitConverter.ToInt16(newArray, 0);
        }
        public static int ToInt32(byte[] value, int startIndex=0)
        {
            if (value.Length < startIndex + 4)
            {
                throw new ArgumentException();
            }
            var newArray = value.SubBytes(startIndex, 4).Reverse().ToArray();
            return BitConverter.ToInt32(newArray, 0);
        }


        public static long ToInt64(byte[] value, int startIndex=0)
        {
            if (value.Length < startIndex + 8)
            {
                throw new ArgumentException();
            }
            var newArray = value.SubBytes(startIndex, 8).Reverse().ToArray();
            return BitConverter.ToInt64(newArray, 0);
        }
        public static float ToSingle(byte[] value, int startIndex=0)
        {
            if (value.Length < startIndex + 4)
            {
                throw new ArgumentException();
            }
            var newArray = value.SubBytes(startIndex, 4).Reverse().ToArray();
            return BitConverter.ToSingle(newArray, 0);
        }
        public static ushort ToUInt16(byte[] value, int startIndex=0)
        {
            if (value.Length < startIndex + 2)
            {
                throw new ArgumentException();
            }
            var newArray = value.SubBytes(startIndex, 2).Reverse().ToArray();
            return BitConverter.ToUInt16(newArray, 0);
        }

        public static uint ToUInt32(byte[] value, int startIndex=0)
        {
            if (value.Length < startIndex + 4)
            {
                throw new ArgumentException();
            }
            var newArray = value.SubBytes(startIndex, 4).Reverse().ToArray();
            return BitConverter.ToUInt32(newArray, 0);
        }
        public static ulong ToUInt64(byte[] value, int startIndex=0)
        {
            if (value.Length < startIndex + 8)
            {
                throw new ArgumentException();
            }
            var newArray = value.SubBytes(startIndex, 8).Reverse().ToArray();
            return BitConverter.ToUInt64(newArray, 0);
        }

    }
}

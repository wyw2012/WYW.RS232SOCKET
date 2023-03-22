using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WYW.RS232SOCKET
{
    static class ExtensionMethod
    {
        public static string ToHexString(this byte[] source)
        {
           return string.Join(" ", source.Select(x => x.ToString("X2")));
        }
   
        public static string ToUTF8(this byte[] source)
        {
            return Encoding.UTF8.GetString(source);
        }
        public static byte[] SubBytes(this byte[] source, int startIndex, int length)
        {
            if (startIndex + length > source.Length)
            {
                throw new ArgumentException("参数错误，起始索引与长度之和大于字节数组长度");
            }
            var result = new byte[length];
            Array.Copy(source, startIndex, result, 0, length);
            return result;
        }
    }
}

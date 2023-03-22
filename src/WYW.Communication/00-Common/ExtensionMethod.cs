using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WYW.Communication
{
    /// <summary>
    /// 扩展方法
    /// </summary>
    static class ExtensionMethod
    {
        /// <summary>
        /// 将字节数组转换成十六进制显示的字符串
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 匹配并返回字符串中第一个浮点数
        /// </summary>
        /// <param name="source"></param>
        /// <returns>匹配不成功则返回null</returns>
        public static double? RegexDouble(this string source)
        {
            var matchData = Regex.Match(source, @"(-?.[\d]){1,20}");
            while (matchData.Success)
            {
                return double.Parse(matchData.Value);
            }
            return null;
        }
        

}
}

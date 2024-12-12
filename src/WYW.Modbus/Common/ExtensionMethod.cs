using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WYW.Modbus
{
    /// <summary>
    /// 扩展方法
    /// </summary>
    public static class ExtensionMethod
    {
        #region String转换
        /// <summary>
        /// 十六进制字符串转换成字节数组
        /// </summary>
        /// <param name="text">十六进制字符串</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static byte[] ToHexArray(this string text)
        {
            var result = new List<byte>();
            var chars = Regex.Replace(text, @"\s", ""); // 剔除空格
            if (chars.Length % 2 == 1)
            {
                throw new ArgumentException($"The length of string must been even");
            }
            var hexs = Regex.Split(chars, @"(?<=\G.{2})(?!$)");   // 两两分组
            try
            {
                result.AddRange(hexs.Select(x => Convert.ToByte(x, 16)).ToArray());
            }
            catch
            {
                throw new ArgumentException($"The string cannot be converted to hexadecimal");
            }
            return result.ToArray();
        }
        public static double[] ToDoubleArray(this string text, char splitChar = ',')
        {
            if (splitChar != '\n' && splitChar != '\r' && splitChar != '\t' && splitChar != '\f' && splitChar != ' ')
            {
                text = Regex.Replace(text, "\\s", "");
            }
            return text.Split(splitChar).Select(x => double.Parse(x)).ToArray();
        }

        public static bool TryToDoubleArray(this string text, out double[] result, char splitChar = ',')
        {
            result = new double[0];
            if (splitChar != '\n' && splitChar != '\r' && splitChar != '\t' && splitChar != '\f' && splitChar != ' ')
            {
                text = Regex.Replace(text, "\\s", "");
            }
            try
            {
                result= text.Split(splitChar).Select(x => double.Parse(x)).ToArray();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static int[] ToInt32Array(this string text, char splitChar = ',')
        {
            if (splitChar != '\n' && splitChar != '\r' && splitChar != '\t' && splitChar != '\f' && splitChar != ' ')
            {
                text = Regex.Replace(text, "\\s", "");
            }
            return text.Split(splitChar).Select(x => int.Parse(x)).ToArray();
        }
        public static bool TryToInt32Array(this string text, out Int32[] result, char splitChar = ',')
        {
            result = new Int32[0];
            if (splitChar != '\n' && splitChar != '\r' && splitChar != '\t' && splitChar != '\f' && splitChar != ' ')
            {
                text = Regex.Replace(text, "\\s", "");
            }
            try
            {
                result = text.Split(splitChar).Select(x => Int32.Parse(x)).ToArray();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string GetMD5(this string source)
        {
            var bytes = Encoding.UTF8.GetBytes(source);
            return bytes.GetMD5();
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
        /// <summary>
        /// 剔除特殊字符，使之满足windows文件命名规则
        /// </summary>
        /// <param name="chars"></param>
        /// <returns></returns>
        public static string ToFileName(this string chars)
        {
            return Regex.Replace(chars, @"[\/\\\|\<\>\*\:\?]", " ");
        }
        /// <summary>
        /// 判断字符串是否是IP地址
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public static bool IsIPV4(this string ipAddress)
        {
            return Regex.IsMatch(ipAddress, @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
        }
        /// <summary>
        /// 每两个字符之间加入空格
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string AddSpace(this string text)
        {
            return string.Join(" ", Regex.Split(text, @"(?<=\G.{2})(?!$)"));
        }

        //从给定的枚举类型Type中，通过枚举描述找到对应的枚举值，
        public static Enum GetEnumByDescription<T>(this string description)
        {
            if (string.IsNullOrEmpty(description))
            {
                return null;
            }
            Array values = Enum.GetValues(typeof(T));
            foreach (Enum item in values)
            {
                if (description == item.ToString())
                {
                    return item;
                }
                else if (description == GetDescription(item))
                {
                    return item;
                }
            }
            return null;
        }
        #endregion

        #region Byte[]转换
        /// <summary>
        /// 将字节数组转换成UTF-8格式的字符串
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToASCII(this byte[] source)
        {
            return Encoding.ASCII.GetString(source);
        }
        /// <summary>
        /// 将字节数组转换成UTF-8格式的字符串
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToUTF8(this byte[] source)
        {
            return Encoding.UTF8.GetString(source);
        }
        /// <summary>
        /// 将字节数组转换成十六进制显示的字符串
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToHexString(this IEnumerable<byte> source)
        {
            return string.Join(" ", source.Select(x => x.ToString("X2")));
        }
        /// <summary>
        /// 将字节数组转换成UTF-8格式的字符串
        /// </summary>
        /// <param name="source"></param>
        /// <param name="startIndex">起始地址</param>
        /// <param name="length">字节长度</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string ToUTF8(this byte[] source, int startIndex, int length)
        {
            if (startIndex + length > source.Length)
            {
                throw new ArgumentException("The sum of the start index and length is greater than the length of the byte array.");
            }
            return Encoding.UTF8.GetString(source, startIndex, length);
        }
        public static byte[] SubBytes(this byte[] source, int startIndex, int length)
        {
            if (startIndex + length > source.Length)
            {
                throw new ArgumentException("The sum of the start index and length is greater than the length of the byte array.");
            }
            var result = new byte[length];
            Array.Copy(source, startIndex, result, 0, length);
            return result;
        }
        public static string GetMD5(this byte[] source)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] data = md5.ComputeHash(source);
            var sb = new StringBuilder();
            for (var i = 0; i < data.Length; i++)
            {
                sb.Append(data[i].ToString("X2"));
            }
            return sb.ToString();
        }
        #endregion

        #region Enum
        /// <summary>
        /// 获取枚举的DescriptionAttribute标记内容
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string GetDescription(this Enum source)
        {
            var field = source.GetType().GetField(source.ToString());
            var customAttribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            return customAttribute == null ? source.ToString() : ((DescriptionAttribute)customAttribute).Description;
        }
        #endregion

        #region Bit Byte Int16 Int32 Int64 

        public static int GetBit(this UInt16 source, int index)
        {
            if (index > 15)
            {
                throw new ArgumentException("The index must been less than 15.");
            }
            return (source >> index) & 1;
        }

        public static byte[] GetUInt16Bytes(this int value, EndianType endianType = EndianType.BigEndian)
            => BitConverterHelper.GetBytes((ushort)value, endianType);
        public static byte[] GetUInt32Bytes(this int value, EndianType endianType = EndianType.BigEndian)
               => BitConverterHelper.GetBytes((uint)value, endianType);
        public static byte[] GetFloatBytes(this double value, EndianType endianType = EndianType.BigEndian)
          => BitConverterHelper.GetBytes((float)value, endianType);

        public static ushort ToUInt16(this byte[] data, int startIndex, EndianType endianType = EndianType.BigEndian)
            => BitConverterHelper.ToUInt16(data, startIndex, endianType);
        public static short ToInt16(this byte[] data, int startIndex, EndianType endianType = EndianType.BigEndian)
          => BitConverterHelper.ToInt16(data, startIndex, endianType);
        public static uint ToUInt32(this byte[] data, int startIndex, EndianType endianType = EndianType.BigEndian)
            => BitConverterHelper.ToUInt32(data, startIndex, endianType);
        public static int ToInt32(this byte[] data, int startIndex, EndianType endianType = EndianType.BigEndian)
          => BitConverterHelper.ToInt32(data, startIndex, endianType);
        public static float ToSingle(this byte[] data, int startIndex, EndianType endianType = EndianType.BigEndian)
            => BitConverterHelper.ToSingle(data, startIndex, endianType);
        public static double ToDouble(this byte[] data, int startIndex, EndianType endianType = EndianType.BigEndian)
          => BitConverterHelper.ToDouble(data, startIndex, endianType);

        #endregion

        #region Compare
        public static bool IsEquals(this byte[] source, byte[] target)
        {
            if (source != null && target != null)
            {
                if (source.Length != target.Length)
                {
                    return false;
                }
                else
                {
                    for (int i = 0; i < source.Length; i++)
                    {
                        if (source[i] != target[i])
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            else if (source == null && target == null)
            {
                return true;
            }
            else
            {
                return false;
            }

        }
        #endregion

        #region Object
        /// <summary>
        /// 深度拷贝，类需要添加标记[Serializable]
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input"></param>
        /// <returns></returns>
        public static T DeepClone<T>(this T input)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, input);
                ms.Position = 0;
                return (T)formatter.Deserialize(ms);
            }
        }


        public static IEnumerable<T> DataTableToList<T>(this DataTable dataTable)
        {
            List<T> list = new List<T>();
            foreach (DataRow row in dataTable.Rows)
            {
                T item = Activator.CreateInstance<T>();
                foreach (DataColumn column in dataTable.Columns)
                {
                    PropertyInfo property = item.GetType().GetProperty(column.ColumnName);
                    if (property != null)
                    {
                        property.SetValue(item, row[column], null);
                    }
                }
                list.Add(item);
            }
            return list;
        }
        #endregion
    }
}

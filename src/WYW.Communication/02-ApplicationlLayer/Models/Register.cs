using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WYW.Communication.Models
{
    public class Register : ObservableObject
    {
        #region 构造函数
        public Register()
        {

        }
        public Register(int address)
        {
            AddressChar = $"0x{address:X2}";
        }
        public Register(int address, int value)
        {
            AddressChar = $"0x{address:X2}";
            Value = value.ToString();
        }
        public Register(int address, int value, RegisterType registerType)
        {
            AddressChar = $"0x{address:X2}";
            Value = value.ToString();
            RegisterType = registerType;
        }
        #endregion

        #region 属性
        private bool isChecked = true;
        private RegisterType registerType = RegisterType.保持寄存器;
        private RegisterValueType valueType = RegisterValueType.UInt16;
        private RegisterWriteType writeType = RegisterWriteType.读写;
        private RegisterEndianType endianType = RegisterEndianType.大端模式;
        private int address;
        private string _value = "0";
        private string description;
        private string unit;
        private int registerCount = 1;
        private OperationType operationType = OperationType.Read;
        /// <summary>
        /// 地址
        /// </summary>
        public int Address
        {
            get => address;
            private set => SetProperty(ref address, value);
        }

        private string addressChar;

        /// <summary>
        /// Address的字符形式
        /// </summary>
        public string AddressChar
        {
            get => addressChar;
            set
            {
                SetProperty(ref addressChar, value);
                if (!string.IsNullOrEmpty(value))
                {
                    try
                    {
                        if (value.ToLower().StartsWith("0x"))
                        {
                            Address = Convert.ToInt32(value, 16);
                        }
                        else
                        {
                            Address = Convert.ToInt32(value, 10);
                        }
                    }
                    catch
                    {
                    }

                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        /// <summary>
        /// 单位
        /// </summary>
        public string Unit
        {
            get => unit;
            set => SetProperty(ref unit, value);
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
        /// 端类型，默认大端对齐
        /// </summary>
        public RegisterEndianType EndianType
        {
            get => endianType;
            set => SetProperty(ref endianType, value);
        }

        /// <summary>
        /// 支持读写类型
        /// </summary>
        public RegisterWriteType WriteType
        {
            get => writeType;
            set
            {
                SetProperty(ref writeType, value);
                switch (writeType)
                {
                    case RegisterWriteType.只读:
                        OperationType = OperationType.Read;
                        break;
                    case RegisterWriteType.只写:
                        OperationType = OperationType.Write;
                        break;
                }
            }
        }


        /// <summary>
        /// 寄存器类型
        /// </summary>
        public RegisterType RegisterType
        {
            get => registerType;
            set
            {
                SetProperty(ref registerType, value);
                switch (value)
                {
                    case RegisterType.保持寄存器:
                        WriteType = RegisterWriteType.读写;
                        break;
                    case RegisterType.输入寄存器:
                        WriteType = RegisterWriteType.只读;
                        break;
                    case RegisterType.离散量输入:
                        WriteType = RegisterWriteType.只读;
                        break;
                    case RegisterType.线圈:
                        WriteType = RegisterWriteType.读写;
                        ValueType = RegisterValueType.UInt16;
                        break;
                }
            }
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
        /// 占用的寄存器数量，一个寄存器占2个字节。当ValueType=String时才允许外部写
        /// </summary>
        public int RegisterCount
        {
            get => registerCount;
            set => SetProperty(ref registerCount, value);
        }

        /// <summary>
        /// 操作类型，受限于寄存器的读写类型
        /// </summary>
        public OperationType OperationType { get => operationType; set => SetProperty(ref operationType, value); }


        #endregion

        #region 公共方法
        /// <summary>
        /// 获取输入寄存器或者保持寄存器值的字节
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes()
        {
            if (string.IsNullOrEmpty(Value))
            {
                return new byte[RegisterCount * 2];
            }
            try
            {
                switch (ValueType)
                {
                    case RegisterValueType.Int16:
                        return BitConverterHelper.GetBytes(Int16.Parse(Value), (EndianType)endianType);
                    case RegisterValueType.UInt16:
                        return BitConverterHelper.GetBytes(UInt16.Parse(Value), (EndianType)endianType);
                    case RegisterValueType.Int32:
                        return BitConverterHelper.GetBytes(Int32.Parse(Value), (EndianType)endianType);
                    case RegisterValueType.UInt32:
                        return BitConverterHelper.GetBytes(UInt32.Parse(Value), (EndianType)endianType);
                    case RegisterValueType.Float:
                        return BitConverterHelper.GetBytes(float.Parse(Value), (EndianType)endianType);
                    case RegisterValueType.Double:
                        return BitConverterHelper.GetBytes(double.Parse(Value), (EndianType)endianType);
                    case RegisterValueType.Int64:
                        return BitConverterHelper.GetBytes(Int64.Parse(Value), (EndianType)endianType);
                    case RegisterValueType.UInt64:
                        return BitConverterHelper.GetBytes(UInt64.Parse(Value), (EndianType)endianType);
                    case RegisterValueType.UTF8:
                        byte[] bytes = new byte[registerCount * 2];
                        if (!string.IsNullOrEmpty(Value))
                        {
                            var charArray = Encoding.UTF8.GetBytes(Value);
                            Array.Copy(charArray, bytes, Math.Min(bytes.Length, charArray.Length));
                        }
                        if (EndianType == RegisterEndianType.小端模式)
                        {
                            return bytes.Reverse().ToArray();
                        }
                        else
                        {
                            return bytes;
                        }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return new byte[RegisterCount * 2];
        }

        public string GetValue(byte[] fullBytes, int startIndex)
        {

            switch (ValueType)
            {
                case RegisterValueType.Int16:
                    return BitConverterHelper.ToInt16(fullBytes, startIndex, (EndianType)endianType).ToString();
                case RegisterValueType.UInt16:
                    return BitConverterHelper.ToUInt16(fullBytes, startIndex, (EndianType)endianType).ToString();
                case RegisterValueType.Int32:
                    return BitConverterHelper.ToInt32(fullBytes, startIndex, (EndianType)endianType).ToString();
                case RegisterValueType.UInt32:
                    return BitConverterHelper.ToUInt32(fullBytes, startIndex, (EndianType)endianType).ToString();
                case RegisterValueType.Int64:
                    return BitConverterHelper.ToInt64(fullBytes, startIndex, (EndianType)endianType).ToString();
                case RegisterValueType.UInt64:
                    return BitConverterHelper.ToUInt64(fullBytes, startIndex, (EndianType)endianType).ToString();
                case RegisterValueType.Float:
                    return BitConverterHelper.ToSingle(fullBytes, startIndex, (EndianType)endianType).ToString();
                case RegisterValueType.Double:
                    return BitConverterHelper.ToDouble(fullBytes, startIndex, (EndianType)endianType).ToString();
                case RegisterValueType.UTF8:
                    var minLength = Math.Min(RegisterCount * 2, fullBytes.Length - startIndex);
                    if (EndianType == RegisterEndianType.小端模式)
                    {
                        return fullBytes.Reverse().ToArray().ToUTF8(startIndex, minLength).Replace("\0", "");
                    }
                    else
                    {
                        return fullBytes.ToUTF8(startIndex, minLength).Replace("\0", "");
                    }
            }
            return "0";
        }

        #endregion

        #region 静态方法
        /// <summary>
        /// 验证寄存器数组地址与寄存器个数是否满足要求
        /// </summary>
        /// <param name="registers"></param>
        /// <exception cref="Exception"></exception>
        public static void ValicateAddress(IEnumerable<Register> registers)
        {
            var registersArray = registers.GroupBy(x => x.RegisterType); // 按寄存器类型分组
            foreach (var reg in registersArray)
            {
                var repeatArray = reg.GroupBy(x => x.Address).Where(x => x.Count() > 1).Select(x => x.Key);

                if (repeatArray.Count() > 0)
                {
                    throw new Exception($"{reg.FirstOrDefault().RegisterType}存在重复的地址：{string.Join(", ", repeatArray)}");
                }
                foreach (var register in reg)
                {
                    if (reg.Any(x => x.Address > register.Address && x.Address < register.Address + register.RegisterCount))
                    {
                        throw new Exception($"{register.RegisterType}的地址{register.Address}占用{register.RegisterCount}个寄存器，所以下一个地址需从{register.Address + register.RegisterCount}开始");
                    }
                }
            }

        }
        public static void ValicateValue(IEnumerable<Register> registers)
        {
            var items = registers.Where(x => x.WriteType != RegisterWriteType.只读);
            foreach (var register in items)
            {
                if (register.RegisterType == RegisterType.线圈)
                {
                    switch (register.Value.ToLower())
                    {
                        case "1":
                        case "0":
                            break;
                        case "false":
                        case "off":
                        case "f":
                            register.Value = "0";
                            break;
                        case "true":
                        case "on":
                        case "t":
                            register.Value = "1";
                            break;
                        default:
                            throw new Exception($"地址为{register.Address}的值错误，该值必须是“0、1、ON、OFF、True、False、T、F”中的一种，不区分大小写。");
                    }
                }

                if (register.ValueType != RegisterValueType.UTF8)
                {
                    //if (register.OperationType == OperationType.Write)
                    //{

                    //}
                    if (!double.TryParse(register.Value, out _))
                    {
                        throw new Exception($"地址为{register.Address}的值错误，该值不符合数值类型");

                    }
                }
            }
        }
        /// <summary>
        /// 将DataTable转换成Register[]
        /// </summary>
        /// <param name="table"> DataTable表头：寄存器类型	寄存器地址	读写类型	数值类型	寄存器个数	对齐方式	值	单位	描述</param>
        /// <param name="ignoreValue">是否忽略Value列</param>
        /// <returns></returns>
        public static Register[] GetRegisters(DataTable table, bool ignoreValue = true)
        {
            int row = table.Rows.Count;
            var registers = new Register[row];
            for (int i = 0; i < row; i++)
            {
                registers[i] = new Register();
                registers[i].RegisterType = (RegisterType)Enum.Parse(typeof(RegisterType), table.Rows[i][0].ToString());
                if (table.Rows[i][1].ToString().ToLower().StartsWith("0x"))
                {
                    registers[i].Address = Convert.ToInt32(table.Rows[i][1].ToString(), 16);
                }
                else
                {
                    registers[i].Address = Convert.ToInt32(table.Rows[i][1].ToString(), 10);
                }
                registers[i].WriteType = (RegisterWriteType)Enum.Parse(typeof(RegisterWriteType), table.Rows[i][2].ToString());
                registers[i].ValueType = (RegisterValueType)Enum.Parse(typeof(RegisterValueType), table.Rows[i][3].ToString());
                if (registers[i].ValueType == RegisterValueType.UTF8)
                {
                    registers[i].RegisterCount = int.Parse(table.Rows[i][4].ToString());
                }
                registers[i].EndianType = (RegisterEndianType)Enum.Parse(typeof(RegisterEndianType), table.Rows[i][5].ToString());
                if (!ignoreValue)
                {
                    registers[i].Value = table.Rows[i][6]?.ToString();
                }
                registers[i].Unit = table.Rows[i][7]?.ToString();
                registers[i].Description = table.Rows[i][8]?.ToString();

            }
            return registers;
        }

        public static DataTable ToDataTable(IEnumerable<Register> registers)
        {
            DataTable table = new DataTable();
            table.Columns.Add("寄存器类型");
            table.Columns.Add("寄存器地址");
            table.Columns.Add("读写类型");
            table.Columns.Add("数值类型");
            table.Columns.Add("寄存器个数");
            table.Columns.Add("对齐方式");
            table.Columns.Add("值");
            table.Columns.Add("单位");
            table.Columns.Add("描述");
            foreach (Register reg in registers)
            {
                table.Rows.Add(
                    reg.RegisterType.GetDescription(),
                    reg.Address,
                    reg.WriteType,
                    reg.ValueType,
                    reg.RegisterCount,
                    reg.EndianType,
                    reg.Value,
                    reg.Unit,
                    reg.Description);

            }
            return table;
        }
        #endregion
    }
}

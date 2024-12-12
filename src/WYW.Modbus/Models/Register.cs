using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace WYW.Modbus
{
    public class Register : ObservableObject
    {
        #region 构造函数
        public Register()
        {

        }
        public Register(int address)
        {
            this.address = address;
            this.addressChar = $"0x{address:X2}";
        }
        public Register(int address, int value)
        {
            this.address = address;
            this.addressChar = $"0x{address:X2}";
            Value = value.ToString();
        }
        public Register(int address, int value, RegisterType registerType)
        {
            this.address = address;
            this.addressChar = $"0x{address:X2}";
            Value = value.ToString();
            RegisterType = registerType;
        }
        #endregion

        #region 属性
        private bool isChecked = true;
        private RegisterType registerType = RegisterType.HoldingRegister;
        private RegisterValueType valueType = RegisterValueType.UInt16;
        private RegisterWriteType writeType = RegisterWriteType.RW;
        private RegisterEndianType endianType = RegisterEndianType.BigEndian;
        private int address;
        private string _value = "0";
        private string description;
        private string unit;
        private int registerCount = 1;
        private OperationType operationType= OperationType.Read;
        private string addressChar;
        /// <summary>
        /// 地址
        /// </summary>
        public int Address
        {
            get => address;
            private set => SetProperty(ref address, value);
        }

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
            set
            {
                SetProperty(ref _value, value);
                if (valueType == RegisterValueType.UTF8)
                {
                    RegisterCount = (Encoding.UTF8.GetBytes(Value ?? "").Length + 1) / 2;
                }
            }
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
                    case RegisterWriteType.R:
                        OperationType = OperationType.Read;
                        break;
                    case RegisterWriteType.W:
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
                    case RegisterType.HoldingRegister:
                        WriteType = RegisterWriteType.RW;
                        break;
                    case RegisterType.InputRegister:
                        WriteType = RegisterWriteType.R;
                        break;
                    case RegisterType.DiscreteInputRegister:
                        WriteType = RegisterWriteType.R;
                        break;
                    case RegisterType.Coil:
                        WriteType = RegisterWriteType.RW;
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
            if(string.IsNullOrEmpty(Value))
            {
                return new byte[RegisterCount*2];
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
                        if (EndianType == RegisterEndianType.LittleEndian)
                        {
                            return bytes.Reverse().ToArray();
                        }
                        else
                        {
                            return bytes;
                        }
                }
            }
            catch(Exception ex)
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
                    if (EndianType == RegisterEndianType.LittleEndian)
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
        #region 寄存器数组操作
        /// <summary>
        /// 读取多个寄存器集合
        /// </summary>
        /// <param name="slaveID"></param>
        /// <param name="registers"></param>
        /// <param name="responseTimeout"></param>
        /// <param name="tokenSource"></param>
        /// <param name="isIgnoreFailed"></param>
        /// <exception cref="Exception"></exception>
        public static void ReadRegisters(ModbusMaster device, int slaveID, IEnumerable<Register> registers, int responseTimeout = 300, CancellationTokenSource tokenSource = null, bool isIgnoreFailed = false)
        {
            if (!registers.Any(x => x.IsChecked == true))
            {
                throw new Exception("请先勾选需要读取的寄存器");
            }
            Register.ValicateAddress(registers.Where(x => x.IsChecked));
            // 获取需要读取的寄存器起始地址和数量
            List<List<Register>> list = new List<List<Register>>();
            ExecutionResult result;
            #region 读取保持寄存器
            var selectedRegisters = registers.Where(x => x.IsChecked && x.RegisterType == RegisterType.HoldingRegister && x.WriteType != RegisterWriteType.W).OrderBy(x => x.Address).ToArray();
            int startIndex = 0;
            list.Clear();
            byte[] fullBytes;
            if (selectedRegisters.Length > 0)
            {
                for (int i = 0; i < selectedRegisters.Length; i++)
                {
                    if (i == selectedRegisters.Length - 1 ||
                        selectedRegisters[i].Address + selectedRegisters[i].RegisterCount != selectedRegisters[i + 1].Address)
                    {
                        List<Register> temp = new List<Register>();
                        for (int j = startIndex; j <= i; j++)
                        {
                            temp.Add(selectedRegisters[j]);
                        }
                        list.Add(temp);
                        startIndex = i + 1;
                    }
                }

                foreach (var item in list)
                {
                    if (tokenSource?.Token.IsCancellationRequested == true)
                    {
                        return;
                    }
                    int startAddress = (UInt16)item.Min(x => x.Address);
                    int count = item.Sum(x => x.RegisterCount);
                    result = device.ReadHoldingRegisters(slaveID, (UInt16)startAddress, (UInt16)count, out fullBytes, responseTimeout: responseTimeout);
                    if (result.IsSuccess)
                    {
                        int index = 0;
                        for (int i = 0; i < item.Count; i++)
                        {
                            item[i].Value = item[i].GetValue(fullBytes, index);
                            index += (item[i].RegisterCount * 2);
                        }
                    }
                    else
                    {
                        if (!isIgnoreFailed)
                        {
                            throw new Exception($"读取起始地址从{startAddress}到{startAddress + count - 1}的保持寄存器失败，原因：{result.ErrorMessage}");

                        }
                    }
                }
            }

            #endregion

            #region 读取InputRegister
            selectedRegisters = registers.Where(x => x.IsChecked && x.RegisterType == RegisterType.InputRegister).OrderBy(x => x.Address).ToArray();
            startIndex = 0;
            list.Clear();
            if (selectedRegisters.Length > 0)
            {
                for (int i = 0; i < selectedRegisters.Length; i++)
                {
                    if (i == selectedRegisters.Length - 1 ||
                        selectedRegisters[i].Address + selectedRegisters[i].RegisterCount != selectedRegisters[i + 1].Address)
                    {
                        List<Register> temp = new List<Register>();
                        for (int j = startIndex; j <= i; j++)
                        {
                            temp.Add(selectedRegisters[j]);
                        }
                        list.Add(temp);
                        startIndex = i + 1;
                    }
                }
                foreach (var item in list)
                {
                    if (tokenSource?.Token.IsCancellationRequested == true)
                    {
                        return;
                    }
                    int startAddress = (UInt16)item.Min(x => x.Address);
                    int count = item.Sum(x => x.RegisterCount);
                    result = device.ReadInputRegisters(slaveID, (UInt16)startAddress, (UInt16)count, out fullBytes, responseTimeout: responseTimeout);
                    if (result.IsSuccess)
                    {
                        int index = 0;
                        for (int i = 0; i < item.Count; i++)
                        {
                            item[i].Value = item[i].GetValue(fullBytes, index);
                            index += (item[i].RegisterCount * 2);
                        }
                    }
                    else
                    {
                        if (!isIgnoreFailed)
                        {
                            throw new Exception($"读取起始地址从{startAddress}到{startAddress + count - 1}InputRegister失败，原因：{result.ErrorMessage}");

                        }
                    }
                }
            }

            #endregion

            #region 读取离散输入量
            selectedRegisters = registers.Where(x => x.IsChecked && x.RegisterType == RegisterType.DiscreteInputRegister).OrderBy(x => x.Address).ToArray();
            startIndex = 0;
            list.Clear();
            if (selectedRegisters.Length > 0)
            {
                for (int i = 0; i < selectedRegisters.Length; i++)
                {
                    if (i == selectedRegisters.Length - 1 ||
                        selectedRegisters[i].Address + selectedRegisters[i].RegisterCount != selectedRegisters[i + 1].Address)
                    {
                        List<Register> temp = new List<Register>();
                        for (int j = startIndex; j <= i; j++)
                        {
                            temp.Add(selectedRegisters[j]);
                        }
                        list.Add(temp);
                        startIndex = i + 1;
                    }
                }
                foreach (var item in list)
                {
                    if (tokenSource?.Token.IsCancellationRequested == true)
                    {
                        return;
                    }
                    int startAddress = (UInt16)item.Min(x => x.Address);
                    int count = item.Sum(x => x.RegisterCount);
                    result = device.ReadDiscreteInputRegisters(slaveID, (UInt16)startAddress, (UInt16)count, out bool[] values, responseTimeout: responseTimeout);
                    if (result.IsSuccess)
                    {
                        for (int i = 0; i < item.Count; i++)
                        {
                            item[i].Value = values[i] ? "1" : "0";
                        }
                    }
                    else
                    {
                        if (!isIgnoreFailed)
                        {
                            throw new Exception($"读取起始地址从{startAddress}到{startAddress + count - 1}离散量输入失败，原因：{result.ErrorMessage}");

                        }
                    }
                }
            }

            #endregion

            #region 读取线圈
            selectedRegisters = registers.Where(x => x.IsChecked && x.RegisterType == RegisterType.Coil && x.WriteType != RegisterWriteType.W).OrderBy(x => x.Address).ToArray();
            startIndex = 0;
            list.Clear();
            if (selectedRegisters.Length > 0)
            {
                for (int i = 0; i < selectedRegisters.Length; i++)
                {
                    if (i == selectedRegisters.Length - 1 ||
                        selectedRegisters[i].Address + selectedRegisters[i].RegisterCount != selectedRegisters[i + 1].Address)
                    {
                        List<Register> temp = new List<Register>();
                        for (int j = startIndex; j <= i; j++)
                        {
                            temp.Add(selectedRegisters[j]);
                        }
                        list.Add(temp);
                        startIndex = i + 1;
                    }
                }
                foreach (var item in list)
                {
                    if (tokenSource?.Token.IsCancellationRequested == true)
                    {
                        return;
                    }
                    int startAddress = (UInt16)item.Min(x => x.Address);
                    int count = item.Sum(x => x.RegisterCount);
                    result = device.ReadCoils(slaveID, (UInt16)startAddress, (UInt16)count, out bool[] values, responseTimeout: responseTimeout);
                    if (result.IsSuccess)
                    {
                        for (int i = 0; i < item.Count; i++)
                        {
                            item[i].Value = values[i] ? "1" : "0";
                        }
                    }
                    else
                    {
                        if (!isIgnoreFailed)
                        {
                            throw new Exception($"读取起始地址从{startAddress}到{startAddress + count - 1}线圈失败，原因：{result.ErrorMessage}");

                        }
                    }
                }
            }

            #endregion
        }
        /// <summary>
        /// 写多个寄存器集合
        /// </summary>
        /// <param name="slaveID"></param>
        /// <param name="registers"></param>
        /// <param name="responseTimeout">每条指令的超时时间，单位ms</param>
        /// <param name="isSupportMultiWriteCommand">是否支持写多个寄存器或线圈指令，例如0x10、0x0F</param>
        /// <exception cref="Exception"></exception>
        public static void WriteRegisters(ModbusMaster device, int slaveID, IEnumerable<Register> registers, int responseTimeout = 300, bool isSupportMultiWriteCommand = true, CancellationTokenSource tokenSource = null, bool isIgnoreFailed = false)
        {
            if (!registers.Any(x => x.IsChecked == true))
            {
                throw new Exception("请先勾选需要读取的寄存器");
            }
            Register.ValicateAddress(registers.Where(x => x.IsChecked));
            Register.ValicateValue(registers.Where(x => x.IsChecked));
            ExecutionResult result;
            // 一条一条写
            if (!isSupportMultiWriteCommand)
            {
                var items = registers.Where(x => x.IsChecked && x.WriteType != RegisterWriteType.R);
                foreach (var item in items)
                {
                    if (tokenSource?.Token.IsCancellationRequested == true)
                    {
                        return;
                    }
                    switch (item.RegisterType)
                    {
                        case RegisterType.HoldingRegister:
                            var values = BitConverterHelper.ToUInt16Array(item.GetBytes(), endianType: (EndianType)item.EndianType);
                            foreach (var value in values)
                            {
                                result = device.WriteHoldingRegister(slaveID, (UInt16)item.Address, value, responseTimeout: responseTimeout);
                                if (!result.IsSuccess && !isIgnoreFailed)
                                {
                                    throw new Exception($"写地址为{item.Address}保持寄存器失败，原因：{result.ErrorMessage}");
                                }
                            }
                            break;
                        case RegisterType.Coil:
                            result = device.WriteCoil(slaveID, (UInt16)item.Address, item.Value == "1", responseTimeout: responseTimeout);
                            if (!result.IsSuccess && !isIgnoreFailed)
                            {
                                throw new Exception($"写地址为{item.Address}线圈失败，原因：{result.ErrorMessage}");
                            }
                            break;
                    }
                }
            }
            // 使用多指令批量写
            else
            {
                // 获取需要写的寄存器
                List<List<Register>> list = new List<List<Register>>();

                #region 写HoldingRegister
                var selectedRegisters = registers.Where(x => x.IsChecked && x.RegisterType == RegisterType.HoldingRegister && x.WriteType != RegisterWriteType.R).OrderBy(x => x.Address).ToArray();
                int startIndex = 0;
                list.Clear();
                if (selectedRegisters.Length > 0)
                {
                    for (int i = 0; i < selectedRegisters.Length; i++)
                    {
                        if (i == selectedRegisters.Length - 1 ||
                            selectedRegisters[i].Address + selectedRegisters[i].RegisterCount != selectedRegisters[i + 1].Address)
                        {
                            List<Register> temp = new List<Register>();
                            for (int j = startIndex; j <= i; j++)
                            {
                                temp.Add(selectedRegisters[j]);
                            }
                            list.Add(temp);
                            startIndex = i + 1;
                        }
                    }

                    foreach (var item in list)
                    {
                        if (tokenSource?.Token.IsCancellationRequested == true)
                        {
                            return;
                        }
                        int startAddress = (UInt16)item.Min(x => x.Address);
                        int count = item.Sum(x => x.RegisterCount);
                        List<byte> sendArray = new List<byte>();
                        foreach (var reg in item)
                        {
                            sendArray.AddRange(reg.GetBytes());
                        }
                        result = device.WriteHoldingRegisters(slaveID, (UInt16)startAddress, sendArray.ToArray(), responseTimeout: responseTimeout);
                        if (!result.IsSuccess && !isIgnoreFailed)
                        {
                            throw new Exception($"写起始地址从{startAddress}到{startAddress + count - 1}保持寄存器失败，原因：{result.ErrorMessage}");
                        }
                    }
                }


                #endregion

                #region 写线圈
                selectedRegisters = registers.Where(x => x.IsChecked && x.RegisterType == RegisterType.Coil && x.WriteType != RegisterWriteType.R).OrderBy(x => x.Address).ToArray();
                startIndex = 0;
                list.Clear();
                if (selectedRegisters.Length > 0)
                {
                    for (int i = 0; i < selectedRegisters.Length; i++)
                    {
                        if (i == selectedRegisters.Length - 1 ||
                            selectedRegisters[i].Address + selectedRegisters[i].RegisterCount != selectedRegisters[i + 1].Address)
                        {
                            List<Register> temp = new List<Register>();
                            for (int j = startIndex; j <= i; j++)
                            {
                                temp.Add(selectedRegisters[j]);
                            }
                            list.Add(temp);
                            startIndex = i + 1;
                        }
                    }

                    foreach (var item in list)
                    {
                        if (tokenSource?.Token.IsCancellationRequested == true)
                        {
                            return;
                        }
                        int startAddress = (UInt16)item.Min(x => x.Address);
                        int count = item.Sum(x => x.RegisterCount);
                        List<bool> sendArray = new List<bool>();
                        foreach (var reg in item)
                        {
                            sendArray.Add((reg.Value == "1"));
                        }
                        result = device.WriteCoils(slaveID, (UInt16)startAddress, sendArray.ToArray(), responseTimeout: responseTimeout);
                        if (!result.IsSuccess && !isIgnoreFailed)
                        {
                            throw new Exception($"写起始地址从{startAddress}到{startAddress + count - 1}线圈失败，原因：{result.ErrorMessage}");
                        }
                    }
                }

                #endregion
            }

        }

        public static ExecutionResult ReadOrWriteRegister(ModbusMaster device, int slaveID, Register register, int responseTimeout = 300)
        {
            byte[] bytes;
            bool[] boolValues;
            ExecutionResult result = ExecutionResult.Failed(null, null);
            switch (register.RegisterType)
            {
                case RegisterType.HoldingRegister:
                    if (register.OperationType == OperationType.Read)
                    {
                        result = device.ReadHoldingRegisters(slaveID, (UInt16)register.Address, (UInt16)register.RegisterCount, out bytes, responseTimeout: responseTimeout);
                        if (result.IsSuccess)
                        {
                            register.Value = register.GetValue(bytes, 0);
                        }
                    }
                    else
                    {
                        result = device.WriteHoldingRegisters(slaveID, (UInt16)register.Address, register.GetBytes(), responseTimeout: responseTimeout);
                    }
                    break;
                case RegisterType.InputRegister:
                    result = device.ReadInputRegisters(slaveID, (UInt16)register.Address, (UInt16)register.RegisterCount, out bytes, responseTimeout: responseTimeout);
                    if (result.IsSuccess)
                    {
                        register.Value = register.GetValue(bytes, 0);
                    }
                    break;
                case RegisterType.Coil:
                    if (register.OperationType == OperationType.Read)
                    {
                        result = device.ReadCoils(slaveID, (UInt16)register.Address, 1, out boolValues, responseTimeout: responseTimeout);
                        if (result.IsSuccess)
                        {
                            register.Value = boolValues[0] ? "1" : "0";
                        }
                    }
                    else
                    {
                        result = device.WriteCoil(slaveID, (UInt16)register.Address, register.Value == "1", responseTimeout: responseTimeout);
                    }
                    break;
                case RegisterType.DiscreteInputRegister:
                    result = device.ReadDiscreteInputRegisters(slaveID, (UInt16)register.Address, 1, out boolValues, responseTimeout: responseTimeout);
                    if (result.IsSuccess)
                    {
                        register.Value = boolValues[0] ? "1" : "0";
                    }
                    break;
            }
            return result;
        }
        #endregion
        /// <summary>
        /// 验证寄存器数组地址与寄存器个数是否满足要求
        /// </summary>
        /// <param name="registers"></param>
        /// <exception cref="Exception"></exception>
        private static void ValicateAddress(IEnumerable<Register> registers)
        {
            var registersArray = registers.GroupBy(x=>x.RegisterType); // 按寄存器类型分组
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
        private static void ValicateValue(IEnumerable<Register> registers)
        {
            var items = registers.Where(x => x.WriteType != RegisterWriteType.R);
            foreach (var register in items)
            {
                if(register.RegisterType== RegisterType.Coil)
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
         
                if(register.ValueType!= RegisterValueType.UTF8)
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
                registers[i].RegisterType = (RegisterType)table.Rows[i][0].ToString().GetEnumByDescription<RegisterType>();
                if (table.Rows[i][1].ToString().ToLower().StartsWith("0x"))
                {
                    registers[i].Address = Convert.ToInt32(table.Rows[i][1].ToString(), 16);
                }
                else
                {
                    registers[i].Address = Convert.ToInt32(table.Rows[i][1].ToString(), 10);
                }
                registers[i].WriteType = (RegisterWriteType)table.Rows[i][2].ToString().GetEnumByDescription<RegisterWriteType>();
                registers[i].ValueType = (RegisterValueType)Enum.Parse(typeof(RegisterValueType), table.Rows[i][3].ToString());
                if (registers[i].ValueType == RegisterValueType.UTF8)
                {
                    registers[i].RegisterCount = int.Parse(table.Rows[i][4].ToString());
                }
                registers[i].EndianType = (RegisterEndianType)table.Rows[i][5].ToString().GetEnumByDescription<RegisterEndianType>();
                if (!ignoreValue)
                {
                    registers[i].Value =  table.Rows[i][6]?.ToString();
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
                    reg.WriteType.GetDescription(),
                    reg.ValueType,
                    reg.RegisterCount,
                    reg.EndianType.GetDescription(),
                    reg.Value,
                    reg.Unit,
                    reg.Description);

            }
            return table;
        }
        #endregion
    }
}

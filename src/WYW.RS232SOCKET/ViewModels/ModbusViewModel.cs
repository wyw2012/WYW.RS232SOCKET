using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WYW.RS232SOCKET.Common;
using WYW.RS232SOCKET.Models;
using MessageBoxImage = WYW.UI.Controls.MessageBoxImage;
using MessageBox = WYW.UI.Controls.MessageBoxWindow;
using MessageControl = WYW.UI.Controls.MessageBoxControl;
using Microsoft.Win32;
using GemBox.Spreadsheet;
using System.Data;
using System.IO;
using WYW.Communication.ApplicationlLayer;
using WYW.Communication.Protocol;
using WYW.Communication;

namespace WYW.RS232SOCKET.ViewModels
{
    partial class MainWindowViewModel
    {

        #region  属性

        private int startAddress;
        private int endAddress = 10;
        /// <summary>
        /// 起始地址
        /// </summary>
        public int StartAddress
        {
            get => startAddress;
            set => SetProperty(ref startAddress, value);
        }

        /// <summary>
        /// 终止地址
        /// </summary>
        public int EndAddress
        {
            get => endAddress;
            set => SetProperty(ref endAddress, value);
        }
        /// <summary>
        /// 保持寄存器集合
        /// </summary>
        public ObservableCollectionEx<Register> Registers { get; } = new ObservableCollectionEx<Register>();

        #endregion

        #region 命令

        public RelayCommand CreateRegisterCommand { get; private set; }
        public RelayCommand LoadTemplateCommand { get; private set; }
        public RelayCommand SaveTemplateCommand { get; private set; }
        public RelayCommand ReadRegisterCommand { get; private set; }
        public RelayCommand WriteRegisterCommand { get; private set; }

        private void CreateRegister()
        {
            if (endAddress < startAddress)
            {
                MessageBox.Warning("创建失败，终止地址不能小于起始地址。");
                return;
            }
            Registers.Clear();

            for (int i = startAddress; i <= endAddress; i++)
            {
                Registers.Add(new Register(i));
            }
        }

        private void LoadTemplate()
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Excel File (*.xlsx)|*.xlsx"; //设置文件类型 
            ofd.FilterIndex = 1; //设置默认文件类型显示顺序 
            ofd.RestoreDirectory = true; //保存对话框是否记忆上次打开的目录 
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    ImportFromExcel(ofd.FileName);
                    MessageControl.Success("加载成功");
                }
                catch (Exception ex)
                {
                    MessageBox.Error(ex.Message);
                    Registers.Clear();
                }
            }
        }

        private void SaveTemplate()
        {
            if(Registers.Count<=0)
            {
                MessageBox.Error("寄存器数量为0，请先创建寄存器。");
                return;
            }
            var sfd = new SaveFileDialog();
            sfd.Filter = "Excel File (*.xlsx)|*.xlsx"; //设置文件类型 
            sfd.FilterIndex = 1; //设置默认文件类型显示顺序 
            sfd.RestoreDirectory = true; //保存对话框是否记忆上次打开的目录 
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    ExportToExcel(sfd.FileName);
                    MessageControl.Success("保存成功");
                }
                catch (Exception ex)
                {
                    MessageBox.Error(ex.Message);

                }
            }
        }
        private void ReadRegister()
        {
            try
            {
                ValicateRegisters();
            }
            catch (Exception ex)
            {
                MessageBox.Warning($"读寄存器失败。{ex.Message}");
                return;
            }
            if (!Registers.Any(x => x.IsChecked == true))
            {
                MessageBox.Warning("请先勾选需要读取的寄存器");
                return;
            }
            Clear();
            ModbusMaster master = Device as ModbusMaster;

            // 获取需要读取的寄存器起始地址和数量
            List<List<Register>> list = new List<List<Register>>();
            var selectedRegisters = Registers.Where(x => x.IsChecked).OrderBy(x => x.Address).ToArray();
            int startIndex = 0;
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
            bool result = false;
            byte[] fullBytes;
            foreach (var item in list)
            {
                result = master.ReadHoldingRegisters(Config.Modbus.SlaveID, (UInt16)item.Min(x => x.Address), (UInt16)item.Sum(x => x.RegisterCount), out fullBytes,responseTimeout:Config.Modbus.ResponseTimeout);
                if (result)
                {
                    int index = 0;
                    for (int i = 0; i < item.Count; i++)
                    {
                        item[i].Value = item[i].GetValue(fullBytes,index);
                        index += (item[i].RegisterCount * 2);
                    }
                }
                else
                {
                    MessageBox.Error("发送无应答或者超时");
                }
            }
        }

        private void WriteRegister()
        {
            try
            {
                ValicateRegisters();
            }
            catch (Exception ex)
            {
                MessageBox.Warning($"写寄存器失败。{ex.Message}");
                return;
            }
            if (!Registers.Any(x => x.IsChecked == true && x.WriteType == RegisterWriteType.读写))
            {
                MessageBox.Warning("请先勾选需支持可写的寄存器");
                return;
            }
            Clear();
            ModbusMaster master = Device as ModbusMaster;
            // 获取需要写的寄存器
            List<List<Register>> list = new List<List<Register>>();
            var selectedRegisters = Registers.Where(x => x.IsChecked && x.WriteType == RegisterWriteType.读写).OrderBy(x => x.Address).ToArray();
            int startIndex = 0;
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
            bool result = false;
            foreach (var item in list)
            {
                List<byte> sendArray = new List<byte>();
                foreach (var reg in item)
                {
                    sendArray.AddRange(reg.GetBytes());
                }
                result = master.WriteHoldingRegisters(Config.Modbus.SlaveID, (UInt16)item.Min(x => x.Address), sendArray.ToArray(), responseTimeout: Config.Modbus.ResponseTimeout);
                if (!result)
                {
                    MessageBox.Error("发送无应答或者超时");
                }
            }
        }
        #endregion


        #region 私有方法
        private static readonly string LICENSE = "E02V-XUB1-52LA-994F";
        /// <summary>
        /// 导入寄存器模板
        /// </summary>
        /// <param name="fileName"></param>
        /// <exception cref="Exception"></exception>
        private void ImportFromExcel(string fileName)
        {
            
            SpreadsheetInfo.SetLicense(LICENSE);
            var ef = ExcelFile.Load(fileName);
            var ws = ef.Worksheets[0];
            Registers.Clear();
            for (int i = 1; i < ws.Rows.Count; i++)
            {
                try
                {
                    Register register = new Register();
                    register.Address = (int)ws.Cells[i, 0].Value;
                    register.ValueType = (RegisterValueType)Enum.Parse(typeof(RegisterValueType), ws.Cells[i, 1].Value.ToString());
                    register.WriteType = (RegisterWriteType)Enum.Parse(typeof(RegisterWriteType), ws.Cells[i, 2].Value.ToString());
                    register.Description = ws.Cells[i, 3].Value?.ToString();
                    Registers.Add(register);
                }
                catch 
                {
                    throw new Exception("模板错误，请加载正确的模板");
                }
             
            }
            ValicateRegisters();
        }
        /// <summary>
        /// 将寄存器模板导出到Excel
        /// </summary>
        /// <param name="fileName"></param>
        private void ExportToExcel(string fileName)
        {
            SpreadsheetInfo.SetLicense(LICENSE);
            var ef = ExcelFile.Load(new MemoryStream(Properties.Resources.RegisterTemplate));
            var ws = ef.Worksheets[0];

            for (var i = 0; i < Registers.Count; i++)
            {
                ws.Cells[i + 1, 0].Value = Registers[i].Address;
                ws.Cells[i + 1, 1].Value = Registers[i].ValueType.ToString();
                ws.Cells[i + 1, 2].Value = Registers[i].WriteType.ToString();
                ws.Cells[i + 1, 3].Value = Registers[i].Description;
            }
            ef.Save(fileName);
        }
        /// <summary>
        /// 验证寄存器的属性是否正确
        /// </summary>
        /// <exception cref="Exception"></exception>
        private void ValicateRegisters()
        {
            for (int i = 0; i < Registers.Count; i++)
            {
                if (Registers[i].ValueType == RegisterValueType.UInt32 ||
                    Registers[i].ValueType == RegisterValueType.Int32 ||
                    Registers[i].ValueType == RegisterValueType.Float)
                {
                    if (Registers.Any(x => x.Address > Registers[i].Address &&
                    x.Address < Registers[i].Address + 2))
                    {
                        throw new Exception($"由于地址{Registers[i].Address}是{Registers[i].ValueType}类型，所以下一个地址需从{Registers[i].Address + 2}开始");
                    }
                }
                else if (Registers[i].ValueType == RegisterValueType.Double ||
                    Registers[i].ValueType == RegisterValueType.Int64 ||
                    Registers[i].ValueType == RegisterValueType.UInt64)
                {
                    if (Registers.Any(x => x.Address > Registers[i].Address &&
                    x.Address < Registers[i].Address + 4))
                    {
                        throw new Exception($"由于地址{Registers[i].Address}是{Registers[i].ValueType}类型，所以下一个地址需从{Registers[i].Address + 4}开始");
                    }
                }
            }
        }

        /// <summary>
        /// Slave处理接收
        /// </summary>
        private void ProcessSlaveRecived(ProtocolBase obj)
        {
            ModbusCommand cmd = ModbusCommand.ReadMoreInputResiters;
            if (obj is ModbusTCP tcp)
            {
                if (Config.Modbus.SlaveID != tcp.SlaveID)
                {
                    return;
                }
                Config.Modbus.TransactionID = tcp.TransactionID;
                cmd = tcp.Command;
            }
            else if (obj is ModbusRTU rtu)
            {
                cmd = rtu.Command;
            }
            #region 获取发送的内容
            List<byte> content = new List<byte>(); // 待发送的内容
            Register register; // 临时变量
            int value;
            switch (cmd)
            {
                case ModbusCommand.ReadMoreHoldingRegisters:
                    var startIndex = BigEndianBitConverter.ToUInt16(obj.Content, 0);
                    var count = BigEndianBitConverter.ToUInt16(obj.Content, 2);
                    content.Add(((byte)(count * 2)));
                    for (int i = 0; i < count; i++)
                    {
                        register = Registers.SingleOrDefault(x => x.Address == startIndex + i);
                        if (register != null)
                        {
                            content.AddRange(register.GetBytes());
                            i = i - 1 + register.RegisterCount;
                        }
                        else
                        {
                            content.AddRange(BigEndianBitConverter.GetBytes((UInt16)0));
                        }
                       
                    }
                    break;
                case ModbusCommand.WriteOneHoldingRegister:
                    var registerAddress = BigEndianBitConverter.ToUInt16(obj.Content, 0);
                    var registerValue = BigEndianBitConverter.ToUInt16(obj.Content, 2);
                    content.AddRange(BigEndianBitConverter.GetBytes((UInt16)registerAddress));
                    content.AddRange(BigEndianBitConverter.GetBytes((UInt16)(registerValue)));
                    register = Registers.SingleOrDefault(x => x.Address == registerAddress);
                    value = BigEndianBitConverter.ToUInt16(obj.Content, 2);
                    if (register != null)
                    {
                        register.Value = value.ToString();
                    }
                    else
                    {
                        Registers.Add(new Register(registerAddress, value));
                    }
                    break;
                case ModbusCommand.WriteMoreHoldingRegisters:
                    startIndex = BigEndianBitConverter.ToUInt16(obj.Content, 0);
                    count = BigEndianBitConverter.ToUInt16(obj.Content, 2);
                    content.AddRange(BigEndianBitConverter.GetBytes((UInt16)startIndex));
                    content.AddRange(BigEndianBitConverter.GetBytes((UInt16)(count)));
                    for (int i = 0; i < count; i++)
                    {
                        // TODO
                        register = Registers.SingleOrDefault(x => x.Address == startIndex + i);
                        value = BigEndianBitConverter.ToUInt16(obj.Content, i * 2 + 5);
                        if (register != null)
                        {
                            register.Value = value.ToString();
                        }
                        else
                        {
                            Registers.Add(new Register(startIndex + i, value));
                        }
                    }
                    break;
                case ModbusCommand.ReadWriteHoldingRegisters:
                    // 处理读取
                    startIndex = BigEndianBitConverter.ToUInt16(obj.Content, 0);
                    count = BigEndianBitConverter.ToUInt16(obj.Content, 2);
                    content.Add(((byte)(count * 2)));
                    for (int i = 0; i < count; i++)
                    {
                        register = Registers.SingleOrDefault(x => x.Address == startIndex + i);
                        if (register != null)
                        {
                            content.AddRange(register.GetBytes());
                            i = i - 1 + register.RegisterCount;
                        }
                        else
                        {
                            content.AddRange(BigEndianBitConverter.GetBytes((UInt16)0));
                        }
                    }
                    // 处理写入
                    startIndex = BigEndianBitConverter.ToUInt16(obj.Content, 4);
                    count = BigEndianBitConverter.ToUInt16(obj.Content, 6);
                    for (int i = 0; i < count; i++)
                    {
                        register = Registers.SingleOrDefault(x => x.Address == startIndex + i);
                        value = BigEndianBitConverter.ToUInt16(obj.Content, i * 2 + 9);
                        if (register != null)
                        {
                            register.Value = value.ToString();
                        }
                        else
                        {
                            Registers.Add(new Register(startIndex + i, value));
                        }
                    }
                    break;

            }
            #endregion

            ProtocolBase response = null;
            if (obj is ModbusRTU)
            {
                response = new ModbusRTU((byte)Config.Modbus.SlaveID, cmd, content.ToArray());
            }
            else if (obj is ModbusTCP)
            {
                response = new ModbusTCP((byte)Config.Modbus.SlaveID, cmd, content.ToArray(), Config.Modbus.TransactionID);
            }
            Device.SendBytes(response.FullBytes);
        }

        #endregion



    }
}

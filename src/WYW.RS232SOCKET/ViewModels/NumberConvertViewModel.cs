using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using WYW.Communication;
using WYW.RS232SOCKET.Models;
using MessageBox = WYW.UI.Controls.MessageBoxWindow;
using MessageControl = WYW.UI.Controls.MessageBoxControl;

namespace WYW.RS232SOCKET.ViewModels
{
    internal class NumberConvertViewModel : ViewModelBase
    {
        #region 属性
        public DeviceController Controller { get; } = Ioc.Controller;
        private int selectedIndex;

        /// <summary>
        /// 
        /// </summary>
        public int SelectedIndex { get => selectedIndex; set => SetProperty(ref selectedIndex, value); }

        private ObservableCollection<NumberConvertModel> items = new ObservableCollection<NumberConvertModel>();

        /// <summary>
        /// 
        /// </summary>
        public ObservableCollection<NumberConvertModel> Items { get => items; set => SetProperty(ref items, value); }

        private string hexText = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        public string HexText { get => hexText; set => SetProperty(ref hexText, value); }

        #endregion


        #region 命令

        protected override void BindingCommand()
        {
            AddCommand = new RelayCommand(Add);
            DeleteCommand = new RelayCommand(Delete);
            ConvertToHexCommand = new RelayCommand(ConvertToHex);
            ConvertToListCommand = new RelayCommand(ConvertToList);
        }
        public RelayCommand AddCommand { get; private set; }
        public RelayCommand DeleteCommand { get; private set; }
        public RelayCommand ConvertToHexCommand { get; private set; }
        public RelayCommand ConvertToListCommand { get; private set; }
        private void Add()
        {
            MessageControl.Clear();
            var parameter = new NumberConvertModel() { ID = Items.Count + 1 };
            Items.Add(parameter);
        }

        private void Delete()
        {
            MessageControl.Clear();
            if (SelectedIndex == -1)
            {
                MessageBox.Warning("请先选择一条数据");
                return;
            }
            if (MessageBox.Question($"是否删除编号为{Items[SelectedIndex].ID}的数据？") == MessageBoxResult.Yes)
            {
                Items.RemoveAt(SelectedIndex);
            }
            var orderItems = Items.OrderBy(x => x.ID);
            int index = 1;
            foreach (var item in orderItems)
            {
                item.ID = index++;
            }

        }
        private void ConvertToHex()
        {
            if (!Items.Any())
            {
                MessageBox.Error("左侧队列为空");
            }
            else
            {
                List<byte> list = new List<byte>();
                var orderItems = Items.OrderBy(x => x.ID);
                foreach (var item in orderItems)
                {
                    try
                    {
                        byte[] temp = new byte[0];
                        EndianType endianType = item.EndianType == RegisterEndianType.大端模式 ? EndianType.BigEndian : EndianType.LittleEndian;
                        switch (item.ValueType)
                        {
                            case RegisterValueType.UInt64:
                                temp = BitConverterHelper.GetBytes(UInt64.Parse(item.Value), endianType);
                                break;
                            case RegisterValueType.UInt32:
                                temp = BitConverterHelper.GetBytes(UInt32.Parse(item.Value), endianType);
                                break;
                            case RegisterValueType.UInt16:
                                temp = BitConverterHelper.GetBytes(UInt16.Parse(item.Value), endianType);
                                break;
                            case RegisterValueType.Int64:
                                temp = BitConverterHelper.GetBytes(Int64.Parse(item.Value), endianType);
                                break;
                            case RegisterValueType.Int32:
                                temp = BitConverterHelper.GetBytes(Int32.Parse(item.Value), endianType);
                                break;
                            case RegisterValueType.Int16:
                                temp = BitConverterHelper.GetBytes(Int16.Parse(item.Value), endianType);
                                break;
                            case RegisterValueType.Double:
                                temp = BitConverterHelper.GetBytes(double.Parse(item.Value), endianType);
                                break;
                            case RegisterValueType.Float:
                                temp = BitConverterHelper.GetBytes(float.Parse(item.Value), endianType);
                                break;
                        }
                        list.AddRange(temp);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Error($"序号为{item.ID}值转换失败。{ex.Message}");
                        return;
                    }

                }
                HexText = list.ToHexString();
            }
        }
        private void ConvertToList()
        {
            if (string.IsNullOrEmpty(HexText))
            {
                MessageBox.Error("待转换的十六进制字符串不能为空");
                return;
            }

            try
            {
                var fullBytes = HexText.ToHexArray();
                int length = GetLength();
                if (fullBytes.Length != length)
                {
                    MessageBox.Error($"左侧队列所需字节长度与右侧字符不匹配，左侧所需{length}字节，右侧有{fullBytes.Length}字节");
                    return;
                }
                int startIndex = 0;
                var orderItems = Items.OrderBy(x => x.ID);
                foreach (var item in orderItems)
                {
                    EndianType endianType = item.EndianType == RegisterEndianType.大端模式 ? EndianType.BigEndian : EndianType.LittleEndian;
                    switch (item.ValueType)
                    {
                        case RegisterValueType.UInt64:
                            item.Value = BitConverterHelper.ToUInt64(fullBytes, startIndex, endianType).ToString();
                            startIndex += 8;
                            break;
                        case RegisterValueType.UInt32:
                            item.Value = BitConverterHelper.ToUInt32(fullBytes, startIndex, endianType).ToString();
                            startIndex += 4;
                            break;
                        case RegisterValueType.UInt16:
                            item.Value = BitConverterHelper.ToUInt16(fullBytes, startIndex, endianType).ToString();
                            startIndex += 2;
                            break;
                        case RegisterValueType.Int64:
                            item.Value = BitConverterHelper.ToInt64(fullBytes, startIndex, endianType).ToString();
                            startIndex += 8;
                            break;
                        case RegisterValueType.Int32:
                            item.Value = BitConverterHelper.ToInt32(fullBytes, startIndex, endianType).ToString();
                            startIndex += 4;
                            break;
                        case RegisterValueType.Int16:
                            item.Value = BitConverterHelper.ToInt16(fullBytes, startIndex, endianType).ToString();
                            startIndex += 2;
                            break;
                        case RegisterValueType.Double:
                            item.Value = BitConverterHelper.ToDouble(fullBytes, startIndex, endianType).ToString();
                            startIndex += 8;
                            break;
                        case RegisterValueType.Float:
                            item.Value = BitConverterHelper.ToSingle(fullBytes, startIndex, endianType).ToString();
                            startIndex += 4;
                            break;
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Error($"转换失败。{ex.Message}");
            }
        }
        /// <summary>
        /// 获取Items总字节个数
        /// </summary>
        /// <returns></returns>
        private int GetLength()
        {
            int length = 0;
            foreach (var item in Items)
            {
                switch (item.ValueType)
                {
                    case RegisterValueType.UInt64:
                    case RegisterValueType.Int64:
                    case RegisterValueType.Double:
                        length += 8;
                        break;
                    case RegisterValueType.UInt32:
                    case RegisterValueType.Int32:
                    case RegisterValueType.Float:
                        length += 4;
                        break;
                    case RegisterValueType.UInt16:
                    case RegisterValueType.Int16:
                        length += 2;
                        break;
                }
            }
            return length;
        }
        #endregion
    }
}

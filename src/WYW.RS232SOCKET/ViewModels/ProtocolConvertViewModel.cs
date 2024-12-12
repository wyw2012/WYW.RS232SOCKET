using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WYW.Communication;
using MessageBox = WYW.UI.Controls.MessageBoxWindow;
using MessageControl = WYW.UI.Controls.MessageBoxControl;

namespace WYW.RS232SOCKET.ViewModels
{
    internal class ProtocolConvertViewModel:ViewModelBase
    {
        private EncodeType stringEncode= EncodeType.UTF8;

        /// <summary>
        /// 
        /// </summary>
        public EncodeType StringEncode { get => stringEncode; set => SetProperty(ref stringEncode, value); }

        private int suffixIndex;

        /// <summary>
        /// 后缀索引。0 无；1 回车；2 换行； 3 回车换行；4 累加和
        /// </summary>
        public int SuffixIndex { get => suffixIndex; set => SetProperty(ref suffixIndex, value); }

        private string stringText;

        /// <summary>
        /// 
        /// </summary>
        public string StringText { get => stringText; set => SetProperty(ref stringText, value); }

        private string hexText;

        /// <summary>
        /// 
        /// </summary>
        public string HexText { get => hexText; set => SetProperty(ref hexText, value); }


        #region 命令

        protected override void BindingCommand()
        {
            ConvertToHexCommand = new RelayCommand(ConvertToHex);
            ConvertToStringCommand = new RelayCommand(ConvertToString);
        }
        public RelayCommand ConvertToHexCommand { get; private set; }

        private void ConvertToHex()
        {
            if (string.IsNullOrEmpty(StringText))
            {
                MessageBox.Warning("左侧文本框请输入字符");
                return;
            }
            try
            {
               List<byte> bytes = new List<byte>();
                switch(StringEncode)
                {
                    case EncodeType.ASCII:
                        bytes.AddRange(Encoding.ASCII.GetBytes(StringText));
                        break;
                    case EncodeType.UTF8:
                        bytes.AddRange(Encoding.UTF8.GetBytes(StringText));
                        break;
                    case EncodeType.Unicode:
                        bytes.AddRange(Encoding.Unicode.GetBytes(StringText));
                        break;
                }
                switch(SuffixIndex)
                {
                    case 1:
                        bytes.Add(0x0D);
                        break;
                    case 2:
                        bytes.Add(0x0A);
                        break;
                    case 3:
                        bytes.Add(0x0D);
                        bytes.Add(0x0A);
                        break;
                    case 4:
                        var sum=bytes.Sum(x => (long)x);
                        bytes.Add((byte)sum);
                        break;

                }
                HexText=bytes.ToHexString();
            }
            catch(Exception ex) 
            {
                MessageBox.Error(ex.Message);
            }
        }


        public RelayCommand ConvertToStringCommand { get; private set; }

        private void ConvertToString()
        {
            if(string.IsNullOrEmpty(HexText))
            {
                MessageBox.Warning("右侧文本框请输入十六进制字符串");
                return;
            }
            try
            {
                var bytes=HexText.ToHexArray();
                byte[] newArray=new byte[0];
                switch (SuffixIndex)
                {
                    case 0:
                        newArray = bytes;
                        break;
                    case 1:
                        if(bytes.LastOrDefault()!=0x0D)
                        {
                            throw new Exception("十六进制字符串需要以0xOD结尾");
                        }
                        newArray=bytes.SubBytes(0, bytes.Length-1);
                        break;
                    case 2:
                        if (bytes.LastOrDefault() != 0x0A)
                        {
                            throw new Exception("十六进制字符串需要以0xOA结尾");
                        }
                        newArray = bytes.SubBytes(0, bytes.Length - 1);
                        break;
                    case 3:
                        if(bytes.Length<=2)
                        {
                            throw new Exception("十六进制字符串长度需要大于2");
                        }
                        if (bytes[bytes.Length-1]!=0x0A || bytes[bytes.Length-2]!=0x0D)
                        {
                            throw new Exception("十六进制字符串需要以0x0D 0xOA结尾");
                        }
                        newArray = bytes.SubBytes(0, bytes.Length - 2);
                        break;
                    case 4:
                       var sum= (byte)bytes.Take(bytes.Length - 1).Sum(x=>(long)x) ;
                        if(sum!=bytes.LastOrDefault())
                        {
                            throw new Exception($"校验位错误，校验位应该为0x{(sum.ToString("X2"))}");
                        }
                        newArray = bytes.SubBytes(0, bytes.Length - 1);
                        break;

                }

                switch (StringEncode)
                {
                    case EncodeType.ASCII:
                      StringText=  Encoding.ASCII.GetString(newArray);
                        break;
                    case EncodeType.UTF8:
                        StringText = Encoding.UTF8.GetString(newArray);
                        break;
                    case EncodeType.Unicode:
                        StringText = Encoding.Unicode.GetString(newArray);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Error(ex.Message);
            }
        }


        #endregion
    }
}

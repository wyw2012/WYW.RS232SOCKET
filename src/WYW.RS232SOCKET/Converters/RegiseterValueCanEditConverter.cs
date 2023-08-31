using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using WYW.Communication;
using WYW.Communication.Models;

namespace WYW.RS232SOCKET.Converters
{
    /// <summary>
    ///
    /// </summary>
    public class RegiseterValueCanEditConverter : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                StationType stationType = (StationType)Enum.Parse(typeof(StationType), values[0].ToString());
                RegisterWriteType writeType = (RegisterWriteType)Enum.Parse(typeof(RegisterWriteType), values[1].ToString());
                if (stationType == StationType.主站 && writeType == RegisterWriteType.只读)
                {
                    return false;
                }

            }
            catch
            {
            }
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
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
    public class RegiseterValueCanEditConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                RegisterWriteType writeType = (RegisterWriteType)Enum.Parse(typeof(RegisterWriteType), value.ToString());
                if (writeType == RegisterWriteType.只读)
                {
                    return false;
                }

            }
            catch
            {
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
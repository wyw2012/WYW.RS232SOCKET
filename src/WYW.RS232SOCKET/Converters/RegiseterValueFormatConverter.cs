using System;
using System.Diagnostics;
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
    public class RegiseterValueFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Register register)
            {
                try
                {
                    return $"{register.GetBytes().ToHexString()}";
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
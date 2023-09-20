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
    public class ToHexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value!=null)
            {
                try
                {
                    return $"0x{UInt32.Parse(value.ToString()):X2}";
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
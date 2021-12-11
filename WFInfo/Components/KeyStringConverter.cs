using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace WFInfo.Components
{
    /// <summary>
    /// Convert a Key into a human readable string
    /// </summary>
    [ValueConversion(typeof(Key), typeof(string))]
    public class KeyStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? null : KeyNameHelpers.GetKeyName((Key)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
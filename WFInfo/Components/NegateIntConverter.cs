using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WFInfo.Components
{
    /// <summary>
    /// Negate an integer value, -1 will be displayed as 1 and vice versa.
    /// </summary>
    [ValueConversion(typeof(int), typeof(string))]
    public class NegateIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? null : (-1 * (int)value).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int retValue;
            if (int.TryParse(value as string, out retValue))
            {
                return -1 * retValue;
            }

            return DependencyProperty.UnsetValue;
        }
    }
   

}
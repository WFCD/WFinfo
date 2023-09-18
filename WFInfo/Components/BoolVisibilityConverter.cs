using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WFInfo.Components
{
    /// <summary>
    /// Convert a bool into visibility, true being visible and false hidden
    /// </summary>
    [ValueConversion(typeof(Visibility), typeof(string))]
    public class BoolVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool) value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Globalization;
using System.Windows.Data;  // WPF - bắt buộc

namespace DeepBIM.Services   // <-- PHẢI đúng y chang chuỗi trong XAML
{
    public sealed class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            return string.Equals(value.ToString(), parameter.ToString(), StringComparison.Ordinal);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool b) || !b || parameter == null)
                return System.Windows.Data.Binding.DoNothing;
            return Enum.Parse(targetType, parameter.ToString());
        }
    }
}

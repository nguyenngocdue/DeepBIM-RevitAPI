using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DeepBIM.Converters
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string enumValue = value.ToString();
            string targetValue = parameter.ToString();

            return enumValue == targetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool && (bool)value)
            {
                return Enum.Parse(targetType, parameter.ToString());
            }
            return DependencyProperty.UnsetValue;
        }
    }
}
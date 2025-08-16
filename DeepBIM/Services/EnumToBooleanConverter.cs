using System;
using System.Globalization;
using System.Windows;        // DependencyProperty.UnsetValue
using System.Windows.Data;  // IValueConverter, Binding

namespace DeepBIM.Services
{
    /// <summary>
    /// RadioButton.IsChecked (bool) <-> Enum (SelectedOption)
    /// </summary>
    public sealed class EnumToBooleanConverter : IValueConverter
    {
        // enum -> bool
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            // Cho phép ConverterParameter là string ("OnlyElevation") hoặc chính giá trị enum
            if (parameter is string sParam)
                return value.ToString().Equals(sParam, StringComparison.Ordinal);

            return value.Equals(parameter);
        }

        // bool -> enum
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = value is bool b && b;
            //if (!isChecked || parameter == null)
            //    return Binding.DoNothing;

            // Nếu parameter là string thì parse, nếu đã là enum thì trả về luôn
            if (parameter is string sParam)
                return Enum.Parse(targetType, sParam);

            return parameter;
        }
    }
}

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Controls;

namespace DeepBIM.Converters
{
    public class AddOneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is int i ? i + 1 : 0;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => System.Windows.Data.Binding.DoNothing;
    }


    public class RowIndexConverter : IValueConverter
    {
       

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DataGridRow row && row.DataContext != null)
            {
                var dataGrid = FindParent<DataGrid>(row);
                if (dataGrid?.ItemsSource is System.Collections.IList items)
                {
                    int index = items.IndexOf(row.DataContext);
                    return (index + 1).ToString(); // STT bắt đầu từ 1
                }
            }
            return "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        // Tìm parent control
        private static T FindParent<T>(System.Windows.DependencyObject child) where T : System.Windows.DependencyObject
        {
            while (child != null && !(child is T))
            {
                child = System.Windows.Media.VisualTreeHelper.GetParent(child);
            }
            return child as T;
        }
    }
}
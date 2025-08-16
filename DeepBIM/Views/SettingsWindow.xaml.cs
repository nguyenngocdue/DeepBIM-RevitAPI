using DeepBIM.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DeepBIM.Views
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public double MinGap { get; set; }

        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadSettings(); // Đọc lại giá trị mỗi lần mở
        }

        // ✅ Thêm hàm riêng để dễ gọi lại nếu cần
        private void LoadSettings()
        {
            try
            {
                MinGap = Math.Round(SettingsManager.GetMinGap(), 3);
                TextBoxMinGap.Text = MinGap.ToString("F3");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load settings: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                MinGap = 0.1;
                TextBoxMinGap.Text = "0.100";
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(TextBoxMinGap.Text, out double value) && value >= 0)
            {
                SettingsManager.SaveMinGap(value);
                MessageBox.Show("Settings saved successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Cập nhật giá trị hiện tại sau khi lưu
                MinGap = value;
            }
            else
            {
                MessageBox.Show("Please enter a valid non-negative number (e.g. 0.1).",
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

       
    }
}

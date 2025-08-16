// DeepBIM.Views.CommandLineRevitWindow.xaml.cs
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Autodesk.Revit.UI;
using DeepBIM.Models;

namespace DeepBIM.Views
{
    public partial class CommandLineRevitWindow : Window
    {
        private readonly UIApplication _uiapp;
        private readonly ObservableCollection<CommandItem> _items;

        public CommandLineRevitWindow(UIApplication uiapp, ObservableCollection<CommandItem> items)
        {
            InitializeComponent();
            _uiapp = uiapp;
            _items = items;

            // phím tắt Enter/Escape toàn cửa sổ
            PreviewKeyDown += CommandLineRevitWindow_PreviewKeyDown;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Đổ dữ liệu cho ComboBoxAC
            comboBoxAC.ItemsSource = _items;
            comboBoxAC.DisplayMemberPath = "DisplayName";
            comboBoxAC.SelectedValuePath = "RevitCommandId";
            comboBoxAC.FilterMode = DeepBIM.Models.AutoCompleteFilterMode.Contains;
            comboBoxAC.ShowBorder = true;

            // chọn dòng đầu nếu muốn
            // comboBoxAC.CustomCbControl.SelectedIndex = 0;

            // Bật dropdown ngay khi mở (tuỳ thích)
            comboBoxAC.CustomCbControl.IsDropDownOpen = true;

            // Bắt sự kiện chọn item
            comboBoxAC.SelectionChanged += (s, ev) =>
            {
                if (comboBoxAC.SelectedItem is CommandItem ci)
                {
                    TryRun(ci);
                }
            };
        }

        private void CommandLineRevitWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Nếu có chọn, chạy; nếu chưa, lấy item đang được highlight trong dropdown (nếu có)
                if (comboBoxAC.SelectedItem is CommandItem ci)
                    TryRun(ci);
                else if (comboBoxAC.CustomCbControl?.Text is string txt && !string.IsNullOrWhiteSpace(txt))
                {
                    var first = _items.FirstOrDefault(x => x.DisplayName.StartsWith(txt, StringComparison.OrdinalIgnoreCase));
                    if (first != null) TryRun(first);
                }

                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxAC.SelectedItem is CommandItem ci)
                TryRun(ci);
        }

        private void TryRun(CommandItem ci)
        {
            try
            {
                if (ci?.RevitCommandId != null)
                {
                    // Đóng hộp thoại trước rồi PostCommand (tránh modal chặn)
                    Close();
                    _uiapp.PostCommand(ci.RevitCommandId);
                }
            }
            catch { /* có thể log nếu cần */ }
        }

        // Title bar drag + close nút X
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}

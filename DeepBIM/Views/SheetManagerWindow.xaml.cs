using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.DependencyInjection;
using DeepBIM.ViewModels;
using DeepBIM.Views.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for SheetManagerWindow.xaml
    /// </summary>
    public partial class SheetManagerWindow : Window
    {
        private readonly UIDocument _uiDoc;
        private readonly Document doc;
        private readonly SheetManagerViewModel _viewModel;

        public SheetManagerWindow(UIApplication app, Document doc)
        {
            InitializeComponent();
            _uiDoc = app.ActiveUIDocument;
            var selectedIds = _uiDoc.Selection.GetElementIds();
            _viewModel = new SheetManagerViewModel(doc, _uiDoc, selectedIds);
            DataContext = _viewModel;

            // Gán các thuộc tính từ BaseWindow
            //this.WindowTitle = "Quản lý Sheet";
            //this.FunctionText = "Lưu Thay Đổi";

        }
   
        private void MoveToLeft_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SheetManagerViewModel vm && DataGridRows.SelectedItems.Count > 0)
            {
                var selected = new List<SheetRow>();
                foreach (SheetRow item in DataGridRows.SelectedItems)
                {
                    selected.Add(item);
                }

                vm.ExecuteMoveToLeft(selected);
            }
        }

        private void MoveRowUp_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedRows(-1);
        }

        private void MoveRowDown_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedRows(1);
        }

        private void MoveSelectedRows(int direction)
        {
            if (DataGridRows.SelectedItems.Count == 0) return;

            var selectedRows = DataGridRows.SelectedItems.Cast<SheetRow>().ToList();
            var rows = (ObservableCollection<SheetRow>)DataGridRows.ItemsSource;

            // Duyệt ngược để không lỗi index khi xóa
            foreach (var row in selectedRows.OrderByDescending(r => rows.IndexOf(r)))
            {
                int oldIndex = rows.IndexOf(row);
                int newIndex = oldIndex + direction;

                if (newIndex >= 0 && newIndex < rows.Count)
                {
                    rows.RemoveAt(oldIndex);
                    rows.Insert(newIndex, row);
                }
            }

            // ✅ Quan trọng: Giữ chọn các dòng đã di chuyển
            DataGridRows.SelectedItems.Clear();
            foreach (var row in selectedRows)
            {
                DataGridRows.SelectedItems.Add(row);
            }

            // ✅ (Tùy chọn) Cuộn đến dòng được chọn nếu cần
            DataGridRows.ScrollIntoView(selectedRows.First());
        }

        private void SearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is SheetManagerViewModel vm)
            {
                vm.SearchCommand.Execute(null);
            }
        }

      
    }
}

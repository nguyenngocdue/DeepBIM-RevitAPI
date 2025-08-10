using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Win32;
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
    /// Interaction logic for SheetCreatetionWindow.xaml
    /// </summary>
    public partial class SheetCreatetionWindow : Window
    {
        private UIDocument _uiDoc;
        private Document _doc;

        // Dùng ObservableCollection để binding
        public ObservableCollection<string> SheetFamilies { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> SheetTypes { get; set; } = new ObservableCollection<string>();

        public SheetCreatetionWindow(UIDocument uiDoc)
        {
            InitializeComponent(); // Phải gọi trước
            _uiDoc = uiDoc;
            _doc = uiDoc.Document;
            this.DataContext = this;
            
            LoadSheetFamilies();

        }

        private void LoadSheetFamilies()
        {
            var sheetFamilies = new List<Family>();

            // Lọc các Family thuộc loại TitleBlock
            FilteredElementCollector collector = new FilteredElementCollector(_doc);
            var familyTypes = collector.OfClass(typeof(FamilySymbol))
                          .OfCategory(BuiltInCategory.OST_TitleBlocks)
                          .Cast<FamilySymbol>()
                          .Select(fs => fs.Family)
                          .DistinctBy(f => f.Id); // Loại bỏ trùng theo Id

            sheetFamilies.AddRange(familyTypes);

            // Gán vào ComboBox
            cmbFamily.ItemsSource = sheetFamilies;
        }

       
        private void cmbFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Xóa danh sách cũ
            cmbTypeName.ItemsSource = null;
            cmbTypeName.Items.Clear();

            // Kiểm tra xem có Family nào được chọn không
            if (cmbFamily.SelectedItem is Family selectedFamily)
            {
                // Lấy tất cả FamilySymbol (Type) thuộc về Family này
                var typeNames = new List<string>();

                var familySymbols = new FilteredElementCollector(_doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_TitleBlocks)
                    .Cast<FamilySymbol>()
                    .Where(fs => fs.Family.Id == selectedFamily.Id);

                foreach (var symbol in familySymbols)
                {
                    typeNames.Add(symbol.Name); // Thêm tên Type (ví dụ: "A1 Landscape", "A1 Portrait",...)
                }

                // Gán danh sách vào cmbTypeName
                cmbTypeName.ItemsSource = typeNames;

                // Chọn mục đầu tiên (tùy chọn)
                if (typeNames.Any())
                {
                    cmbTypeName.SelectedIndex = 0;
                }
            }
        }





        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Mở OpenFileDialog để chọn file Excel
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xls;*.xlsx|All Files|*.*",
                Title = "Select an Excel File"
            };

            // Nếu người dùng nhấn OK và chọn file hợp lệ
            if (openFileDialog.ShowDialog() == true)
            {
                // Gán đường dẫn file vào TextBox
                txtDirectory.Text = openFileDialog.FileName;
            }
        }


        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }


}

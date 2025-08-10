using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
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
using ClosedXML.Excel;
using System.Data;

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


        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra dữ liệu
            if (excelDataTable.ItemsSource == null)
            {
                MessageBox.Show("No data to process.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbFamily.SelectedItem == null || cmbTypeName.SelectedItem == null)
            {
                MessageBox.Show("Please select both Family and Type Name.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Lấy Family và Symbol
            Family selectedFamily = cmbFamily.SelectedItem as Family;
            string typeName = cmbTypeName.SelectedItem.ToString();
            FamilySymbol symbol = GetFamilySymbolByName(selectedFamily, typeName);

            if (symbol == null)
            {
                MessageBox.Show($"Symbol '{typeName}' not found in family '{selectedFamily.Name}'.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Lấy danh sách tất cả Sheet Number hiện có trong dự án
            var existingSheetNumbers = new HashSet<string>(
                new FilteredElementCollector(_doc)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>()
                    .Select(s => s.SheetNumber)
            );

            // Lấy dữ liệu từ DataGrid
            DataView dv = excelDataTable.ItemsSource as DataView;
            if (dv == null || dv.Count == 0)
            {
                MessageBox.Show("No valid data to process.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Bắt đầu transaction
            Transaction trans = new Transaction(_doc, "Update or Create Sheets");
            trans.Start();

            try
            {
                int updatedCount = 0;
                int createdCount = 0;

                foreach (DataRowView row in dv)
                {
                    string oldSheetNumber = row["Old Sheet Number"]?.ToString().Trim();
                    string newSheetNumber = row["Sheet Number"]?.ToString().Trim();
                    string newSheetName = row["Sheet Name"]?.ToString().Trim();

                    if (string.IsNullOrEmpty(newSheetNumber))
                    {
                        MessageBox.Show("New Sheet Number is required. Skipped a row.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue;
                    }

                    ViewSheet sheetToUpdate = null;

                    // 1. Tìm theo Old Sheet Number (nếu có)
                    if (!string.IsNullOrEmpty(oldSheetNumber))
                    {
                        sheetToUpdate = GetSheetByNumber(oldSheetNumber);
                    }

                    // 2. Nếu tìm thấy → cập nhật
                    if (sheetToUpdate != null)
                    {
                        // Kiểm tra trùng khi cập nhật Sheet Number?
                        if (existingSheetNumbers.Contains(newSheetNumber) && sheetToUpdate.SheetNumber != newSheetNumber)
                        {
                            // Nếu số mới đã tồn tại và khác với hiện tại → tạo tên mới
                            newSheetNumber = GetUniqueSheetNumber(newSheetNumber, existingSheetNumbers);
                        }

                        sheetToUpdate.SheetNumber = newSheetNumber;
                        sheetToUpdate.Name = newSheetName ?? "Unnamed";

                        // Cập nhật danh sách đã dùng
                        existingSheetNumbers.Remove(sheetToUpdate.SheetNumber); // Xóa cũ
                        existingSheetNumbers.Add(newSheetNumber); // Thêm mới

                        updatedCount++;
                    }
                    // 3. Nếu không tìm thấy → tạo mới
                    else
                    {
                        // Đảm bảo Symbol được Activate
                        if (!symbol.IsActive)
                        {
                            symbol.Activate();
                        }

                        // Đảm bảo Sheet Number là duy nhất
                        string uniqueSheetNumber = GetUniqueSheetNumber(newSheetNumber, existingSheetNumbers);

                        ViewSheet newSheet = ViewSheet.Create(_doc, symbol.Id);
                        newSheet.SheetNumber = uniqueSheetNumber;
                        newSheet.Name = newSheetName ?? "Unnamed";

                        existingSheetNumbers.Add(uniqueSheetNumber);
                        createdCount++;
                    }
                }

                trans.Commit();

                string message = $"Update completed:\n- Updated: {updatedCount}\n- Created: {createdCount}";
                MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                trans.RollBack();
                MessageBox.Show("Error during update/create: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ViewSheet GetSheetByNumber(string sheetNumber)
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .FirstOrDefault(s => s.SheetNumber == sheetNumber);
        }

        private string GetUniqueSheetNumber(string desiredNumber, HashSet<string> existingNumbers)
        {
            string number = desiredNumber;
            int suffix = 1;

            while (existingNumbers.Contains(number))
            {
                number = suffix == 1
                    ? $"{desiredNumber} - Copy"
                    : $"{desiredNumber} - Copy {suffix}";
                suffix++;
            }

            return number;
        }

        private FamilySymbol GetFamilySymbolByName(Family family, string typeName)
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .Cast<FamilySymbol>()
                .FirstOrDefault(fs => fs.Family.Id == family.Id && fs.Name == typeName);
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra dữ liệu
            if (excelDataTable.ItemsSource == null)
            {
                MessageBox.Show("No data to create sheets.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbFamily.SelectedItem == null || cmbTypeName.SelectedItem == null)
            {
                MessageBox.Show("Please select both Family and Type Name.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Lấy Family và Symbol
            Family selectedFamily = cmbFamily.SelectedItem as Family;
            string typeName = cmbTypeName.SelectedItem.ToString();

            FamilySymbol symbol = GetFamilySymbolByName(selectedFamily, typeName);
            if (symbol == null)
            {
                MessageBox.Show($"Symbol '{typeName}' not found in family '{selectedFamily.Name}'.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Lấy danh sách sheet từ DataGrid
            DataView dv = excelDataTable.ItemsSource as DataView;
            if (dv == null || dv.Count == 0)
            {
                MessageBox.Show("No valid data to process.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Danh sách sheet number đã tồn tại trong dự án (để kiểm tra trùng)
            var existingSheetNumbers = new HashSet<string>(
                new FilteredElementCollector(_doc)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>()
                    .Select(s => s.SheetNumber)
            );

            // Bắt đầu transaction
            Transaction trans = new Transaction(_doc, "Create Sheets");
            trans.Start();

            try
            {
                int createdCount = 0;

                foreach (DataRowView row in dv)
                {
                    string sheetNumber = row["Sheet Number"]?.ToString().Trim();
                    string sheetName = row["Sheet Name"]?.ToString().Trim();

                    if (string.IsNullOrEmpty(sheetNumber))
                    {
                        MessageBox.Show("Sheet Number is required. Skipped a row.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue;
                    }

                    // Kiểm tra trùng & tạo tên mới nếu cần
                    string uniqueSheetNumber = GetUniqueSheetNumber(sheetNumber, existingSheetNumbers);

                    // Tạo sheet
                    ViewSheet newSheet = ViewSheet.Create(_doc, symbol.Id);
                    if (!symbol.IsActive)
                    {
                        _doc.Regenerate(); // Cần regen để activate
                        symbol.Activate();
                    }
                    newSheet.SheetNumber = uniqueSheetNumber;
                    newSheet.Name = sheetName ?? "Unnamed";

                    // Cập nhật danh sách đã dùng
                    existingSheetNumbers.Add(uniqueSheetNumber);

                    createdCount++;
                }

                trans.Commit();
                MessageBox.Show($"Successfully created {createdCount} sheet(s).", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                trans.RollBack();
                MessageBox.Show("Error creating sheets: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }



        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BrowseExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls",
                Title = "Select Excel File"
            };

            if (dialog.ShowDialog() == true)
            {
                txtDirectory.Text = dialog.FileName;
                LoadExcelData(dialog.FileName);
            }
        }

        private void LoadExcelData(string filePath)
        {
            DataTable dt = ReadExcelToDataTable(filePath);
            if (dt != null && dt.Rows.Count > 0)
            {
                excelDataTable.ItemsSource = null;
                excelDataTable.ItemsSource = dt.DefaultView;

                // Thông báo để kiểm tra
                MessageBox.Show($"Loaded {dt.Rows.Count} rows with {dt.Columns.Count} columns.");
            }
            else
            {
                MessageBox.Show("Failed to load data or file is empty.");
            }
        }

        private DataTable ReadExcelToDataTable(string filePath)
        {
            var dt = new DataTable();

            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheets.First();

                    // Đọc dòng đầu tiên làm header
                    bool isFirstRow = true;
                    foreach (var row in worksheet.RowsUsed())
                    {
                        if (isFirstRow)
                        {
                            // Tạo cột từ dòng đầu
                            foreach (var cell in row.Cells())
                            {
                                string columnName = cell.Value.ToString().Trim();
                                // Đảm bảo tên cột hợp lệ
                                if (string.IsNullOrWhiteSpace(columnName))
                                    columnName = $"Column_{cell.Address.ColumnNumber}";

                                // Tránh trùng tên cột
                                if (dt.Columns.Contains(columnName))
                                    columnName = $"{columnName}_{Guid.NewGuid().ToString("N")[..4]}"; // thêm hậu tố

                                dt.Columns.Add(columnName);
                            }

                            isFirstRow = false;
                        }
                        else
                        {
                            // Đọc dữ liệu các dòng còn lại
                            var fieldList = new List<object>();
                            foreach (var cell in row.Cells(1, dt.Columns.Count)) // chỉ đọc đủ số cột
                            {
                                fieldList.Add(cell.Value.ToString());
                            }

                            // Bổ sung nếu thiếu cột
                            while (fieldList.Count < dt.Columns.Count)
                                fieldList.Add("");

                            dt.Rows.Add(fieldList.ToArray());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading Excel file: " + ex.Message);
                return null;
            }

            return dt;
        }
    }


}

using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.Messaging;
using DeepBIM.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace DeepBIM.ViewModels
{
    public class SheetRow: INotifyPropertyChanged
    {
        public string CurrentNumber { get; set; }
        public string NewNumber { get; set; }
        public string CurrentName { get; set; }
        private string _newName;
        public string NewName
        {
            get => _newName;
            set
            {
                if (_newName != value)
                {
                    _newName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NewName)));
                }
            }
        }


        public ElementId NewSheetId { get; set; }   // <-- để xóa trên Revit
        public bool IsSelected { get; internal set; }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class SheetItem : INotifyPropertyChanged
    {
        public ElementId Id { get; set; }
        public string Number { get; set; }
        public string Name { get; set; }
        public string Display => $"{Number} - {Name}";

        bool _isChecked;
        public bool IsChecked { get => _isChecked; set { _isChecked = value; PropertyChanged?.Invoke(this, new(nameof(IsChecked))); } }


        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class SheetManagerViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<SheetItem> Sheets { get; } = new();
        public ICollectionView SheetsView { get; }

        // <-- DataGrid nguồn dữ liệu
        public ObservableCollection<SheetRow> Rows { get; } = new();
        public bool SelectAll
        {
            get => _selectAll;
            set { _selectAll = value; foreach (var s in Sheets) s.IsChecked = value; OnChanged(nameof(SelectAll)); }
        }
        bool _selectAll;


        // Các quy tắc
        private string _prefix = "";
        private string _suffix = "";
        private string _find = "";
        private string _replace = "";
        private bool _applyToName = true; // mặc định chọn Sheet Name

        public string Prefix
        {
            get => _prefix;
            set { _prefix = value; OnChanged(nameof(Prefix)); }
        }

        public string Suffix
        {
            get => _suffix;
            set { _suffix = value; OnChanged(nameof(Suffix)); }
        }

        public string Find
        {
            get => _find;
            set { _find = value; OnChanged(nameof(Find)); }
        }

        public string Replace
        {
            get => _replace;
            set { _replace = value; OnChanged(nameof(Replace)); }
        }

        public bool ApplyToName
        {
            get => _applyToName;
            set { _applyToName = value; _applyToNumber = !value; OnChanged(nameof(ApplyToName)); OnChanged(nameof(ApplyToNumber)); }
        }

        private bool _applyToNumber = false;
        public bool ApplyToNumber
        {
            get => _applyToNumber;
            set { _applyToNumber = value; _applyToName = !value; OnChanged(nameof(ApplyToNumber)); OnChanged(nameof(ApplyToName)); }
        }


        public ICommand ApplyRulesCommand { get; }
        public ICommand RenameCommand { get; }


        private readonly Document _doc;
        public ICommand DuplicateCommand { get; }
        public ICommand DeleteSelectedSheetsCommand { get; }
        public ICommand MoveToRightCommand { get; }

        public SheetManagerViewModel(Document doc)
        {
            _doc = doc;
            var sheets = new FilteredElementCollector(doc).OfClass(typeof(ViewSheet))
                           .Cast<ViewSheet>()
                           .Where(v => !v.IsPlaceholder)
                           .OrderBy(v => v.SheetNumber, StringComparer.OrdinalIgnoreCase);

            foreach (var s in sheets)
                Sheets.Add(new SheetItem { Id = s.Id, Number = s.SheetNumber, Name = s.Name });

            SheetsView = CollectionViewSource.GetDefaultView(Sheets);

            // ✅ Khởi tạo các lệnh
            DuplicateCommand = new RelayCommand(DuplicateSelected, () => Sheets.Any(s => s.IsChecked));

            // ✅ Khởi tạo RemoveCommand
            DeleteSelectedSheetsCommand = new RelayCommand(
                 execute: DeleteSelectedSheets,
                 canExecute: () => Sheets.Any(s => s.IsChecked)
             );

            MoveToRightCommand = new RelayCommand(
                execute: MoveCheckedSheetsToRows,
                canExecute: () => true
            );

            ApplyRulesCommand = new RelayCommand(ApplyRules);
            RenameCommand = new RelayCommand(RenameSheets, () => Rows.Any(r => r.NewSheetId != null));
        }


        private void RenameSheets()
        {
            var sheetsToRename = Rows.Where(r => r.NewSheetId != null).ToList();
            if (!sheetsToRename.Any())
            {
                TaskDialog.Show("Thông báo", "Không có sheet nào đã được sao chép để đổi tên.");
                return;
            }

            using (var tg = new TransactionGroup(_doc, "Đổi tên sheet"))
            {
                tg.Start();

                foreach (var row in sheetsToRename)
                {
                    var sheet = _doc.GetElement(row.NewSheetId) as ViewSheet;
                    if (sheet == null) continue;

                    try
                    {
                        using (var t = new Transaction(_doc, "Đổi tên"))
                        {
                            t.Start();
                            sheet.Name = row.NewName;
                            t.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Lỗi", $"Không thể đổi tên sheet {row.NewNumber}: {ex.Message}");
                    }
                }

                tg.Assimilate();
            }
        }


        private void ApplyRules()
        {

            if (!ApplyToName)
            {
                TaskDialog.Show("Chỉ hỗ trợ Sheet Name", "Hiện tại chỉ hỗ trợ áp dụng quy tắc cho Sheet Name.");
                return;
            }

            foreach (var row in Rows)
            {
                System.Diagnostics.Debug.WriteLine($"🔍 Xử lý sheet: {row.CurrentName}");
                string name = row.CurrentName;

                // Tìm và thay thế
                if (!string.IsNullOrEmpty(Find))
                {
                    name = name.Replace(Find, Replace);
                }

                // Thêm tiền tố
                if (!string.IsNullOrEmpty(Prefix))
                {
                    name = Prefix + name;
                }

                // Thêm hậu tố
                if (!string.IsNullOrEmpty(Suffix))
                {
                    name = name + Suffix;
                }

                row.NewName = name;
            }
        }

        public void ExecuteMoveToLeft(List<SheetRow> selectedRows)
        {
            foreach (var row in selectedRows.ToList())
            {
                // Tìm sheet tương ứng
                var sheet = Sheets.FirstOrDefault(s =>
                    s.Number == row.CurrentNumber &&
                    s.Name == row.CurrentName);
                if (sheet != null)
                {
                    sheet.IsChecked = false; // Bỏ tích
                }

                Rows.Remove(row); // Xóa khỏi danh sách
            }
        }


        private void MoveCheckedSheetsToRows()
        {
            var checkedSheets = Sheets.Where(s => s.IsChecked).ToList();

            if (!checkedSheets.Any())
            {
                TaskDialog.Show("Thông báo", "Vui lòng chọn ít nhất một bản vẽ để di chuyển.");
                return;
            }

            foreach (var sheet in checkedSheets)
            {
                // Kiểm tra trùng trong Rows
                bool alreadyExists = Rows.Any(r =>
                    r.CurrentNumber == sheet.Number &&
                    r.CurrentName == sheet.Name);

                if (alreadyExists) continue; // Bỏ qua nếu đã có

                Rows.Add(new SheetRow
                {
                    CurrentNumber = sheet.Number,
                    CurrentName = sheet.Name,
                    NewNumber = GetUniqueSheetNumber(_doc, sheet.Number),
                    NewName = sheet.Name,
                    NewSheetId = sheet.Id,
                });
            }
        }


        private void DeleteSelectedSheets()
        {
            var selectedSheets = Sheets.Where(s => s.IsChecked).ToList();
            if (!selectedSheets.Any())
                return;

            // Hỏi người dùng xác nhận
            var result = TaskDialog.Show("Xác nhận xóa",
                $"Bạn có chắc muốn xóa {selectedSheets.Count} bản vẽ đã chọn?\n" +
                "Thao tác này không thể hoàn tác.",
                TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel);

            if (result != TaskDialogResult.Ok)
                return;

            // Danh sách sheet bị lỗi
            var failedSheets = new List<SheetItem>();

            using (var tg = new TransactionGroup(_doc, "Xóa các bản vẽ đã chọn"))
            {
                tg.Start();

                foreach (var sheetItem in selectedSheets)
                {
                    try
                    {
                        var sheet = _doc.GetElement(sheetItem.Id) as ViewSheet;
                        if (sheet == null) continue;

                        using (var t = new Transaction(_doc, "Xóa sheet"))
                        {
                            t.Start();
                            _doc.Delete(sheet.Id);
                            t.Commit();
                        }

                        // Xóa khỏi danh sách UI
                        Sheets.Remove(sheetItem);

                        // Xóa khỏi bảng kết quả (nếu có trong Rows)
                        var rowToRemove = Rows.FirstOrDefault(r => r.NewSheetId == sheetItem.Id);
                        if (rowToRemove != null)
                            Rows.Remove(rowToRemove);
                    }
                    catch (Exception ex)
                    {
                        // Ghi lại sheet lỗi để thông báo sau
                        failedSheets.Add(sheetItem);
                        TaskDialog.Show("Lỗi", $"Không thể xóa sheet {sheetItem.Number}: {ex.Message}");
                    }
                }

                tg.Assimilate();
            }

            // Cập nhật lại nút (nếu cần)
            CommandManager.InvalidateRequerySuggested();
        }

        private void DuplicateSelected()
        {
            var selected = Sheets.Where(s => s.IsChecked).ToList();
            if (!selected.Any()) return;

            using (var tg = new TransactionGroup(_doc, "Duplicate Sheets"))
            {
                tg.Start();

                foreach (var item in selected)
                {
                    var src = _doc.GetElement(item.Id) as ViewSheet;
                    if (src == null) continue;

                    string newNumber = GetUniqueSheetNumber(_doc, src.SheetNumber + "-COPY");
                    string newName = src.Name + " - Copy";

                    var created = DuplicateSheetWithViewsAndSchedules(_doc, src, newNumber, newName);
                    if (created == null) continue;

                    // ghi vào DataGrid
                    Rows.Add(new SheetRow
                    {
                        CurrentNumber = src.SheetNumber,
                        NewNumber = created.SheetNumber,
                        CurrentName = src.Name,
                        NewName = created.Name,
                        NewSheetId = created.Id
                    });

                    // thêm sheet mới vào panel trái (tuỳ ý)
                    Sheets.Add(new SheetItem
                    {
                        Id = created.Id,
                        Number = created.SheetNumber,
                        Name = created.Name,
                        IsChecked = true
                    });
                }

                tg.Assimilate();
            }
        }

        // ===== Helpers =====
        private static string GetUniqueSheetNumber(Document doc, string candidate)
        {
            string cand = candidate;
            int i = 2;
            while (IsSheetNumberExists(doc, cand))
            {
                cand = candidate + "-" + i.ToString();
                i++;
            }
            return cand;
        }

        private static bool IsSheetNumberExists(Document doc, string number)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet)).Cast<ViewSheet>()
                .Any(v => string.Equals(v.SheetNumber, number, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Tạo sheet mới cùng titleblock; duplicate các view thường (WithDetailing) & đặt lại vị trí;
        /// đặt lại legend/schedule (không duplicate); (tuỳ chọn) copy annotation trên sheet.
        /// </summary>
        private static ViewSheet DuplicateSheetWithViewsAndSchedules(Document doc, ViewSheet src, string newNumber, string newName)
        {
            ViewSheet created = null;

            using (var t = new Transaction(doc, $"Duplicate {src.SheetNumber}"))
            {
                t.Start();

                // 1) Lấy TitleBlock type từ sheet gốc
                FamilyInstance tbInst = new FilteredElementCollector(doc, src.Id)
                    .OfCategory(BuiltInCategory.OST_TitleBlocks)
                    .WhereElementIsNotElementType()
                    .Cast<FamilyInstance>()
                    .FirstOrDefault();

                ElementId tbTypeId = tbInst?.Symbol?.Id ?? ElementId.InvalidElementId;
                if (tbTypeId == ElementId.InvalidElementId)
                {
                    tbTypeId = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_TitleBlocks)
                        .WhereElementIsElementType()
                        .FirstElementId();
                }

                // 2) Tạo sheet mới
                created = ViewSheet.Create(doc, tbTypeId);
                if (created == null) { t.RollBack(); return null; }

                created.SheetNumber = newNumber;
                created.Name = newName;

                // 3) Duplicate & đặt lại viewports
                var viewports = new FilteredElementCollector(doc, src.Id)
                    .OfClass(typeof(Viewport)).Cast<Viewport>().ToList();

                foreach (var vp in viewports)
                {
                    var view = doc.GetElement(vp.ViewId) as View;
                    var center = vp.GetBoxCenter();

                    // Legend: đặt lại trực tiếp (legend dùng lại, không duplicate)
                    if (view.ViewType == ViewType.Legend)
                    {
                        if (Viewport.CanAddViewToSheet(doc, created.Id, view.Id))
                            Viewport.Create(doc, created.Id, view.Id, center);
                        continue;
                    }

                    // View thường: duplicate với detailing
                    var dupId = view.Duplicate(ViewDuplicateOption.WithDetailing);
                    if (Viewport.CanAddViewToSheet(doc, created.Id, dupId))
                        Viewport.Create(doc, created.Id, dupId, center);
                }

                // 4) Schedule: đặt lại (không duplicate)
                var schedules = new FilteredElementCollector(doc, src.Id)
                    .OfClass(typeof(ScheduleSheetInstance))
                    .Cast<ScheduleSheetInstance>()
                    .ToList();

                foreach (var ssi in schedules)
                {
                    var schedView = doc.GetElement(ssi.ScheduleId) as ViewSchedule;
                    if (schedView == null) continue;

                    try
                    {
                        ScheduleSheetInstance.Create(doc, created.Id, schedView.Id, ssi.Point);
                    }
                    catch (Autodesk.Revit.Exceptions.ArgumentException) { /* schedule internal => bỏ qua */ }
                    catch (Autodesk.Revit.Exceptions.InvalidOperationException) { /* không place được => bỏ qua */ }
                }




                // 5) (Tuỳ chọn) copy annotation trực tiếp trên sheet (text, generic annotation...)
                //    Bỏ qua titleblock
                if (tbInst != null)
                {
                    var ex = new ExclusionFilter(new[] { tbInst.Id });
                    var cats = new LogicalOrFilter(new ElementFilter[]
                    {
                        new ElementCategoryFilter(BuiltInCategory.OST_TextNotes),
                        new ElementCategoryFilter(BuiltInCategory.OST_GenericAnnotation)
                    });

                    var toCopy = new FilteredElementCollector(doc, src.Id)
                        .WherePasses(new LogicalAndFilter(ex, cats))
                        .Select(e => e.Id)
                        .ToList();

                    if (toCopy.Any())
                        ElementTransformUtils.CopyElements(src, toCopy, created, Transform.Identity, new CopyPasteOptions());
                }

                t.Commit();
            }

            return created;
        }


        void OnChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        public event PropertyChangedEventHandler PropertyChanged;
    }
}

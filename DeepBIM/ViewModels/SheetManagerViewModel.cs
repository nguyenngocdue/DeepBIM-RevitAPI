using Autodesk.Revit.UI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;

namespace DeepBIM.ViewModels
{
    public class SheetRow : INotifyPropertyChanged
    {
        private string _currentNumber;
        public string CurrentNumber
        {
            get => _currentNumber;
            set
            {
                if (_currentNumber != value)
                {
                    _currentNumber = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentNumber)));
                }
            }
        }

        private string _newNumber;
        public string NewNumber
        {
            get => _newNumber;
            set
            {
                if (_newNumber != value)
                {
                    _newNumber = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NewNumber)));
                }
            }
        }

        private string _currentName;
        public string CurrentName
        {
            get => _currentName;
            set
            {
                if (_currentName != value)
                {
                    _currentName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentName)));
                }
            }
        }
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
        private ElementId _id;
        private string _number;
        private string _name;
        private bool _isChecked;

        public ElementId Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        public string Number
        {
            get => _number;
            set
            {
                if (_number != value)
                {
                    _number = value;
                    OnPropertyChanged(nameof(Number));
                    OnPropertyChanged(nameof(Display)); // ✅ Bắt buộc
                }
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                    OnPropertyChanged(nameof(Display)); // ✅ Bắt buộc
                }
            }
        }

        public string Display => $"{Number} - {Name}";

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged(nameof(IsChecked));
                    CommandManager.InvalidateRequerySuggested(); // Cập nhật nút
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SheetManagerViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<SheetItem> Sheets { get; } = new();
        public ICollectionView SheetsView { get; }

        // <-- DataGrid nguồn dữ liệu
        public ObservableCollection<SheetRow> Rows { get; } = new();
        public bool SelectAll
        {
            // Chỉ true khi tất cả item đang HIỂN THỊ đều check
            get
            {
                if (SheetsView == null) return false;
                var visible = SheetsView.Cast<object>().OfType<SheetItem>().ToList();
                if (visible.Count == 0) return false;
                return visible.All(s => s.IsChecked);
            }
            // Set → áp dụng cho CÁC ITEM ĐANG HIỂN THỊ (sau filter)
            set
            {
                if (SheetsView == null) return;
                foreach (SheetItem s in SheetsView) s.IsChecked = value;
                OnChanged(nameof(SelectAll)); // cập nhật trạng thái checkbox
            }
        }



        private void HookSheetItem(SheetItem s)
        {
            s.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(SheetItem.IsChecked))
                    OnChanged(nameof(SelectAll));   // tự cập nhật lại ô Select all
            };
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



        private string _prefixNumber = "";
        private string _startNumber = "";

        public string PrefixNumber
        {
            get => _prefixNumber;
            set { _prefixNumber = value; OnChanged(nameof(PrefixNumber)); }
        }

        public string StartNumber
        {
            get => _startNumber;
            set { _startNumber = value; OnChanged(nameof(StartNumber)); }
        }

        private string _scopeOption;
        public string ScopeOption
        {
            get => _scopeOption;
            set
            {
                if (_scopeOption != value)
                {
                    _scopeOption = value;
                    OnChanged(nameof(ScopeOption));
                    OnChanged(nameof(IsAllSelected));
                    OnChanged(nameof(IsSelectedSelected));
                }
            }
        }

        public bool IsAllSelected
        {
            get => ScopeOption == "All";
            set
            {
                if (value) ScopeOption = "All";
            }
        }

        public bool IsSelectedSelected
        {
            get => ScopeOption == "Selected";
            set
            {
                if (value) ScopeOption = "Selected";
            }
        }

        // Search
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnChanged(nameof(SearchText));
                ApplyFilter();
            }
        }

        public ICommand SearchCommand { get; }

        public ICommand ArrangeSheetNumbersCommand { get; }
        public ICommand ApplyRulesCommand { get; }
        public ICommand RenameCommand { get; }

        private readonly Document _doc;
        private readonly UIDocument _uidoc;
        public ICommand DuplicateCommand { get; }
        public ICommand DeleteSelectedSheetsCommand { get; }
        public ICommand MoveToRightCommand { get; }

        public ICommand ScopeChangedCommand { get; }
        public ICommand RefreshCommand { get; }

        private bool _initializing;
        public bool HasRevitSelection { get; private set; }

        public SheetManagerViewModel(Document doc, UIDocument uidoc, ICollection<ElementId> selectedIds = null)
        {
            _doc = doc;
            _uidoc = uidoc;
            var allSheets = new FilteredElementCollector(doc).OfClass(typeof(ViewSheet))
                           .Cast<ViewSheet>()
                           .Where(v => !v.IsPlaceholder)
                           .OrderBy(v => v.SheetNumber, StringComparer.OrdinalIgnoreCase)
                           .ToList();
            // Có selection là sheet?
            var selectedSheetElements = new List<ViewSheet>();
            if (selectedIds != null && selectedIds.Any())
                selectedSheetElements = allSheets.Where(s => selectedIds.Contains(s.Id)).ToList();

            HasRevitSelection = selectedSheetElements.Any();

            _initializing = true; // chặn handler khi set mặc định

            if (HasRevitSelection)
            {
                // Default: Current selection
                foreach (var s in selectedSheetElements)
                {
                    var item = new SheetItem { Id = s.Id, Number = s.SheetNumber, Name = s.Name };
                    Sheets.Add(item);
                    HookSheetItem(item);
                }
                ScopeOption = "Selected";
            }
            else
            {
                // ✅ Tải toàn bộ
                foreach (var s in allSheets)
                {
                    var item = new SheetItem { Id = s.Id, Number = s.SheetNumber, Name = s.Name };
                    Sheets.Add(item);
                    HookSheetItem(item);
                }

                ScopeOption = "All";
            }
            _initializing = false;
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

            ArrangeSheetNumbersCommand = new RelayCommand(ArrangeSheetNumbers);

            ScopeChangedCommand = new RelayCommand<string>(param =>
            {
                ScopeOption = param; // "All" hoặc "Selected"
                OnScopeChanged();    // Tải lại danh sách
            });

            SearchCommand = new RelayCommand(SearchSheets);

            RefreshCommand = new RelayCommand(
                execute: RefreshRows,
                canExecute: () => Sheets.Any(s => s.IsChecked)
            );
        }

        private void RefreshRows()
        {
            // Xác nhận (tùy chọn)
            var result = TaskDialog.Show("Làm mới danh sách",
                "Bạn có chắc muốn làm mới danh sách?\n" +
                "Tất cả thay đổi trong bảng sẽ bị mất.",
                TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);

            if (result != TaskDialogResult.Yes)
                return;

            // Xóa hết dòng cũ
            Rows.Clear();

            // Lấy các sheet đang được tích
            var checkedSheets = Sheets.Where(s => s.IsChecked).ToList();

            if (!checkedSheets.Any())
            {
                TaskDialog.Show("Thông báo", "Không có sheet nào được chọn để làm mới.");
                return;
            }

            // Thêm lại vào Rows với giá trị gốc
            foreach (var sheet in checkedSheets)
            {
                Rows.Add(new SheetRow
                {
                    CurrentNumber = sheet.Number,
                    CurrentName = sheet.Name,
                    NewNumber = sheet.Number,   // Reset về số gốc
                    NewName = sheet.Name        // Reset về tên gốc
                });
            }

            // (Tùy chọn) Cuộn xuống cuối
            // Nếu có DataGrid, có thể scroll: DataGridRows.ScrollIntoView(Rows.Last());
        }

        private void ApplyFilter()
        {
            if (SheetsView == null) return;

            if (string.IsNullOrWhiteSpace(_searchText))
            {
                SheetsView.Filter = null;
            }
            else
            {
                string keyword = _searchText.Trim().ToLower();

                SheetsView.Filter = item =>
                {
                    if (item is SheetItem sheet)
                    {
                        return sheet.Number.ToLower().Contains(keyword) ||
                               sheet.Name.ToLower().Contains(keyword);
                    }
                    return false;
                };
            }

            SheetsView.Refresh();
            OnChanged(nameof(SelectAll));
        }

        private void SearchSheets()
        {
            if (SheetsView == null) return;

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // Nếu trống → hiển thị tất cả
                SheetsView.Filter = null;
            }
            else
            {
                string keyword = SearchText.Trim().ToLower();

                SheetsView.Filter = item =>
                {
                    if (item is SheetItem sheet)
                    {
                        return sheet.Number.ToLower().Contains(keyword) ||
                               sheet.Name.ToLower().Contains(keyword);
                    }
                    return false;
                };
            }

            // Cập nhật UI
            SheetsView.Refresh();
            OnChanged(nameof(SelectAll));
        }

        private void OnScopeChanged()
        {
            // Xóa dữ liệu cũ
            Sheets.Clear();
            

            var allSheets = new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .Where(v => !v.IsPlaceholder)
                .OrderBy(v => v.SheetNumber, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (IsSelectedSelected)
            {
                // Lấy danh sách sheet đang được chọn trong Revit
                var selectedIds = _uidoc.Selection.GetElementIds();
                var selectedSheetElements = allSheets
                    .Where(s => selectedIds.Contains(s.Id))
                    .ToList();

                foreach (var s in selectedSheetElements)
                {
                    Sheets.Add(new SheetItem { Id = s.Id, Number = s.SheetNumber, Name = s.Name });
                }
            }
            else // IsAllSelected
            {
                foreach (var s in allSheets)
                {
                    Sheets.Add(new SheetItem { Id = s.Id, Number = s.SheetNumber, Name = s.Name });
                }
            }

            // Cập nhật UI
            SheetsView.Refresh();
            OnChanged(nameof(SelectAll));
        }

        private void ArrangeSheetNumbers()
        {
            if (Rows.Count == 0)
            {
                TaskDialog.Show("Notification", "There are no sheets to arrange.");
                return;
            }

            string startStr = _startNumber?.Trim() ?? "1";
            if (string.IsNullOrEmpty(startStr))
            {
                TaskDialog.Show("Error", "Starting number cannot be empty.");
                return;
            }

            // Extract the numeric part from the string (e.g., "001" → 1, "00" → 0)
            if (!int.TryParse(startStr, out int start))
            {
                TaskDialog.Show("Error", "Starting number must be numeric.");
                return;
            }

            string prefix = _prefixNumber ?? "";

            // Determine the padding length from _startNumber (e.g., "001" → 3, "00" → 2)
            int padding = startStr.Length; // ← Key: use the original string length

            // Get all existing sheet numbers in the project to avoid duplicates
            var existingNumbers = new HashSet<string>(
                new FilteredElementCollector(_doc)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>()
                    .Select(v => v.SheetNumber)
            );

            int current = start;

            foreach (var row in Rows)
            {
                string newNumber;
                do
                {
                    string numberPart = current.ToString().PadLeft(padding, '0');
                    newNumber = prefix + numberPart;
                    current++;
                }
                while (existingNumbers.Contains(newNumber));

                row.NewNumber = newNumber;
            }

            // ✅ Update UI
            TaskDialog.Show("Completed", $"Arranged {Rows.Count} sheets in order.\n\nPlease review the new sheet numbers carefully before clicking Apply.");

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
                            sheet.SheetNumber = row.NewNumber;
                            t.Commit();
                        }

                        // ✅ CẬP NHẬT DỮ LIỆU TRONG VIEWMODEL
                        var sheetItem = Sheets.FirstOrDefault(s => s.Id == row.NewSheetId);
                        if (sheetItem != null)
                        {
                            sheetItem.Number = row.NewNumber; // ← Cập nhật số
                            sheetItem.Name = row.NewName;     // ← Cập nhật tên
                                                              // Display sẽ tự động cập nhật nếu có INotifyPropertyChanged
                        }

                        // ✅ CẬP NHẬT LẠI CurrentNumber và CurrentName trong Rows
                        row.CurrentNumber = row.NewNumber;
                        row.CurrentName = row.NewName;

                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Lỗi", $"Không thể đổi tên sheet {row.NewNumber}: {ex.Message}");
                    }
                }

                tg.Assimilate();
            }
            // ✅ Buộc UI cập nhật
            SheetsView.Refresh();


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
                    NewNumber = sheet.Number,
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

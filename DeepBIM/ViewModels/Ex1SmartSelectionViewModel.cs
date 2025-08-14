using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DeepBIM.Events;
using DeepBIM.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;

namespace DeepBIM.ViewModels
{
    public class Ex1SmartSelectionViewModel : INotifyPropertyChanged
    {
        private readonly UIApplication _uiApp;
        private readonly UIDocument _uiDoc;
        private readonly Document _doc;
        private readonly View _view;

        // External event (để sẵn nếu bạn cần sau này)
        private readonly ExternalRevitEventHandler _externalHandler;
        private readonly ExternalEvent _externalEvent;

        public ObservableCollection<CategoryItem> CategoryView { get; set; }
        private ObservableCollection<CategoryItem> _allCategories;

        // === Select All (tri-state) ===
        private bool? _selectAll;
        public bool? SelectAll
        {
            get => _selectAll;
            set
            {
                if (_selectAll != value)
                {
                    _selectAll = value;
                    if (value.HasValue)
                    {
                        foreach (var item in CategoryView)
                            item.IsChecked = value.Value;
                    }
                    OnPropertyChanged();
                    RaiseCommandsCanExecuteChanged();
                }
            }
        }

        // === Commands ===
        public ICommand ApplyColorCommand { get; }
        public ICommand ClearOverridesCommand { get; }
        private ICommand SetColorCommand { get; }
        public ICommand ApplyCommand { get; } // Giữ nguyên lệnh select elements

        public Ex1SmartSelectionViewModel(UIApplication uiApp, UIDocument uiDoc, Document doc)
        {
            _uiApp = uiApp;
            _uiDoc = uiDoc;
            _doc = doc;
            _view = _uiDoc.ActiveView;

            CategoryView = new ObservableCollection<CategoryItem>();
            LoadCategories();

            _selectAll = false;

            ApplyColorCommand = new RelayCommand(
                execute: ApplyElementColor_ByElement,
                canExecute: () => CategoryView != null && CategoryView.Any(c => c.IsChecked)
            );

            ClearOverridesCommand = new RelayCommand(
                execute: ClearOverrides,
                canExecute: () => CategoryView != null && CategoryView.Any(c => c.IsChecked)
            );

            SetColorCommand = new RelayCommand(
                execute: (param) => SetColor(param as string),
                canExecute: (param) => !string.IsNullOrWhiteSpace(param as string)
            );

            ApplyCommand = new RelayCommand(
                execute: ApplyUI,
                canExecute: () => CategoryView != null && CategoryView.Any(c => c.IsChecked)
            );

            RehookPropertyEvents();
        }

        private void LoadCategories()
        {
            var items = _doc.Settings.Categories
                .Cast<Category>()
                .Where(cat => !string.IsNullOrEmpty(cat.Name) &&
                              (cat.CategoryType == CategoryType.Model || cat.CategoryType == CategoryType.Annotation) &&
                              cat.Parent == null)
                .Select(cat => new CategoryItem
                {
                    Display = cat.Name,
                    CategoryId = cat.Id,
                    IsChecked = false
                })
                .OrderBy(x => x.Display, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _allCategories = new ObservableCollection<CategoryItem>(items);
            CategoryView = new ObservableCollection<CategoryItem>(items);

            RehookPropertyEvents();
        }

        private void OnCategoryItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CategoryItem.IsChecked))
            {
                UpdateSelectAllState();
                RaiseCommandsCanExecuteChanged();
            }
        }

        private void UpdateSelectAllState()
        {
            var checkedCount = CategoryView.Count(c => c.IsChecked);
            var totalCount = CategoryView.Count;

            bool? newState = checkedCount == 0 ? false :
                             (checkedCount == totalCount ? true : (bool?)null);

            if (_selectAll != newState)
            {
                _selectAll = newState;
                OnPropertyChanged(nameof(SelectAll));
            }
        }

        private void RaiseCommandsCanExecuteChanged()
        {
            (ApplyColorCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ClearOverridesCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ApplyCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        // === Chọn elements theo Category (giữ nguyên ý tưởng của bạn) ===
        private void ApplyUI()
        {
            var selectedCategories = CategoryView.Where(item => item.IsChecked).ToList();
            if (!selectedCategories.Any())
            {
                TaskDialog.Show("Thông báo", "Vui lòng chọn ít nhất một danh mục.");
                return;
            }

            var elementIds = new List<ElementId>();
            try
            {
                using (var t = new Transaction(_doc, "Select Elements"))
                {
                    t.Start();

                    foreach (var item in selectedCategories)
                    {
                        var elements = new FilteredElementCollector(_doc)
                            .OfCategoryId(item.CategoryId)
                            .WhereElementIsNotElementType()
                            .ToElements();

                        foreach (var elem in elements)
                        {
                            if (elem.Id != ElementId.InvalidElementId)
                                elementIds.Add(elem.Id);
                        }
                    }

                    if (elementIds.Count > 10000)
                    {
                        TaskDialog.Show("Cảnh báo", $"Quá nhiều phần tử ({elementIds.Count}), chỉ chọn 10,000 đầu.");
                        elementIds = elementIds.Take(10000).ToList();
                    }

                    _uiApp.ActiveUIDocument.Selection.SetElementIds(elementIds);
                    t.Commit();

                    TaskDialog.Show("Thành công", $"Đã chọn {elementIds.Count} phần tử.");
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Lỗi", "Không thể chọn phần tử: " + ex.Message);
            }
        }

        // === Search ===
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    ExecuteSearchCommand(null);
                }
            }
        }

        private RelayCommand _searchCommand;
        public ICommand SearchCommand => _searchCommand ??= new RelayCommand(ExecuteSearchCommand);

        private void ExecuteSearchCommand(object parameter)
        {
            IEnumerable<CategoryItem> source = _allCategories;
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                source = _allCategories.Where(x =>
                    x.Display.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            CategoryView = new ObservableCollection<CategoryItem>(source.OrderBy(x => x.Display));
            OnPropertyChanged(nameof(CategoryView));
            RehookPropertyEvents();
            UpdateSelectAllState();
            RaiseCommandsCanExecuteChanged();
        }

        // ===================================================
        // COLOR FEATURES
        // ===================================================

        private System.Windows.Media.Color _selectedColor = System.Windows.Media.Colors.Yellow;
        public System.Windows.Media.Color SelectedColor
        {
            get => _selectedColor;
            set
            {
                if (_selectedColor != value)
                {
                    _selectedColor = value;
                    OnPropertyChanged();
                }
            }
        }

        // 0..90 theo UI (Revit cho 0..100)
        private int _selectedTransparency = 0;
        public int SelectedTransparency
        {
            get => _selectedTransparency;
            set
            {
                if (_selectedTransparency != value)
                {
                    _selectedTransparency = Math.Max(0, Math.Min(90, value));
                    OnPropertyChanged();
                }
            }
        }

        private bool _isHalftone;
        public bool IsHalftone
        {
            get => _isHalftone;
            set
            {
                if (_isHalftone != value)
                {
                    _isHalftone = value;
                    OnPropertyChanged();
                }
            }
        }

        public void SetColor(string colorName)
        {
            var colorObj = ColorConverter.ConvertFromString(colorName);
            if (colorObj is System.Windows.Media.Color wpfColor)
            {
                SelectedColor = wpfColor;
            }
            else
            {
                TaskDialog.Show("Lỗi", $"Không nhận diện được màu: '{colorName}'.\n" +
                    "Hỗ trợ tên màu (Red, Blue, …) hoặc mã hex (#FF0000).");
            }
        }

        // === Áp dụng tô màu Category theo View hiện tại ===
        public void ApplyElementColor()
        {
            var selectedCategories = CategoryView.Where(x => x.IsChecked).ToList();
            if (!selectedCategories.Any())
            {
                TaskDialog.Show("Thông báo", "Vui lòng chọn ít nhất một danh mục.");
                return;
            }

            // Chuẩn bị màu
            var revitColor = new Autodesk.Revit.DB.Color(SelectedColor.R, SelectedColor.G, SelectedColor.B);

            // Tìm Solid fill
            var fill = PatternElementUtils.FindFillPatternByName(_doc, "<Solid fill>");
            if (fill == null)
            {
                TaskDialog.Show("Lỗi", "Không tìm thấy mẫu tô đặc 'Solid fill'.");
                return;
            }

            int applied = 0, skipped = 0;

            try
            {
                using (var t = new Transaction(_doc, "Tô màu danh mục"))
                {
                    t.Start();

                    var ogs = new OverrideGraphicSettings();
                    #if REVIT2022_OR_NEWER
                                        // API mới vẫn giữ các method này
                    #endif
                    ogs.SetSurfaceForegroundPatternId(fill.Id);
                    ogs.SetSurfaceForegroundPatternColor(revitColor);
                    ogs.SetSurfaceBackgroundPatternColor(revitColor);
                    ogs.SetSurfaceTransparency(SelectedTransparency); // 0..100
                    if (IsHalftone)
                        ogs.SetHalftone(true);

                    foreach (var cat in selectedCategories)
                    {
                        if (TrySetCategoryOverrides(cat.CategoryId, ogs)) applied++;
                        else skipped++;
                    }

                    t.Commit();
                }
                TaskDialog.Show("Kết quả",
                                 $"Đã tô màu {applied} category.\nBỏ qua {skipped} category không hỗ trợ override.");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Lỗi", "Không thể tô màu danh mục: " + ex.Message);
            }
        }


        private void ApplyElementColor_ByElement()
        {
            var selectedCategories = CategoryView.Where(x => x.IsChecked).ToList();
            if (!selectedCategories.Any())
            {
                TaskDialog.Show("Thông báo", "Vui lòng chọn ít nhất một danh mục.");
                return;
            }

            var revitColor = new Autodesk.Revit.DB.Color(SelectedColor.R, SelectedColor.G, SelectedColor.B);
            var fill = PatternElementUtils.FindFillPatternByName(_doc, "<Solid fill>");
            if (fill == null)
            {
                TaskDialog.Show("Lỗi", "Không tìm thấy mẫu tô đặc 'Solid fill'.");
                return;
            }

            int applied = 0, skipped = 0;

            try
            {
                using (var t = new Transaction(_doc, "Tô màu By Element"))
                {
                    t.Start();

                    var ogs = new OverrideGraphicSettings();
                    ogs.SetSurfaceForegroundPatternId(fill.Id);
                    ogs.SetSurfaceForegroundPatternColor(revitColor);
                    ogs.SetSurfaceBackgroundPatternColor(revitColor);
                    ogs.SetSurfaceTransparency(SelectedTransparency);
                    if (IsHalftone) ogs.SetHalftone(true);

                    foreach (var cat in selectedCategories)
                    {
                        // Lấy toàn bộ element trong category
                        var elements = new FilteredElementCollector(_doc, _view.Id)
                            .OfCategoryId(cat.CategoryId)
                            .WhereElementIsNotElementType()
                            .ToElementIds();

                        foreach (var elemId in elements)
                        {
                            try
                            {
                                _view.SetElementOverrides(elemId, ogs);
                                applied++;
                            }
                            catch
                            {
                                skipped++;
                            }
                        }
                    }

                    t.Commit();
                }

                TaskDialog.Show("Kết quả",
                    $"Đã tô màu {applied} phần tử.\nBỏ qua {skipped} phần tử không thể override.");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Lỗi", "Không thể tô màu phần tử: " + ex.Message);
            }
        }



        private bool TrySetCategoryOverrides(ElementId catId, OverrideGraphicSettings ogs)
        {
            if (!CanOverrideCategory(catId)) return false;
            try
            {
                _view.SetCategoryOverrides(catId, ogs);
                return true;
            }
            catch
            {
                // Ví dụ: "Category cannot be overridden" → bỏ qua
                return false;
            }
        }

        private bool CanOverrideCategory(ElementId catId)
        {
            try
            {
                if (catId == ElementId.InvalidElementId) return false;
                // View dạng Schedule không hỗ trợ override đồ họa theo Category
                if (_view is ViewSchedule) return false;

                // Proxy check: nếu không thể Hide theo Category trong view này,
                // thường cũng không override được.
                return _view.CanCategoryBeHidden(catId);
            }
            catch
            {
                return false;
            }
        }

        // === Xóa override của các Category đã chọn ===
        private void ClearOverrides()
        {
            var selectedCategories = CategoryView.Where(x => x.IsChecked).ToList();
            if (!selectedCategories.Any())
            {
                TaskDialog.Show("Thông báo", "Vui lòng chọn ít nhất một danh mục để xóa override.");
                return;
            }

            int cleared = 0, skipped = 0, failed = 0;

            try
            {
                using (var t = new Transaction(_doc, "Clear category overrides"))
                {
                    t.Start();

                    var resetOgs = new OverrideGraphicSettings(); // mặc định: reset
                    foreach (var cat in selectedCategories)
                    {
                        var catId = cat.CategoryId;

                        // Bỏ qua những cái không hợp lệ / không hỗ trợ
                        if (catId == ElementId.InvalidElementId || !CanOverrideCategory(catId))
                        {
                            skipped++;
                            continue;
                        }

                        try
                        {
                            _view.SetCategoryOverrides(catId, resetOgs);
                            cleared++;
                        }
                        catch (Autodesk.Revit.Exceptions.ArgumentException)
                        {
                            // ví dụ: "Category cannot be overridden"
                            skipped++;
                        }
                        catch
                        {
                            failed++;
                        }
                    }

                    t.Commit();
                }

                TaskDialog.Show(
                    "Kết quả",
                    $"Đã xóa override: {cleared}\nBỏ qua (không hỗ trợ): {skipped}\nThất bại khác: {failed}"
                );
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Lỗi", "Không thể xóa override: " + ex.Message);
            }
        }


        // ===================================================
        // Rehook property changed events
        private void RehookPropertyEvents()
        {
            foreach (var item in CategoryView)
            {
                item.PropertyChanged -= OnCategoryItemPropertyChanged;
                item.PropertyChanged += OnCategoryItemPropertyChanged;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

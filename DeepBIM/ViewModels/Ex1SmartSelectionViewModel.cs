using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DeepBIM.Events;
using DeepBIM.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;                // Application
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DeepBIM.ViewModels
{

    // ===== Row model cho bảng Processing =====
    public class ProcessingRow : INotifyPropertyChanged
    {
        private System.Windows.Media.Color _color = System.Windows.Media.Colors.Transparent;
        public System.Windows.Media.Color Color
        {
            get => _color;
            set => _color = value;
        }

        public ElementId CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int RowIndex { get; set; }

        private int _amount;
        public int Amount { get => _amount; set { if (_amount != value) { _amount = value; OnPropertyChanged(nameof(Amount)); } } }
       

        private Brush _colorBrush = Brushes.Transparent;
        public Brush ColorBrush { get => _colorBrush; set { if (_colorBrush != value) { _colorBrush = value; OnPropertyChanged(nameof(ColorBrush)); } } }

        private bool _isIsolated;
        public bool IsIsolated { get => _isIsolated; set { if (_isIsolated != value) { _isIsolated = value; OnPropertyChanged(nameof(IsIsolated)); } } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // ===== ViewModel chính =====
    public class Ex1SmartSelectionViewModel : INotifyPropertyChanged
    {
        private readonly UIApplication _uiApp;
        private readonly UIDocument _uiDoc;
        private readonly Document _doc;
        private readonly View _view;

        // Dispatcher UI để cập nhật ObservableCollection an toàn
        private readonly Dispatcher _uiDispatcher;

        // External Event (DeepBIM.Events)
        private readonly ExternalRevitEventHandler _externalHandler;
        private readonly ExternalEvent _externalEvent;

        public ObservableCollection<CategoryItem> CategoryView { get; set; }
        private ObservableCollection<CategoryItem> _allCategories;

        public ObservableCollection<ProcessingRow> ProcessingRows { get; } = new();

        // ==== Select All (tri-state) ====
        private bool? _selectAll = false;
        public bool? SelectAll
        {
            get => _selectAll;
            set
            {
                if (_selectAll != value)
                {
                    _selectAll = value;
                    if (value.HasValue && CategoryView != null)
                        foreach (var item in CategoryView) item.IsChecked = value.Value;
                    OnPropertyChanged();
                    RaiseCommandsCanExecuteChanged();
                }
            }
        }

        // ==== Commands ====
        public ICommand ApplyColorCommand { get; }
        public ICommand ClearOverridesCommand { get; }
        private ICommand SetColorCommand { get; }
        public ICommand ApplyCommand { get; }

        // (bạn có thể chuyển 4 command dưới sang ExternalEvent tương tự nếu muốn)
        public ICommand ToggleIsolateCommand { get; }
        public ICommand HighlightCommand { get; }
        public ICommand EditColorCommand { get; }
        public ICommand ResetRowCommand { get; }
        public ICommand DeleteRowCommand { get; }

        public Ex1SmartSelectionViewModel(UIApplication uiApp, UIDocument uiDoc, Document doc)
        {
            _uiApp = uiApp;
            _uiDoc = uiDoc;
            _doc   = doc;
            _view  = _uiDoc.ActiveView;

            // Lưu dispatcher UI hiện tại
            _uiDispatcher  = Dispatcher.CurrentDispatcher;

            // External Event
            _externalHandler = new ExternalRevitEventHandler();
            _externalEvent   = ExternalEvent.Create(_externalHandler);

            // Categories
            CategoryView = new ObservableCollection<CategoryItem>();
            LoadCategories();

            // ---- Commands ----
            ApplyColorCommand = new RelayCommand(
                execute: () =>
                {
                    _externalHandler.SetAction(app => ApplyElementColor_ByElement_API(app));
                    _externalEvent.Raise();
                },
                canExecute: () => CategoryView != null && CategoryView.Any(c => c.IsChecked)
            );

            ClearOverridesCommand = new RelayCommand(
                execute: () =>
                {
                    _externalHandler.SetAction(app => ClearOverrides_ByElement_API(app));
                    _externalEvent.Raise();
                },
                canExecute: () => CategoryView != null && CategoryView.Any(c => c.IsChecked)
            );

            SetColorCommand = new RelayCommand(
                execute: (param) => SetColor(param as string),
                canExecute: (param) => !string.IsNullOrWhiteSpace(param as string)
            );

            // Chọn elements theo category (không đổi đồ họa) → có thể giữ chạy trực tiếp
            ApplyCommand = new RelayCommand(
                execute: ApplyUI,
                canExecute: () => CategoryView != null && CategoryView.Any(c => c.IsChecked)
            );

            RehookPropertyEvents();
        }

        // ===== Load category list =====
        private void LoadCategories()
        {
            var items = _doc.Settings.Categories
                .Cast<Category>()
                .Where(cat => !string.IsNullOrEmpty(cat.Name)
                           && (cat.CategoryType == CategoryType.Model || cat.CategoryType == CategoryType.Annotation)
                           && cat.Parent == null)
                .Select(cat => new CategoryItem { Display = cat.Name, CategoryId = cat.Id, IsChecked = false })
                .OrderBy(x => x.Display, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _allCategories = new ObservableCollection<CategoryItem>(items);
            CategoryView   = new ObservableCollection<CategoryItem>(items);

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
            var total       = CategoryView?.Count ?? 0;
            var checkedCount= CategoryView?.Count(c => c.IsChecked) ?? 0;

            bool? newState = checkedCount == 0 ? false :
                             (checkedCount == total ? true : (bool?)null);

            if (_selectAll != newState)
            {
                _selectAll = newState;
                OnPropertyChanged(nameof(SelectAll));
            }
        }

        private void RaiseCommandsCanExecuteChanged()
        {
            (ApplyColorCommand     as RelayCommand)?.RaiseCanExecuteChanged();
            (ClearOverridesCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ApplyCommand          as RelayCommand)?.RaiseCanExecuteChanged();
        }

        // ======= UI: chọn elements theo Category (không thay đổi đồ hoạ) =======
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
                foreach (var item in selectedCategories)
                {
                    var elements = new FilteredElementCollector(_doc)
                        .OfCategoryId(item.CategoryId)
                        .WhereElementIsNotElementType()
                        .ToElements();

                    foreach (var elem in elements)
                        if (elem.Id != ElementId.InvalidElementId) elementIds.Add(elem.Id);
                }

                if (elementIds.Count > 10000)
                {
                    TaskDialog.Show("Cảnh báo", $"Quá nhiều phần tử ({elementIds.Count}), chỉ chọn 10,000 đầu.");
                    elementIds = elementIds.Take(10000).ToList();
                }

                _uiApp.ActiveUIDocument.Selection.SetElementIds(elementIds);
                TaskDialog.Show("Thành công", $"Đã chọn {elementIds.Count} phần tử.");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Lỗi", "Không thể chọn phần tử: " + ex.Message);
            }
        }

        // ===== Search =====
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set { if (_searchText != value) { _searchText = value; OnPropertyChanged(); ExecuteSearchCommand(null); } }
        }

        private RelayCommand _searchCommand;
        public ICommand SearchCommand => _searchCommand ??= new RelayCommand(ExecuteSearchCommand);

        private void ExecuteSearchCommand(object _)
        {
            IEnumerable<CategoryItem> source = _allCategories;
            if (!string.IsNullOrWhiteSpace(SearchText))
                source = _allCategories.Where(x => x.Display.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            CategoryView = new ObservableCollection<CategoryItem>(source.OrderBy(x => x.Display));
            OnPropertyChanged(nameof(CategoryView));
            RehookPropertyEvents();
            UpdateSelectAllState();
            RaiseCommandsCanExecuteChanged();
        }

        // ===== Color options =====
        private System.Windows.Media.Color _selectedColor = Colors.Yellow;
        public System.Windows.Media.Color SelectedColor { get => _selectedColor; set { if (_selectedColor != value) { _selectedColor = value; OnPropertyChanged(); } } }

        // 0..90 theo UI (Revit 0..100)
        private int _selectedTransparency = 0;
        public int SelectedTransparency { get => _selectedTransparency; set { if (_selectedTransparency != value) { _selectedTransparency = Math.Max(0, Math.Min(90, value)); OnPropertyChanged(); } } }

        private bool _isHalftone;
        public bool IsHalftone { get => _isHalftone; set { if (_isHalftone != value) { _isHalftone = value; OnPropertyChanged(); } } }

        public void SetColor(string colorName)
        {
            var colorObj = ColorConverter.ConvertFromString(colorName);
            if (colorObj is System.Windows.Media.Color wpfColor) SelectedColor = wpfColor;
            else TaskDialog.Show("Lỗi", $"Không nhận diện được màu: '{colorName}'. Hỗ trợ tên màu (Red, Blue, …) hoặc mã hex (#FF0000).");
        }

        // ===== ExternalEvent: APPLY BY ELEMENT (API) =====
        private void ApplyElementColor_ByElement_API(UIApplication app)
        {
            var cats = CategoryView.Where(x => x.IsChecked).ToList();
            if (!cats.Any())
            {
                TaskDialog.Show("Notification", "Please select at least one category.");
                return;
            }

            var wpfColor     = SelectedColor;
            var revitColor   = new Autodesk.Revit.DB.Color(wpfColor.R, wpfColor.G, wpfColor.B);
            var transparency = SelectedTransparency;
            var halftone     = IsHalftone;

            var fill = FindSolidFill(_doc);
            if (fill == null) { TaskDialog.Show("Error", "Could not find the 'Solid fill' pattern."); return; }

            int applied = 0, skipped = 0;

            try
            {
                using (var t = new Transaction(_doc, "Color Fill By Element"))
                {
                    t.Start();

                    var ogs = new OverrideGraphicSettings();
                    ogs.SetSurfaceForegroundPatternId(fill.Id);
                    ogs.SetSurfaceForegroundPatternColor(revitColor);
                    ogs.SetSurfaceBackgroundPatternColor(revitColor);
                    ogs.SetSurfaceTransparency(transparency);
                    if (halftone) ogs.SetHalftone(true);

                    foreach (var cat in cats)
                    {
                        var elemIds = new FilteredElementCollector(_doc, _view.Id)
                            .OfCategoryId(cat.CategoryId)
                            .WhereElementIsNotElementType()
                            .ToElementIds();

                        foreach (var id in elemIds)
                        {
                            try { _view.SetElementOverrides(id, ogs); applied++; }
                            catch { skipped++; } // nếu không thỏa thì bỏ qua
                        }
                    }

                    t.Commit();
                }

                // cập nhật bảng sau khi API xong (UI thread)
                RunOnUI(() => UpsertProcessingRows(cats,   wpfColor));

                TaskDialog.Show("Result", $"Colored {applied} elements.\nSkipped {skipped} elements that could not be overridden.");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", "Failed to apply element color: " + ex.Message);
            }
        }

        // ===== ExternalEvent: CLEAR BY ELEMENT (API) =====
        private void ClearOverrides_ByElement_API(UIApplication app)
        {
            var cats = CategoryView.Where(x => x.IsChecked).ToList();
            if (!cats.Any())
            {
                TaskDialog.Show("Notification", "Please select at least one category to clear overrides (By Element).");
                return;
            }

            var selectedInView = _uiDoc?.Selection?.GetElementIds();
            bool clearOnlySelection = selectedInView != null && selectedInView.Count > 0;

            int cleared = 0, skipped = 0, failed = 0;

            try
            {
                using (var t = new Transaction(_doc, "Clear element overrides (By Element)"))
                {
                    t.Start();

                    if (clearOnlySelection)
                    {
                        foreach (var id in selectedInView)
                        {
                            try
                            {
                                var e = _doc.GetElement(id);
                                if (e?.Category?.Id == null) { skipped++; continue; }
                                if (!cats.Any(c => c.CategoryId == e.Category.Id)) { skipped++; continue; }

                                try { _view.SetElementOverrides(id, new OverrideGraphicSettings()); cleared++; }
                                catch { skipped++; }
                            }
                            catch { failed++; }
                        }
                    }
                    else
                    {
                        foreach (var cat in cats)
                        {
                            var elemIds = new FilteredElementCollector(_doc, _view.Id)
                                .OfCategoryId(cat.CategoryId)
                                .WhereElementIsNotElementType()
                                .ToElementIds();

                            foreach (var id in elemIds)
                            {
                                try { _view.SetElementOverrides(id, new OverrideGraphicSettings()); cleared++; }
                                catch { skipped++; }
                            }
                        }
                    }

                    t.Commit();
                }

                try { _view.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate); } catch { }

                // cập nhật bảng (UI thread)
                RunOnUI(() =>
                {
                    foreach (var row in ProcessingRows)
                    {
                        if (cats.Any(c => c.CategoryId == row.CategoryId))
                        {
                            row.Amount = CountElementsInViewByCategory(row.CategoryId);
                            if (row.IsIsolated) row.IsIsolated = false;
                        }
                    }
                });

                TaskDialog.Show("Result", $"Reset overrides for {cleared} elements.\nSkipped: {skipped}\nOther failures: {failed}");
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", "Could not clear overrides (By Element): " + ex.Message);
            }
        }

        // ===== Helpers =====
        private FillPatternElement FindSolidFill(Document doc)
        {
            var names = new[] { "Solid fill", "<Solid fill>" };
            foreach (var n in names)
            {
                var e = FillPatternElement.GetFillPatternElementByName(doc, FillPatternTarget.Drafting, n)
                        ?? FillPatternElement.GetFillPatternElementByName(doc, FillPatternTarget.Model, n);
                if (e != null) return e;
            }
            return new FilteredElementCollector(doc)
                .OfClass(typeof(FillPatternElement))
                .Cast<FillPatternElement>()
                .FirstOrDefault(fp => fp?.GetFillPattern()?.IsSolidFill == true);
        }

        private int CountElementsInViewByCategory(ElementId catId)
        {
            try
            {
                return new FilteredElementCollector(_doc, _view.Id)
                    .OfCategoryId(catId)
                    .WhereElementIsNotElementType()
                    .GetElementCount();
            }
            catch { return 0; }
        }

        private void UpsertProcessingRows(IEnumerable<CategoryItem> coloredCats, System.Windows.Media.Color wpfColor)
        {
            foreach (var cat in coloredCats)
            {
                var amount   = CountElementsInViewByCategory(cat.CategoryId);
                var existing = ProcessingRows.FirstOrDefault(r => r.CategoryId == cat.CategoryId);

                if (existing == null)
                {
                    ProcessingRows.Add(new ProcessingRow
                    {
                        CategoryId   = cat.CategoryId,
                        CategoryName = cat.Display,
                        Amount       = amount,
                        Color        = wpfColor,
                        IsIsolated   = false
                    });
                }
                else
                {
                    existing.Amount = amount;
                    existing.Color  = wpfColor;
                }
            }

            for (int i = 0; i < ProcessingRows.Count; i++)
                ProcessingRows[i].RowIndex = i + 1;
        }

        // chạy action an toàn trên UI thread
        private void RunOnUI(Action action)
        {
            if (action == null) return;

            if (_uiDispatcher != null)
            {
                if (_uiDispatcher.CheckAccess()) action();
                else _uiDispatcher.Invoke(action);
                return;
            }

            var app = System.Windows.Application.Current;
            if (app != null)
            {
                if (app.Dispatcher.CheckAccess()) action();
                else app.Dispatcher.Invoke(action);
                return;
            }

            // fallback (hiếm)
            action();
        }

        // ===== Rehook property changed events =====
        private void RehookPropertyEvents()
        {
            foreach (var item in CategoryView)
            {
                item.PropertyChanged -= OnCategoryItemPropertyChanged;
                item.PropertyChanged += OnCategoryItemPropertyChanged;
            }
        }

        // ===== INotifyPropertyChanged =====
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

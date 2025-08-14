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
    public class Ex1SmartSelectionViewModel: INotifyPropertyChanged
    {
        private UIApplication _uiApp;
        private UIDocument _uiDoc;
        private Document _doc;
        private View _view;

        // External event handler for Revit events
        private ExternalRevitEventHandler _externalHandler;
        private ExternalEvent _externalEvent;

        public ObservableCollection<CategoryItem> CategoryView { get; set; }
        private ObservableCollection<CategoryItem> _allCategories;

        private bool? _selectAll;
        public bool? SelectAll
        {
            get => _selectAll;
            set
            {
                if (_selectAll != value)
                {
                    _selectAll = value;
                    // Khi SelectAll thay đổi, cập nhật tất cả các mục
                    if (value.HasValue)
                    {
                        foreach (var item in CategoryView)
                        {
                            item.IsChecked = value.Value;
                        }
                    }
                    // notify UI need to update
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ApplyColorCommand { get; }

        private ICommand SetColorCommand { get; }

        public ICommand ApplyCommand { get; }



        public Ex1SmartSelectionViewModel(UIApplication uiApp, UIDocument uiDoc, Document doc)
        {
            // Lưu trữ các tham chiếu đến UIApplication, UIDocument và Document
            _uiApp = uiApp;
            _uiDoc = uiDoc;
            _doc = doc;
            _view = _uiDoc.ActiveView;

            CategoryView = new ObservableCollection<CategoryItem>();

            // Bước 1: Tải danh mục
            LoadCategories();

            // Khởi tạo SelectAll với giá trị null (không chọn gì)
            _selectAll = false;


            // Apply color command
            ApplyColorCommand = new RelayCommand(
                execute: ApplyElementColor,
                canExecute: () => CategoryView.Any(c => c.IsChecked)
                );
            // Apply Set SetColorCommand
            SetColorCommand = new RelayCommand(
                execute: (param) => SetColor(param as string),
                canExecute: (param) => !string.IsNullOrWhiteSpace(param as string)
                );

            // Apply command
             ApplyCommand = new RelayCommand(
                execute: ApplyUI,
                canExecute: () => CategoryView.Any(c => c.IsChecked)
                );

            // Đăng ký sự kiện
            RehookPropertyEvents(); // ✅ Dùng hàm thống nhất
        }

        private void LoadCategories()
        {
            var items = _doc.Settings.Categories
             .Cast<Category>()
             .Where(cat => !string.IsNullOrEmpty(cat.Name) &&
                           (cat.CategoryType == CategoryType.Model ||
                            cat.CategoryType == CategoryType.Annotation) &&
                           cat.Parent == null)
             .Select(cat => new CategoryItem
             {
                 Display = cat.Name,
                 CategoryId = cat.Id,
                 IsChecked = false
             })
             .OrderBy(x => x.Display, StringComparer.OrdinalIgnoreCase)
             .ToList();

                    // Lưu bản gốc
                    _allCategories = new ObservableCollection<CategoryItem>(items);

                    // Gán cho CategoryView (ban đầu là toàn bộ)
                    CategoryView = new ObservableCollection<CategoryItem>(items);

            RehookPropertyEvents(); // ✅ Gọi ở đây
        }

        private void OnCategoryItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CategoryItem.IsChecked))
            {
                UpdateSelectAllState();
            }
        }

        private void UpdateSelectAllState()
        {
            var checkedCount = CategoryView.Count(c => c.IsChecked);
            var totalCount = CategoryView.Count;

            bool? newState;

            if (checkedCount == 0)
                newState = false;
            else if (checkedCount == totalCount)
                newState = true;
            else
                newState = null; // Indeterminate

            // Chỉ cập nhật nếu thay đổi, tránh vòng lặp
            if (_selectAll != newState)
            {
                _selectAll = newState;
                OnPropertyChanged(nameof(SelectAll));
            }
        }



        //Show elements in the selected categories
        private void ApplyUI()
        {
            var selectedCategories = CategoryView
                .Where(item => item.IsChecked)
                .ToList();

            if (!selectedCategories.Any())
            {
                TaskDialog.Show("Thông báo", "Vui lòng chọn ít nhất một danh mục.");
                return;
            }

           
                var elementIds = new List<ElementId>(); // ✅ KHAI BÁO TRONG SetAction

                try
                {
                    using (var transaction = new Transaction(_doc, "Select Elements"))
                    {
                        transaction.Start();

                        foreach (var item in selectedCategories)
                        {
                            var elements = new FilteredElementCollector(_doc)
                                .OfCategoryId(item.CategoryId)
                                .WhereElementIsNotElementType()
                                .ToElements();

                            foreach (var elem in elements)
                            {
                                if (elem.Id != ElementId.InvalidElementId)
                                {
                                    elementIds.Add(elem.Id);
                                }
                            }
                        }

                        if (elementIds.Count > 10000)
                        {
                            TaskDialog.Show("Cảnh báo", $"Quá nhiều phần tử ({elementIds.Count}), chỉ chọn 10,000 đầu.");
                            elementIds = elementIds.Take(10000).ToList();
                        }

                    _uiApp.ActiveUIDocument.Selection.SetElementIds(elementIds);
                        transaction.Commit();

                        TaskDialog.Show("Thành công", $"Đã chọn {elementIds.Count} phần tử.");
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Lỗi", "Không thể chọn phần tử: " + ex.Message);
                }
        }

        //Search
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
                    // ✅ GỌI LỆNH TÌM KIẾM NGAY KHI GÕ
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
            //update category view after search
            OnPropertyChanged(nameof(CategoryView));
            RehookPropertyEvents();
        }

        // ===================================================
        // IMPLEMENTATION FOR COLOR SELECTION
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

        public void SetColor(string colorName)
        {
            {
                // Dùng WPF ColorConverter để parse tên màu hoặc mã hex
                var color = ColorConverter.ConvertFromString(colorName);
                if (color is System.Windows.Media.Color wpfColor)
                {
                    SelectedColor = wpfColor; // ← LẤY THẲNG, KHÔNG CẦN CHUYỂN ĐỔI PHỨC TẠP
                }
                else
                {
                    TaskDialog.Show("Lỗi", $"Không thể nhận diện màu: '{colorName}'\n" +
                                          "Hỗ trợ: tên màu (Red, Blue) hoặc mã hex (#FF0000)");
                }
            }
        }

        // This method applies the selected color to the checked categories
        public void  ApplyElementColor()
        {
            var selectedCategories = CategoryView.Where(x => x.IsChecked).ToList();

            if (!selectedCategories.Any())
            {
                TaskDialog.Show("Vui lòng chọn ít nhất một danh mục.", "Thông báo");
                return;
            }

            // Chuẩn bị dữ liệu cho ExternalEvent
            var revitColor = new Autodesk.Revit.DB.Color(SelectedColor.R, SelectedColor.G, SelectedColor.B);
            var catIds = selectedCategories.Select(c => c.CategoryId).ToList();

            if (catIds.Count == 0)
            {
                TaskDialog.Show("Cảnh báo", "Không có danh mục nào để tô màu.");
                return;
            }

            FillPatternElement fillPatternElement = PatternElementUtils.FindFillPatternByName(_doc, "<Solid fill>");
           
            if (fillPatternElement == null)
            {
                TaskDialog.Show("Lỗi", "Không tìm thấy mẫu tô đặc 'Solid fill'.");
                return;
            }

                try
                {
                    using (var t = new Transaction(_doc, "Tô màu danh mục"))
                    {
                        t.Start();
                        var ogs = new OverrideGraphicSettings();
                        if (fillPatternElement != null)
                        {
                            ogs.SetSurfaceForegroundPatternId(fillPatternElement.Id);
                            ogs.SetSurfaceForegroundPatternColor(revitColor);
                        }
                        ogs.SetSurfaceBackgroundPatternColor(revitColor);

                        foreach (var catId in catIds)
                        {
                            // ✅ Kiểm tra ID hợp lệ
                            if (catId == ElementId.InvalidElementId)
                            {
                                DeepBIMLog.LogError($"catId không hợp lệ: {catId}");
                                continue;
                            }

                            _view.SetCategoryOverrides(catId, ogs);
                        }
                        t.Commit();
                    }

                    //TaskDialog.Show("Thành công", $"Đã tô màu {catIds.Count} danh mục.");
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Lỗi", "Không thể tô màu danh mục: " + ex.Message);
            }
        }


        // ===================================================
        // Rehook property changed events for all items in CategoryView
        private void RehookPropertyEvents()
        {
            foreach (var item in CategoryView)
            {
                // 1. Loại bỏ đăng ký cũ (tránh trùng)
                item.PropertyChanged -= OnCategoryItemPropertyChanged;
                // 2. Đăng ký lại
                item.PropertyChanged += OnCategoryItemPropertyChanged;
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        // This method is used to notify the UI that a property has changed
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}

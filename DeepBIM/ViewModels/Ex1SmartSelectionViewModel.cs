using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DeepBIM.Events;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace DeepBIM.ViewModels
{
    public class Ex1SmartSelectionViewModel: INotifyPropertyChanged
    {
        private UIApplication _uiApp;
        private UIDocument _uiDoc;
        private Document _doc;

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

        
        public Ex1SmartSelectionViewModel(UIApplication uiApp, UIDocument uiDoc, Document doc)
        {
            // Lưu trữ các tham chiếu đến UIApplication, UIDocument và Document
            _uiApp = uiApp;
            _uiDoc = uiDoc;
            _doc = doc;

            // ✅ Khởi tạo External Event
            _externalHandler = new ExternalRevitEventHandler();
            _externalEvent = ExternalEvent.Create(_externalHandler);

            CategoryView = new ObservableCollection<CategoryItem>();

            // Bước 1: Tải danh mục
            LoadCategories();

            // Khởi tạo SelectAll với giá trị null (không chọn gì)
            _selectAll = false;

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
        private RelayCommand _applyCommand;
        public ICommand ApplyCommand => _applyCommand ??= new RelayCommand(ExecuteApplyCommand);

        private void ExecuteApplyCommand(object parameter)
        {
            var selectedCategories = CategoryView
                .Where(item => item.IsChecked)
                .ToList();

            if (!selectedCategories.Any())
            {
                TaskDialog.Show("Thông báo", "Vui lòng chọn ít nhất một danh mục.");
                return;
            }

            // ✅ GỬI selectedCategories, KHÔNG dùng biến bên ngoài
            _externalHandler.SetAction(app =>
            {
                var doc = app.ActiveUIDocument.Document;
                var uidoc = app.ActiveUIDocument;
                var elementIds = new List<ElementId>(); // ✅ KHAI BÁO TRONG SetAction

                try
                {
                    using (var transaction = new Transaction(doc, "Select Elements"))
                    {
                        transaction.Start();

                        foreach (var item in selectedCategories)
                        {
                            var elements = new FilteredElementCollector(doc)
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

                        uidoc.Selection.SetElementIds(elementIds);
                        transaction.Commit();

                        TaskDialog.Show("Thành công", $"Đã chọn {elementIds.Count} phần tử.");
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Lỗi", "Không thể chọn phần tử: " + ex.Message);
                }
            });

            _externalEvent.Raise();
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

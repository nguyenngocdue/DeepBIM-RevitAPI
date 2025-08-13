using Autodesk.Revit.UI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DeepBIM.ViewModels
{
    public class Ex1SmartSelectionViewModel: INotifyPropertyChanged
    {
        private UIApplication _uiApp;
        private UIDocument _uiDoc;
        private Document _doc;

        public ObservableCollection<CategoryItem> CategoryView { get; set; }

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
            _uiApp = uiApp;
            _uiDoc = uiDoc;
            _doc = doc;
            CategoryView = new ObservableCollection<CategoryItem>();

            // Bước 1: Tải danh mục
            LoadCategories();

            // Khởi tạo SelectAll với giá trị null (không chọn gì)
            _selectAll = false;

            // Bước 2: Đăng ký sự kiện PropertyChanged cho từng item
            foreach (var item in CategoryView)
            {
                item.PropertyChanged += OnCategoryItemPropertyChanged;
            }

        }

        private void LoadCategories()
        {
            CategoryView.Clear();

            Categories allCategories = _doc.Settings.Categories;
            foreach (Category category in allCategories)
            {
                if (!string.IsNullOrEmpty(category.Name))
                {
                    var item = new CategoryItem
                    {
                        Display = category.Name,
                        IsChecked = false
                    };
                    CategoryView.Add(item);
                }
            }

            OnPropertyChanged(nameof(CategoryView));
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

        public event PropertyChangedEventHandler PropertyChanged;
        // This method is used to notify the UI that a property has changed
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace DeepBIM.ViewModels
{
    public class SmartSelectionViewModel : INotifyPropertyChanged
    {
        private readonly UIDocument _uiDoc;
        private readonly Document _doc;

        private string _searchText;
        private int _selectedElementsCount;
        private double _selectionProgressValue;
        private bool _isProcessing;
        private bool _selectAll;

        private ObservableCollection<CategoryItem> _allCategories = new ObservableCollection<CategoryItem>();
        private System.Timers.Timer _debounceTimer;

        // View để hỗ trợ lọc
        public ICollectionView CategoryView { get; private set; }

        // Properties
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                CategoryView.Refresh(); // Kích hoạt lọc
                // Reset & start timer
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }
        }

        public bool SelectAll
        {
            get => _selectAll;
            set
            {
                _selectAll = value;
                OnPropertyChanged(nameof(SelectAll));
                // Cập nhật trạng thái IsChecked cho tất cả mục
                foreach (var cat in _allCategories)
                {
                    cat.IsChecked = value;
                }
                RefreshPreview();
            }
        }

        public int SelectedElementsCount
        {
            get => _selectedElementsCount;
            set
            {
                _selectedElementsCount = value;
                OnPropertyChanged(nameof(SelectedElementsCount));
            }
        }

        public double SelectionProgressValue
        {
            get => _selectionProgressValue;
            set
            {
                _selectionProgressValue = value;
                OnPropertyChanged(nameof(SelectionProgressValue));
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                OnPropertyChanged(nameof(IsProcessing));
            }
        }

        public ICommand SearchCommand { get; private set; }

        public enum SelectionScopeType
        {
            CurrentView,
            EntireProject,
            SelectedViews
        }

        private SelectionScopeType _selectionScope = SelectionScopeType.CurrentView;

        public SelectionScopeType SelectionScope
        {
            get => _selectionScope;
            set
            {
                _selectionScope = value;
                OnPropertyChanged(nameof(SelectionScope));
                UpdateCategoriesBasedOnScope(); // ← Gọi khi thay đổi
            }
        }

        private void UpdateCategoriesBasedOnScope()
        {
            ObservableCollection<CategoryItem> newCategories;

            switch (SelectionScope)
            {
                case SelectionScopeType.CurrentView:
                    newCategories = GetCategoriesInCurrentView();
                    break;

                case SelectionScopeType.EntireProject:
                    newCategories = GetAllCategoriesInProject();
                    break;

                case SelectionScopeType.SelectedViews:
                    newCategories = GetCategoriesInSelectedViews(); // Tạm thời như trên
                    break;

                default:
                    newCategories = new ObservableCollection<CategoryItem>();
                    break;
            }

            // Xóa danh sách cũ
            _allCategories.Clear();

            // Thêm mới
            foreach (var cat in newCategories)
            {
                _allCategories.Add(cat);
            }

            // Refresh view
            CategoryView.Refresh();

            // Reset SelectAll
            SelectAll = _allCategories.All(c => c.IsChecked);

            // Cập nhật preview
            RefreshPreview();
        }

        // === Các hàm giả lập (sau sẽ thay bằng Revit API) ===

        private ObservableCollection<CategoryItem> GetCategoriesInCurrentView()
        {
            try
            {
                var view = _doc.ActiveView;
                if (view == null) return new ObservableCollection<CategoryItem>();

                // Lấy tất cả phần tử trong view hiện tại (không phải type)
                var collector = new FilteredElementCollector(_doc, view.Id)
                    .WhereElementIsNotElementType();

                var categoryNames = collector
                    .Select(e => e.Category?.Name) // e.Category có thể null
                    .Where(name => !string.IsNullOrEmpty(name)) // Lọc null/empty
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();

                return new ObservableCollection<CategoryItem>(
                    categoryNames.Select(name => new CategoryItem
                    {
                        Display = name
                    })
                );
            }
            catch
            {
                // Nếu lỗi (ví dụ view là drafting, không có element), trả về rỗng
                return new ObservableCollection<CategoryItem>();
            }
        }


        private ObservableCollection<CategoryItem> GetAllCategoriesInProject()
        {
            try
            {
                var collector = new FilteredElementCollector(_doc)
                    .WhereElementIsNotElementType();

                var categoryNames = collector
                    .Select(e => e.Category?.Name)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();

                return new ObservableCollection<CategoryItem>(
                    categoryNames.Select(name => new CategoryItem
                    {
                        Display = name
                    })
                );
            }
            catch
            {
                return new ObservableCollection<CategoryItem>();
            }
        }

        private ObservableCollection<CategoryItem> GetCategoriesInSelectedViews()
        {
            // Tạm thời giống Entire Project
            return GetAllCategoriesInProject();
        }


        public SmartSelectionViewModel(Document doc, UIDocument uiDoc)
        {
            _uiDoc = uiDoc;
            _doc = uiDoc.Document;

            // Khởi tạo ban đầu theo scope mặc định
            SelectionScope = SelectionScopeType.CurrentView;
            UpdateCategoriesBasedOnScope();


            // Khởi tạo collection
            _allCategories = new ObservableCollection<CategoryItem>();

            // Khởi tạo view
            CategoryView = CollectionViewSource.GetDefaultView(_allCategories);
            CategoryView.Filter = FilterCategories;


            // Khởi tạo scope
            SelectionScope = SelectionScopeType.CurrentView;

            // Tạo CollectionView để hỗ trợ lọc
            CategoryView = CollectionViewSource.GetDefaultView(_allCategories);
            CategoryView.Filter = FilterCategories;

            // Khởi tạo giá trị
            SearchText = "";
            SelectedElementsCount = 0;
            SelectionProgressValue = 0;
            IsProcessing = false;

            // Cập nhật SelectAll khi thay đổi IsChecked trong danh sách
            foreach (var cat in _allCategories)
            {
            }


            SearchCommand = new RelayCommand<string>(text => { /* Search handled by binding */ });
        }

        // Lọc danh mục theo tên hiển thị
        private bool FilterCategories(object item)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            if (item is CategoryItem cat)
            {
                return cat.Display.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        // Khi một CategoryItem thay đổi IsChecked
        private void OnCategoryItemChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CategoryItem.IsChecked))
            {
                // Kiểm tra xem tất cả có được chọn không
                var allChecked = _allCategories.All(c => c.IsChecked);
                if (allChecked != _selectAll)
                {
                    _selectAll = allChecked;
                    OnPropertyChanged(nameof(SelectAll));
                }
                RefreshPreview();
            }
        }

        // Gọi hàm này để cập nhật preview (gắn vào ICommand sau)
        public void RefreshPreview()
        {
            IsProcessing = true;
            SelectionProgressValue = 0;

            // Mô phỏng xử lý (thay bằng logic Revit thật)
            System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = 0; i <= 100; i++)
                {
                    System.Threading.Thread.Sleep(10);
                    SelectionProgressValue = i;
                }

                // Đếm số lượng danh mục đang được chọn
                var selectedCount = _allCategories.Count(c => c.IsChecked);
                // Giả lập số phần tử (thay bằng đếm thật từ Revit)
                SelectedElementsCount = selectedCount * 15; // Ví dụ: mỗi danh mục ~15 phần tử

                IsProcessing = false;
            });
        }

        // --- MVVM: ICommand (nếu dùng) ---
        // Ví dụ: RefreshCommand
        /*
        public ICommand RefreshCommand => new RelayCommand(RefreshPreview);
        */
        // (Bạn có thể dùng RelayCommand từ MVVM Toolkit hoặc tự viết)

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
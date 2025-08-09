using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DeepBIM.ViewModels
{

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

        public bool SelectAll
        {
            get => _selectAll;
            set { _selectAll = value; foreach (var s in Sheets) s.IsChecked = value; OnChanged(nameof(SelectAll)); }
        }
        bool _selectAll;

        public SheetManagerViewModel(Document doc)
        {
            var sheets = new FilteredElementCollector(doc).OfClass(typeof(ViewSheet))
                           .Cast<ViewSheet>()
                           .Where(v => !v.IsPlaceholder)
                           .OrderBy(v => v.SheetNumber, StringComparer.OrdinalIgnoreCase);

            foreach (var s in sheets)
                Sheets.Add(new SheetItem { Id = s.Id, Number = s.SheetNumber, Name = s.Name });

            SheetsView = CollectionViewSource.GetDefaultView(Sheets);
        }

        void OnChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        public event PropertyChangedEventHandler PropertyChanged;
    }
}

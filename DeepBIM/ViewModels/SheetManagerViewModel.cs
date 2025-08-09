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
    public class SheetRow : INotifyPropertyChanged
    {
        public bool IsChecked { get; set; }
        public string Id { get; set; }           // Revit ElementId string
        public string CurrentNumber { get; set; }
        public string CurrentName { get; set; }

        string _newNumber;
        public string NewNumber { get => _newNumber; set { _newNumber = value; OnPropertyChanged(nameof(NewNumber)); } }

        string _newName;
        public string NewName { get => _newName; set { _newName = value; OnPropertyChanged(nameof(NewName)); } }

        public string Display => $"{CurrentNumber} - {CurrentName}";
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class SheetManagerViewModel: BaseViewModel  // BaseViewModel chứa INotifyPropertyChanged + RelayCommand của bạn
    {
        public ObservableCollection<SheetRow> Rows { get; } = new();
        public ListCollectionView SheetsView { get; }
        public IList SelectedSheets { get; set; }

        // Search & Select all
        public string SearchText { get; set; }
        bool _selectAll;
        public bool SelectAll
        {
            get => _selectAll;
            set { _selectAll = value; OnPropertyChanged(); foreach (var r in Rows) r.IsChecked = value; }
        }

        // Bottom configs
        public bool ScopeAll { get; set; } = true;
        public bool ScopeSelection { get; set; }

        public string SortPrefix { get; set; } = "";
        public int SortStart { get; set; } = 1;

        public string AddPrefix { get; set; } = "";
        public string AddSuffix { get; set; } = "";
        public string FindValue { get; set; } = "";
        public string ReplaceValue { get; set; } = "";

        public bool RuleTargetIsNumber { get; set; } = true;
        public bool RuleTargetIsName { get => !RuleTargetIsNumber; set => RuleTargetIsNumber = !value; }

        // Commands
        public RelayCommand SearchCommand { get; }
        public RelayCommand CopyCurrentToNewCommand { get; }
        public RelayCommand MoveUpCommand { get; }
        public RelayCommand MoveDownCommand { get; }
        public RelayCommand ClearNewValuesCommand { get; }
        public RelayCommand RefreshCommand { get; }

        public RelayCommand SortCommand { get; }
        public RelayCommand ApplyRulesCommand { get; }
        public RelayCommand RenameSheetsCommand { get; }

        public SheetManagerViewModel()
        {
            // CollectionView to reuse left list with filtering
            SheetsView = (ListCollectionView)CollectionViewSource.GetDefaultView(Rows);

            SearchCommand = new RelayCommand(_ => ApplyFilter());
            CopyCurrentToNewCommand = new RelayCommand(_ => {
                foreach (var r in Checked()) { r.NewNumber = r.CurrentNumber; r.NewName = r.CurrentName; }
            });
            MoveUpCommand = new RelayCommand(_ => Move(-1), _ => CanMove(-1));
            MoveDownCommand = new RelayCommand(_ => Move(1), _ => CanMove(1));
            ClearNewValuesCommand = new RelayCommand(_ => { foreach (var r in Checked()) { r.NewNumber = null; r.NewName = null; } });
            RefreshCommand = new RelayCommand(_ => ReloadFromRevit());

            SortCommand = new RelayCommand(_ => ApplySort());
            ApplyRulesCommand = new RelayCommand(_ => ApplyRules());
            RenameSheetsCommand = new RelayCommand(_ => DoRenameInRevit());
        }

        void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(SearchText)) SheetsView.Filter = null;
            else
                SheetsView.Filter = o =>
                {
                    var r = (SheetRow)o;
                    var s = SearchText.ToLowerInvariant();
                    return (r.CurrentNumber?.ToLowerInvariant().Contains(s) == true)
                        || (r.CurrentName?.ToLowerInvariant().Contains(s) == true);
                };
            SheetsView.Refresh();
        }

        IEnumerable<SheetRow> Checked() => Rows.Where(r => r.IsChecked);

        bool CanMove(int dir) => SelectedSheets != null && SelectedSheets.Count > 0;
        void Move(int dir)
        {
            // simple reorder by selected indexes
            var sel = SelectedSheets.Cast<SheetRow>().ToList();
            if (dir < 0)
                for (int i = 0; i < Rows.Count; i++)
                    if (sel.Contains(Rows[i]) && i > 0) Rows.Move(i, i - 1);
                    else
                        for (int i = Rows.Count - 1; i >= 0; i--)
                            if (sel.Contains(Rows[i]) && i < Rows.Count - 1) Rows.Move(i, i + 1);
        }

        void ApplySort()
        {
            int n = SortStart;
            foreach (var r in Checked())
                r.NewNumber = $"{SortPrefix}{n++:D3}";
        }

        void ApplyRules()
        {
            foreach (var r in Checked())
            {
                if (RuleTargetIsNumber)
                {
                    var val = r.NewNumber ?? r.CurrentNumber ?? "";
                    val = $"{AddPrefix}{val}{AddSuffix}";
                    if (!string.IsNullOrEmpty(FindValue))
                        val = val.Replace(FindValue, ReplaceValue ?? "");
                    r.NewNumber = val;
                }
                else
                {
                    var val = r.NewName ?? r.CurrentName ?? "";
                    val = $"{AddPrefix}{val}{AddSuffix}";
                    if (!string.IsNullOrEmpty(FindValue))
                        val = val.Replace(FindValue, ReplaceValue ?? "");
                    r.NewName = val;
                }
            }
        }

        void ReloadFromRevit()
        {
            // TODO: đọc lại danh sách từ Revit Document và fill Rows
        }

        void DoRenameInRevit()
        {
            // TODO: mở Transaction, set Parameter BuilInSheetNumber / Name cho từng Row có New*
        }
    }
}

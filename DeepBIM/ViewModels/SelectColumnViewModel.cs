using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using DeepBIM.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DeepBIM.ViewModels
{
    public class SelectColumnViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ColumnModel> SelectedColumns { get; set; } = new();

        public void LoadSelectedColumns(IEnumerable<Element> elements)
        {
            SelectedColumns.Clear();

            foreach (var elem in elements)
            {
                SelectedColumns.Add(new ColumnModel
                {
                    Id = elem.Id,
                    Name = elem.Name
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    // Chỉ cho chọn cột kết cấu
    public class ColumnSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem.Category?.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralColumns;
        }

        public bool AllowReference(Reference reference, XYZ position) => true;
    }
}

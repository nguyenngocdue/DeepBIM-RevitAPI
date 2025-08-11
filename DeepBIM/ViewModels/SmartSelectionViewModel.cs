using Autodesk.Revit.UI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;

namespace DeepBIM.ViewModels
{


    public class SmartSelectionViewModel : INotifyPropertyChanged
    {
        public SmartSelectionViewModel(Document doc, UIDocument uidoc, ICollection<ElementId> selectedIds = null)
        {
        }

        event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }
    }
       
}

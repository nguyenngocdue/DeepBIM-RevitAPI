using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace DeepBIM.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class TagElementsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;

                // ✅ Lấy danh sách đã chọn
                var selectedIds = uidoc.Selection.GetElementIds();

                var win = new DeepBIM.Views.TagElementsWindow(uiapp, doc);
                var vm = new DeepBIM.ViewModels.TagElementsViewModel(doc, uidoc, selectedIds);
                vm.RequestClose += () => { if (win.IsVisible) win.Close(); };
                win.DataContext = vm;

                new System.Windows.Interop.WindowInteropHelper(win) { Owner = uiapp.MainWindowHandle };
                win.Show(); 
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}

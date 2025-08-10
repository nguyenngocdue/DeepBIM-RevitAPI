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
    public class SheetCreationCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;


                // Khởi tạo Window + ViewModel
                var win = new DeepBIM.Views.SheetCreatetionWindow(uidoc); // Pass 'uiapp' as required by the constructor
                win.DataContext = new DeepBIM.ViewModels.SheetCreationViewModel();

                // Đặt owner là Revit để form modal và luôn trên cùng
                new WindowInteropHelper(win) { Owner = uiapp.MainWindowHandle };

                win.ShowDialog();   // hoặc .Show() nếu muốn non‑modal
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

using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using DeepBIM.Views;
using System.Windows.Interop;

namespace DeepBIM.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class CommandLineRevitCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;

                //// ✅ Lấy danh sách đã chọn
                //var selectedIds = uidoc.Selection.GetElementIds();

                //// Khởi tạo Window + ViewModel
                //var win = new DeepBIM.Views.SheetManagerWindow(uiapp, doc); // Pass 'uiapp' as required by the constructor
                //win.DataContext = new DeepBIM.ViewModels.SheetManagerViewModel(doc, uidoc, selectedIds);

                //// Đặt owner là Revit để form modal và luôn trên cùng
                //new WindowInteropHelper(win) { Owner = uiapp.MainWindowHandle };

                //win.ShowDialog();   // hoặc .Show() nếu muốn non‑modal
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

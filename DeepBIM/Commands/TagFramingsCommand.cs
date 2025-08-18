using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using System;
using System.Windows.Interop;

namespace DeepBIM.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class TagFramingsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Khởi tạo Window + ViewModel
                var win = new DeepBIM.Views.WelcomeDueWindow(uiapp, uidoc, doc); 
                win.DataContext = new DeepBIM.ViewModels.WelcomeDueViewModel(uiapp, uidoc, doc);

                new WindowInteropHelper(win) { Owner = uiapp.MainWindowHandle };
                win.ShowDialog();
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

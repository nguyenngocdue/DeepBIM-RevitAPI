using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;

namespace DeepBIM.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class Ex1SmartSelectionCommand: IExternalCommand
    {
        public Result Execute(ExternalCommandData commanData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiapp = commanData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;


                //initialize the window and view model
                var win = new DeepBIM.Views.Ex1SmartSelectionWindow(uiapp, uidoc, doc);

                // Set the DataContext to the ViewModel
                win.DataContext = new DeepBIM.ViewModels.Ex1SmartSelectionViewModel(uiapp, uidoc, doc);
                
                // Show the window as a dialog
                //win.ShowDialog(); // or .Show() if you want it non-modal
                win.Show(); // or .Show() if you want it non-modal
                win.Activate();

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

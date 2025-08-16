using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using DeepBIM.Views;

namespace DeepBIM.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class SettingsCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                // WPF cần chạy trên STA thread
                var window = new SettingsWindow();
                window.ShowDialog(); // Dùng ShowDialog để modal
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}

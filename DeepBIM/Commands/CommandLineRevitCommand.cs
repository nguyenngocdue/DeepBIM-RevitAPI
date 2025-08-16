using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using DeepBIM.Services;
using DeepBIM.Views;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;



namespace DeepBIM.Commands
{
    [Transaction(TransactionMode.Manual)]
    //[Regeneration(RegenerationOption.Manual)]
    public class CommandLineRevitCommand : IExternalCommand
    {
        
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                var uiapp = commandData.Application;

                // Đăng ký 1 lần rồi gỡ
                EventHandler<IdlingEventArgs> handler = null;
                handler = (s, e) =>
                {
                    uiapp.Idling -= handler;              // gỡ ngay tránh lặp
                    DeepBIM.Services.RibbonCommandCatalog.RefreshOnce(); // quét bằng reflection
                    var win = new DeepBIM.Views.CommandLineRevitWindow(uiapp, DeepBIM.Services.RibbonCommandCatalog.Items);
                    win.Show(); // non-modal để không block Idling
                };
                uiapp.Idling += handler;
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

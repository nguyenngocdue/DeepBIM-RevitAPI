using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepBIM.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class SelectColumnCommand : IExternalCommand
    {
        public Result Execute (ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication app = commandData.Application;
                var window = new Views.SelectColumnWindow(app);
                window.Show();
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

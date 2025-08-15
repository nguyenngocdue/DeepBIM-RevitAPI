using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DeepBIM.Helpers;

namespace DeepBIM.Commands
{
    public abstract class AlignBaseCommand : IExternalCommand
    {
        protected abstract AlignType Type { get; }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiDoc = commandData.Application.ActiveUIDocument;
            var doc = uiDoc.Document;

            var service = new AlignService();
            return service.AlignElements(uiDoc, doc, Type);
        }
    }
}

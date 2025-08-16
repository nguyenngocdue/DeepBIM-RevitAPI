using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace DeepBIM.Helpers
{
    internal class OrientableSelectionFilter : ISelectionFilter
    {
        private readonly View _view;
        public OrientableSelectionFilter(View v) => _view = v;

        public bool AllowElement(Element e) => OrientationUtils.TryGetAngle(e, _view, out _);
        public bool AllowReference(Reference r, XYZ p) => true;
    }
}

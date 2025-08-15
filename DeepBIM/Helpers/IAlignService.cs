using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace DeepBIM.Helpers
{
    public enum AlignType
    {
        Left,
        Right,
        Bottom,
        Top,
        CenterX,
        CenterY,
        DistributeHorizontally,
        DistributeVertically,
        UntangleHorizontally, 
        UntangleVertically    
    }

    public interface IAlignService
    {
        Result AlignElements(UIDocument uiDoc, Document doc, AlignType alignType, double? minGap = null);
    }
}

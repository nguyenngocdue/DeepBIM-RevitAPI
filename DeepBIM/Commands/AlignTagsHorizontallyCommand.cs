using Autodesk.Revit.Attributes;
using DeepBIM.Helpers;

namespace DeepBIM.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class AlignTagsHorizontallyCommand : AlignBaseCommand
    {
        protected override AlignType Type => AlignType.AlignTagsHorizontally;
    }
}

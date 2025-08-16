using Autodesk.Revit.Attributes;
using DeepBIM.Helpers;

namespace DeepBIM.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class UntangleHorizontallyCommand : AlignBaseCommand
    {
        protected override AlignType Type => AlignType.UntangleHorizontally;
    }
}

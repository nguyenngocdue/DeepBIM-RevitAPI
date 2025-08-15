using Autodesk.Revit.Attributes;
using DeepBIM.Helpers;

namespace DeepBIM.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class AlignRightCommand : AlignBaseCommand
    {
        protected override AlignType Type => AlignType.Right;
    }
}

using Autodesk.Revit.Attributes;
using DeepBIM.Helpers;

namespace DeepBIM.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class AlignTopCommand : AlignBaseCommand
    {
        protected override AlignType Type => AlignType.Top;
    }
}

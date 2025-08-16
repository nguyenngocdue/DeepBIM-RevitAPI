using Autodesk.Revit.Attributes;
using DeepBIM.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepBIM.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class AlignCenterXCommand : AlignBaseCommand
    {
        protected override AlignType Type => AlignType.CenterX;
    }
}

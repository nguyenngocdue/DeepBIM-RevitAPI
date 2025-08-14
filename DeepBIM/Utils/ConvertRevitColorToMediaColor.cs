using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepBIM.Utils
{
    public class ConvertRevitColorToMediaColor
    {
        public static Autodesk.Revit.DB.Color ConvertMediaColorToRevitColor(System.Windows.Media.Color mediaColor)
        {
            return new Autodesk.Revit.DB.Color(mediaColor.R, mediaColor.G, mediaColor.B);
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepBIM.Utils
{
    public class ConvertRevitColorToMediaColor
    {
        /// <summary>
        /// Converts a Revit color to a Media color.
        /// </summary>
        /// <param name="revitColor">The Revit color to convert.</param>
        /// <returns>A Media color equivalent to the Revit color.</returns>
        public static System.Windows.Media.Color Convert(Autodesk.Revit.DB.Color revitColor)
        {
            return System.Windows.Media.Color.FromArgb(
                255, // Assuming full opacity as Revit's Color does not have an Alpha channel
                revitColor.Red, // Corrected property name
                revitColor.Green, // Corrected property name
                revitColor.Blue // Corrected property name
            );
        }
    }
}

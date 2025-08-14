using System;
using System.Linq;
using Autodesk.Revit.DB;

namespace DeepBIM.Utils
{
    /// <summary>
    /// Utility class for finding FillPatternElement by exact name in Revit.
    /// </summary>
    public static class PatternElementUtils
    {
        /// <summary>
        /// Finds a FillPatternElement with the exact name (case-insensitive) and of type Model.
        /// </summary>
        /// <param name="doc">The Revit document.</param>
        /// <param name="typeName">The exact name of the fill pattern to find (e.g., "Solid fill").</param>
        /// <returns>The FillPatternElement if found; otherwise, null.</returns>
        public static FillPatternElement? FindFillPatternByName(Document doc, string typeName)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (string.IsNullOrWhiteSpace(typeName)) return null;

            var fillPatternElement = new FilteredElementCollector(doc)
                .OfClass(typeof(FillPatternElement))
                .Cast<FillPatternElement>()
                .FirstOrDefault(x =>
                {
                    var pattern = x.GetFillPattern();
                    return pattern != null
                        && string.Equals(pattern.Name?.Trim(), typeName.Trim(), StringComparison.OrdinalIgnoreCase);
                });

            return fillPatternElement;
        }
    }
}
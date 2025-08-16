using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;            // ✅ THÊM
// Không cần Autodesk.Revit.UI ở đây

namespace DeepBIM.Helpers
{
    public static class AlignTags
    {
        /// <summary>
        /// vertical = true  → cố định theo RightDirection (X của view) ⇒ thẳng hàng dọc
        /// vertical = false → cố định theo UpDirection    (Y của view) ⇒ thẳng hàng ngang
        /// </summary>
        public static void AlignTagElements(List<IndependentTag> tags, View view, bool vertical)
        {
            if (tags == null || tags.Count < 2) return;

            XYZ axis = vertical
                ? (view.RightDirection.Normalize() ?? XYZ.BasisX)
                : (view.UpDirection.Normalize() ?? XYZ.BasisY);

            double fixedComp = tags[0].TagHeadPosition.DotProduct(axis);

            foreach (var tag in tags.Skip(1))
            {
                XYZ p = tag.TagHeadPosition;
                double curComp = p.DotProduct(axis);
                double deltaComp = fixedComp - curComp;
                if (Math.Abs(deltaComp) < 1e-9) continue;

                XYZ delta = deltaComp * axis;

                try
                {
                    tag.TagHeadPosition = p + delta; // một số tag cho phép set trực tiếp
                }
                catch
                {
                    ElementTransformUtils.MoveElement(tag.Document, tag.Id, delta); // fallback
                }
            }
        }
    }
}

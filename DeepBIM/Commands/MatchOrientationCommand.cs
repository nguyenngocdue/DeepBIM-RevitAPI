using System;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace DeepBIM.Commands
{
    /// Chọn 1 đối tượng GỐC -> chọn các đối tượng khác -> tất cả xoay theo góc của GỐC
    [Transaction(TransactionMode.Manual)]
    public class MatchOrientationCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData data, ref string message, ElementSet elements)
        {
            var uiDoc = data.Application.ActiveUIDocument;
            var doc = uiDoc.Document;
            var view = doc.ActiveView;
            var sel = uiDoc.Selection;

            try
            {
                // 1) Pick BASE (không dùng filter để tránh bị chặn)
                var baseRef = sel.PickObject(ObjectType.Element, "Pick BASE element");
                var baseEl = doc.GetElement(baseRef);
                if (!Utils.TryGetAngle(baseEl, view, out double baseAngle))
                {
                    TaskDialog.Show("Match Orientation", $"Base ({baseEl?.GetType().Name}) has no computable orientation on this view.");
                    return Result.Cancelled;
                }

                // 2) Pick TARGETS
                var picked = sel.PickObjects(ObjectType.Element, "Pick targets to match orientation (ESC to finish)");
                var targets = picked.Select(r => doc.GetElement(r))
#if REVIT2024_OR_GREATER
                                    .GroupBy(e => e.Id.Value).Select(g => g.First())
#else
                                    .GroupBy(e => e.Id.IntegerValue).Select(g => g.First())
#endif
                                    .Where(e => e != null && e.Id != baseEl.Id)
                                    .ToList();

                if (targets.Count == 0) return Result.Cancelled;

                using (var t = new Transaction(doc, "Match Orientation"))
                {
                    t.Start();
                    foreach (var e in targets)
                        Utils.RotateTo(doc, e, view, baseAngle);
                    t.Commit();
                }

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled; // người dùng bấm ESC
            }
            catch (Exception ex)
            {
                message = ex.ToString();
                TaskDialog.Show("Match Orientation - Error", ex.ToString());
                return Result.Failed;
            }
        }

        /// Helpers gói gọn trong cùng file
        private static class Utils
        {
            const double EPS = 1e-9;

            // Tính góc (radian) của element trên mặt phẳng view (Right/Up)
            public static bool TryGetAngle(Element e, View view, out double angle)
            {
                angle = 0;
                if (e == null || view == null) return false;

                // ✅ TextNote: lấy qua LocationPoint.Rotation
                if (e is TextNote tn && tn.Location is LocationPoint lpTN)
                {
                    angle = lpTN.Rotation; // rad
                    return true;
                }

                // ✅ Family/Tag/ModelText... point-based
                if (e.Location is LocationPoint lp)
                {
                    angle = lp.Rotation; // rad
                    return true;
                }

                // ✅ Các phần tử tuyến tính (line-based, model/detail curve, dimension…)
                if (e.Location is LocationCurve lc)
                    return TryAngleFromCurve(lc.Curve, view, out angle);

                // ✅ Tường
                if (e is Wall w)
                    return TryAngleFromVector(w.Orientation, view, out angle);

                // ✅ CurveElement
                if (e is CurveElement ce)
                    return TryAngleFromCurve(ce.GeometryCurve, view, out angle);

                return false;
            }

            public static void RotateTo(Document doc, Element e, View view, double targetAngle)
            {
                if (!TryGetAngle(e, view, out var current)) return;

                double delta = Normalize(targetAngle - current);
                if (Math.Abs(delta) < EPS) return;

                XYZ origin = (e.Location as LocationPoint)?.Point ?? CenterOf(e, view);
                Line axis = Line.CreateBound(origin, origin + view.ViewDirection); // quay quanh Z của view

                ElementTransformUtils.RotateElement(doc, e.Id, axis, delta);
            }

            // ====== tính góc từ curve/vector, tâm bbox, chuẩn hoá ======
            private static bool TryAngleFromCurve(Curve c, View v, out double a)
            {
                a = 0; if (c == null) return false;
                double mid = 0.5 * (c.GetEndParameter(0) + c.GetEndParameter(1));
                XYZ tan = c.ComputeDerivatives(mid, true).BasisX.Normalize();
                return TryAngleFromVector(tan, v, out a);
            }

            private static bool TryAngleFromVector(XYZ vec, View v, out double a)
            {
                a = 0; if (vec == null) return false;
                XYZ r = v.RightDirection.Normalize(), u = v.UpDirection.Normalize();
                double x = vec.DotProduct(r), y = vec.DotProduct(u);
                if (Math.Abs(x) < EPS && Math.Abs(y) < EPS) return false;
                a = Math.Atan2(y, x); // rad
                return true;
            }

            private static XYZ CenterOf(Element e, View v)
            {
                var bb = e.get_BoundingBox(v) ?? e.get_BoundingBox(null);
                return bb != null ? (bb.Min + bb.Max) * 0.5 : XYZ.Zero;
            }

            private static double Normalize(double x)
            {
                while (x > Math.PI) x -= 2 * Math.PI;
                while (x < -Math.PI) x += 2 * Math.PI;
                return x;
            }
        }
    }
}

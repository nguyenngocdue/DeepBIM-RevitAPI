using System;
using Autodesk.Revit.DB;

namespace DeepBIM.Helpers
{
    static class OrientationUtils
    {
        const double EPS = 1e-9;

        public static bool TryGetAngle(Element e, View view, out double angle)
        {
            angle = 0;
            if (e == null || view == null) return false;

            if (e.Location is LocationPoint lp) { angle = lp.Rotation; return true; }
            if (e.Location is LocationCurve lc) return TryAngleFromCurve(lc.Curve, view, out angle);
            if (e is Wall w) return TryAngleFromVector(w.Orientation, view, out angle);
            if (e is CurveElement ce) return TryAngleFromCurve(ce.GeometryCurve, view, out angle);
            return false;
        }

        public static void RotateTo(Document doc, Element e, View view, double targetAngle)
        {
            if (!TryGetAngle(e, view, out var cur)) return;
            double delta = Normalize(targetAngle - cur);
            if (Math.Abs(delta) < EPS) return;

            XYZ origin = (e.Location as LocationPoint)?.Point
                      ?? ((e.get_BoundingBox(view) ?? e.get_BoundingBox(null)).Min
                       + (e.get_BoundingBox(view) ?? e.get_BoundingBox(null)).Max) * 0.5;

            Line axis = Line.CreateBound(origin, origin + view.ViewDirection);
            ElementTransformUtils.RotateElement(doc, e.Id, axis, delta);
        }

        static bool TryAngleFromCurve(Curve c, View v, out double a)
        {
            a = 0; if (c == null) return false;
            double mid = 0.5 * (c.GetEndParameter(0) + c.GetEndParameter(1));
            XYZ tan = c.ComputeDerivatives(mid, true).BasisX.Normalize();
            return TryAngleFromVector(tan, v, out a);
        }

        static bool TryAngleFromVector(XYZ vec, View v, out double a)
        {
            a = 0; if (vec == null) return false;
            XYZ r = v.RightDirection.Normalize(), u = v.UpDirection.Normalize();
            double x = vec.DotProduct(r), y = vec.DotProduct(u);
            if (Math.Abs(x) < EPS && Math.Abs(y) < EPS) return false;
            a = Math.Atan2(y, x); return true;
        }

        static double Normalize(double x)
        {
            while (x > Math.PI) x -= 2 * Math.PI;
            while (x < -Math.PI) x += 2 * Math.PI;
            return x;
        }
    }

}

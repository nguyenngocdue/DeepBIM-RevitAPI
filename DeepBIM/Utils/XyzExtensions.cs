using System;
using Autodesk.Revit.DB;

namespace DeepBIM.Utils
{
    public static class XyzExtensions
    {
        public static bool IsZeroLength(this XYZ v, double tol = 1e-9)
            => Math.Abs(v.X) < tol && Math.Abs(v.Y) < tol && Math.Abs(v.Z) < tol;
    }
}

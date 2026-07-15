using System;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;

namespace NacelleSolidWorks
{
    internal static class B2Geometry
    {
        public static Feature CreateOmlSection(IModelDoc2 doc, B2OmlSection s, string name)
        {
            const int count = 96;
            double[] points = new double[(count + 1) * 3];
            for (int i = 0; i <= count; i++)
            {
                double angle = 2.0 * Math.PI * i / count;
                double c = Math.Cos(angle);
                double sn = Math.Sin(angle);
                double ey = 2.0 / s.NSide;
                double ez = 2.0 / (sn >= 0.0 ? s.NTop : s.NBottom);
                points[3 * i] = s.X;
                points[3 * i + 1] = s.Width * 0.5 * SignedPower(c, ey);
                points[3 * i + 2] = s.ZCenter + s.Height * 0.5 * SignedPower(sn, ez);
            }
            return CreateClosedSpline(doc, points, name);
        }

        public static Feature CreateGuide(IModelDoc2 doc, IList<B2OmlSection> sections, int index, string name)
        {
            double[] points = new double[sections.Count * 3];
            for (int i = 0; i < sections.Count; i++)
            {
                B2OmlSection s = sections[i];
                double y = 0.0;
                double z = s.ZCenter;
                if (index == 0) z += s.Height * 0.5;
                else if (index == 1) z -= s.Height * 0.5;
                else if (index == 2) y += s.Width * 0.5;
                else y -= s.Width * 0.5;

                points[3 * i] = s.X;
                points[3 * i + 1] = y;
                points[3 * i + 2] = z;
            }
            return CreateOpenSpline(doc, points, name);
        }

        public static Feature CreateRoundedRectangleSectionX(IModelDoc2 doc, B2RoundedSection s, string name)
        {
            double r = Math.Min(s.CornerRadius, Math.Min(s.Width, s.Height) * 0.49);
            const int arcPoints = 14;
            List<double> points = new List<double>();

            AddCorner(points, s.X, +s.Width * 0.5 - r, s.ZCenter + s.Height * 0.5 - r, r, 0.0, Math.PI * 0.5, arcPoints);
            AddCorner(points, s.X, -s.Width * 0.5 + r, s.ZCenter + s.Height * 0.5 - r, r, Math.PI * 0.5, Math.PI, arcPoints);
            AddCorner(points, s.X, -s.Width * 0.5 + r, s.ZCenter - s.Height * 0.5 + r, r, Math.PI, Math.PI * 1.5, arcPoints);
            AddCorner(points, s.X, +s.Width * 0.5 - r, s.ZCenter - s.Height * 0.5 + r, r, Math.PI * 1.5, Math.PI * 2.0, arcPoints);

            points.Add(points[0]);
            points.Add(points[1]);
            points.Add(points[2]);
            return CreateClosedSpline(doc, points.ToArray(), name);
        }

        public static Feature CreateSaddleSection(IModelDoc2 doc, B2SaddleSection s, string name)
        {
            double h = s.ZTop - s.ZBottom;
            double half = s.Width * 0.5;
            double[] points = new double[]
            {
                s.X, -half, s.ZTop - 0.06 * h,
                s.X, -0.74 * half, s.ZTop - 0.01 * h,
                s.X, -0.28 * half, s.ZTop,
                s.X,  0.28 * half, s.ZTop,
                s.X,  0.74 * half, s.ZTop - 0.01 * h,
                s.X,  half, s.ZTop - 0.06 * h,
                s.X,  0.96 * half, s.ZTop - 0.28 * h,
                s.X,  0.70 * half, s.ZBottom + 0.16 * h,
                s.X,  0.30 * half, s.ZBottom + 0.02 * h,
                s.X,  0.00, s.ZBottom,
                s.X, -0.30 * half, s.ZBottom + 0.02 * h,
                s.X, -0.70 * half, s.ZBottom + 0.16 * h,
                s.X, -0.96 * half, s.ZTop - 0.28 * h,
                s.X, -half, s.ZTop - 0.06 * h
            };
            return CreateClosedSpline(doc, points, name);
        }

        private static void AddCorner(List<double> points, double x, double yCenter, double zCenter, double radius, double a0, double a1, int count)
        {
            for (int i = 0; i <= count; i++)
            {
                double a = a0 + (a1 - a0) * i / count;
                points.Add(x);
                points.Add(yCenter + radius * Math.Cos(a));
                points.Add(zCenter + radius * Math.Sin(a));
            }
        }

        private static Feature CreateClosedSpline(IModelDoc2 doc, double[] points, string name)
        {
            ISketchManager sketch = doc.SketchManager;
            doc.ClearSelection2(true);
            sketch.Insert3DSketch(true);
            SketchSegment spline = sketch.CreateSpline2(points, true);
            if (spline == null) throw new InvalidOperationException("Fallo spline cerrada B2 " + name);
            sketch.Insert3DSketch(true);
            Feature feature = doc.IFeatureByPositionReverse(0);
            if (feature == null) throw new InvalidOperationException("No se recupero feature B2 " + name);
            feature.Name = name;
            return feature;
        }

        private static Feature CreateOpenSpline(IModelDoc2 doc, double[] points, string name)
        {
            ISketchManager sketch = doc.SketchManager;
            doc.ClearSelection2(true);
            sketch.Insert3DSketch(true);
            SketchSegment spline = sketch.CreateSpline(points);
            if (spline == null) throw new InvalidOperationException("Fallo guia B2 " + name);
            sketch.Insert3DSketch(true);
            Feature feature = doc.IFeatureByPositionReverse(0);
            if (feature == null) throw new InvalidOperationException("No se recupero guia B2 " + name);
            feature.Name = name;
            return feature;
        }

        private static double SignedPower(double value, double exponent)
        {
            if (Math.Abs(value) < 1e-12) return 0.0;
            return Math.Sign(value) * Math.Pow(Math.Abs(value), exponent);
        }
    }
}

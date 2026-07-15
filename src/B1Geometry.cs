using System;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;

namespace NacelleSolidWorks
{
    internal static class B1Geometry
    {
        public static Feature CreateClosedSection(IModelDoc2 doc, B1Section s, string name)
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

        public static Feature CreateGuide(IModelDoc2 doc, IList<B1Section> sections, int index, string name)
        {
            double[] points = new double[sections.Count * 3];
            for (int i = 0; i < sections.Count; i++)
            {
                B1Section s = sections[i];
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

        public static Feature CreateSaddleSection(IModelDoc2 doc, B1SaddleSection s, string name)
        {
            // Seccion cerrada con techo casi plano y vientre curvo. Evita la joroba eliptica
            // de las revisiones anteriores y deja una superficie de apoyo creible bajo el ala.
            double h = s.ZTop - s.ZBottom;
            double half = s.Width * 0.5;
            double[] p = new double[]
            {
                s.X, -half, s.ZTop - 0.020 * h,
                s.X, -0.72 * half, s.ZTop,
                s.X,  0.72 * half, s.ZTop,
                s.X,  half, s.ZTop - 0.020 * h,
                s.X,  0.98 * half, s.ZTop - 0.22 * h,
                s.X,  0.78 * half, s.ZBottom + 0.18 * h,
                s.X,  0.38 * half, s.ZBottom + 0.02 * h,
                s.X,  0.00, s.ZBottom,
                s.X, -0.38 * half, s.ZBottom + 0.02 * h,
                s.X, -0.78 * half, s.ZBottom + 0.18 * h,
                s.X, -0.98 * half, s.ZTop - 0.22 * h,
                s.X, -half, s.ZTop - 0.020 * h
            };
            return CreateClosedSpline(doc, p, name);
        }

        public static Feature CreateRoundedRectangleSectionX(
            IModelDoc2 doc, double x, double yCenter, double zCenter,
            double width, double height, double cornerRadius, string name)
        {
            double r = Math.Min(cornerRadius, Math.Min(width, height) * 0.49);
            int arcPts = 12;
            List<double> points = new List<double>();
            AddCornerX(points, x, yCenter + width * 0.5 - r, zCenter + height * 0.5 - r, r, 0.0, Math.PI * 0.5, arcPts);
            AddCornerX(points, x, yCenter - width * 0.5 + r, zCenter + height * 0.5 - r, r, Math.PI * 0.5, Math.PI, arcPts);
            AddCornerX(points, x, yCenter - width * 0.5 + r, zCenter - height * 0.5 + r, r, Math.PI, Math.PI * 1.5, arcPts);
            AddCornerX(points, x, yCenter + width * 0.5 - r, zCenter - height * 0.5 + r, r, Math.PI * 1.5, Math.PI * 2.0, arcPts);
            points.Add(points[0]); points.Add(points[1]); points.Add(points[2]);
            return CreateClosedSpline(doc, points.ToArray(), name);
        }

        public static Feature CreateRoundedRectangleSide(
            IModelDoc2 doc, double y, double xCenter, double zCenter,
            double length, double height, double cornerRadius, string name)
        {
            double r = Math.Min(cornerRadius, Math.Min(length, height) * 0.49);
            int arcPts = 12;
            List<double> points = new List<double>();
            AddCornerSide(points, y, xCenter + length * 0.5 - r, zCenter + height * 0.5 - r, r, 0.0, Math.PI * 0.5, arcPts);
            AddCornerSide(points, y, xCenter - length * 0.5 + r, zCenter + height * 0.5 - r, r, Math.PI * 0.5, Math.PI, arcPts);
            AddCornerSide(points, y, xCenter - length * 0.5 + r, zCenter - height * 0.5 + r, r, Math.PI, Math.PI * 1.5, arcPts);
            AddCornerSide(points, y, xCenter + length * 0.5 - r, zCenter - height * 0.5 + r, r, Math.PI * 1.5, Math.PI * 2.0, arcPts);
            points.Add(points[0]); points.Add(points[1]); points.Add(points[2]);
            return CreateClosedSpline(doc, points.ToArray(), name);
        }

        public static Feature CreateNacaTopProfile(
            IModelDoc2 doc, double z, double xCenter, double yCenter,
            double length, double width, double scale, string name)
        {
            double l = length * scale;
            double w = width * scale;
            double[] points = new double[]
            {
                xCenter - 0.50 * l, yCenter, z,
                xCenter - 0.15 * l, yCenter + 0.18 * w, z,
                xCenter + 0.28 * l, yCenter + 0.48 * w, z,
                xCenter + 0.50 * l, yCenter + 0.50 * w, z,
                xCenter + 0.50 * l, yCenter - 0.50 * w, z,
                xCenter + 0.28 * l, yCenter - 0.48 * w, z,
                xCenter - 0.15 * l, yCenter - 0.18 * w, z,
                xCenter - 0.50 * l, yCenter, z
            };
            return CreateClosedSpline(doc, points, name);
        }

        private static void AddCornerX(List<double> p, double x, double yc, double zc, double r, double a0, double a1, int count)
        {
            for (int i = 0; i <= count; i++)
            {
                double a = a0 + (a1 - a0) * i / count;
                p.Add(x);
                p.Add(yc + r * Math.Cos(a));
                p.Add(zc + r * Math.Sin(a));
            }
        }

        private static void AddCornerSide(List<double> p, double y, double xc, double zc, double r, double a0, double a1, int count)
        {
            for (int i = 0; i <= count; i++)
            {
                double a = a0 + (a1 - a0) * i / count;
                p.Add(xc + r * Math.Cos(a));
                p.Add(y);
                p.Add(zc + r * Math.Sin(a));
            }
        }

        private static Feature CreateClosedSpline(IModelDoc2 doc, double[] points, string name)
        {
            ISketchManager sketch = doc.SketchManager;
            doc.ClearSelection2(true);
            sketch.Insert3DSketch(true);
            SketchSegment spline = sketch.CreateSpline2(points, true);
            if (spline == null) throw new InvalidOperationException("Fallo spline cerrada B1 " + name);
            sketch.Insert3DSketch(true);
            Feature feature = doc.IFeatureByPositionReverse(0);
            if (feature == null) throw new InvalidOperationException("No se recupero feature B1 " + name);
            feature.Name = name;
            return feature;
        }

        private static Feature CreateOpenSpline(IModelDoc2 doc, double[] points, string name)
        {
            ISketchManager sketch = doc.SketchManager;
            doc.ClearSelection2(true);
            sketch.Insert3DSketch(true);
            SketchSegment spline = sketch.CreateSpline(points);
            if (spline == null) throw new InvalidOperationException("Fallo guia B1 " + name);
            sketch.Insert3DSketch(true);
            Feature feature = doc.IFeatureByPositionReverse(0);
            if (feature == null) throw new InvalidOperationException("No se recupero guia B1 " + name);
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

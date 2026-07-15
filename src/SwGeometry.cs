using System;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;

namespace NacelleSolidWorks
{
    internal static class SwGeometry
    {
        public static Feature CreateClosedSection(IModelDoc2 doc, NacelleSection s, double yCenter, string name)
        {
            const int count = 72;
            double[] points = new double[(count + 1) * 3];
            for (int i = 0; i <= count; i++)
            {
                double angle = 2.0 * Math.PI * i / count;
                double c = Math.Cos(angle);
                double sn = Math.Sin(angle);
                double ey = 2.0 / s.NSide;
                double ez = 2.0 / (sn >= 0.0 ? s.NTop : s.NBottom);
                points[3 * i] = s.X;
                points[3 * i + 1] = yCenter + s.Width * 0.5 * SignedPower(c, ey);
                points[3 * i + 2] = s.ZCenter + s.Height * 0.5 * SignedPower(sn, ez);
            }
            return CreateClosedSpline(doc, points, name);
        }

        public static Feature CreateFairingSection(IModelDoc2 doc, FairingSection s, double yCenter, string name)
        {
            const int count = 48;
            double zCenter = (s.ZBottom + s.ZTop) * 0.5;
            double height = s.ZTop - s.ZBottom;
            double[] points = new double[(count + 1) * 3];
            for (int i = 0; i <= count; i++)
            {
                double angle = 2.0 * Math.PI * i / count;
                double c = Math.Cos(angle);
                double sn = Math.Sin(angle);
                points[3 * i] = s.X;
                points[3 * i + 1] = yCenter + s.Width * 0.5 * SignedPower(c, 0.78);
                points[3 * i + 2] = zCenter + height * 0.5 * SignedPower(sn, sn >= 0.0 ? 0.90 : 0.72);
            }
            return CreateClosedSpline(doc, points, name);
        }

        public static Feature CreateGuide(IModelDoc2 doc, IList<NacelleSection> sections, double yCenter, int guideIndex, string name)
        {
            double[] points = new double[sections.Count * 3];
            for (int i = 0; i < sections.Count; i++)
            {
                NacelleSection s = sections[i];
                double y = yCenter;
                double z = s.ZCenter;
                if (guideIndex == 0) z += s.Height * 0.5;
                else if (guideIndex == 1) z -= s.Height * 0.5;
                else if (guideIndex == 2) y += s.Width * 0.5;
                else y -= s.Width * 0.5;
                points[3 * i] = s.X;
                points[3 * i + 1] = y;
                points[3 * i + 2] = z;
            }

            ISketchManager sketch = doc.SketchManager;
            doc.ClearSelection2(true);
            sketch.Insert3DSketch(true);
            SketchSegment spline = sketch.CreateSpline(points);
            if (spline == null) throw new InvalidOperationException("Fallo curva guia " + name);
            sketch.Insert3DSketch(true);
            Feature feature = doc.IFeatureByPositionReverse(0);
            feature.Name = name;
            return feature;
        }

        public static Feature LoftWithGuides(IModelDoc2 doc, IList<Feature> profiles, IList<Feature> guides, string name)
        {
            doc.ClearSelection2(true);
            for (int i = 0; i < profiles.Count; i++)
                if (!profiles[i].Select2(i > 0, 1)) throw new InvalidOperationException("No se selecciono perfil " + i);
            for (int i = 0; i < guides.Count; i++)
                if (!guides[i].Select2(true, 2)) throw new InvalidOperationException("No se selecciono guia " + i);

            Feature loft = doc.FeatureManager.InsertProtrusionBlend2(
                false, true, false, 1, 0, 0, 1, 1,
                true, true, false, 0, 0, 0,
                true, true, true, 3);
            if (loft == null) throw new InvalidOperationException("Fallo loft principal con guias");
            loft.Name = name;
            doc.ClearSelection2(true);
            doc.EditRebuild3();
            return loft;
        }

        public static Feature SimpleMergedLoft(IModelDoc2 doc, IList<Feature> profiles, string name)
        {
            doc.ClearSelection2(true);
            for (int i = 0; i < profiles.Count; i++)
                if (!profiles[i].Select2(i > 0, 1)) throw new InvalidOperationException("No se selecciono perfil de " + name);

            Feature loft = doc.FeatureManager.InsertProtrusionBlend2(
                false, false, false, 1, 0, 0, 0, 0,
                false, false, false, 0, 0, 0,
                true, false, true, 0);
            if (loft == null) throw new InvalidOperationException("Fallo " + name);
            loft.Name = name;
            doc.ClearSelection2(true);
            doc.EditRebuild3();
            return loft;
        }

        public static Feature CreateAxisSketch(IModelDoc2 doc, double x1, double x2, double y, double z, string name)
        {
            ISketchManager sketch = doc.SketchManager;
            doc.ClearSelection2(true);
            sketch.Insert3DSketch(true);
            SketchSegment line = sketch.CreateLine(x1, y, z, x2, y, z);
            if (line == null) throw new InvalidOperationException("Fallo eje de referencia");
            line.ConstructionGeometry = true;
            sketch.Insert3DSketch(true);
            Feature feature = doc.IFeatureByPositionReverse(0);
            feature.Name = name;
            return feature;
        }

        public static void HideFeatureSketch(IModelDoc2 doc, Feature feature)
        {
            doc.ClearSelection2(true);
            if (feature != null && feature.Select2(false, 0)) doc.BlankSketch();
            doc.ClearSelection2(true);
        }

        public static void HideConstruction(IModelDoc2 doc)
        {
            Feature feature = doc.FirstFeature() as Feature;
            while (feature != null)
            {
                string type = feature.GetTypeName2() ?? "";
                doc.ClearSelection2(true);
                if (feature.Select2(false, 0))
                {
                    if (type.IndexOf("ProfileFeature", StringComparison.OrdinalIgnoreCase) >= 0) doc.BlankSketch();
                    else if (type.IndexOf("RefPlane", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             type.IndexOf("RefAxis", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             type.IndexOf("RefPoint", StringComparison.OrdinalIgnoreCase) >= 0) doc.BlankRefGeom();
                }
                feature = feature.GetNextFeature() as Feature;
            }
            doc.ClearSelection2(true);
        }

        public static int SolidBodyCount(IModelDoc2 doc)
        {
            object[] bodies = ((IPartDoc)doc).GetBodies2(0, true) as object[];
            return bodies == null ? 0 : bodies.Length;
        }

        public static int SurfaceBodyCount(IModelDoc2 doc)
        {
            object[] bodies = ((IPartDoc)doc).GetBodies2(1, true) as object[];
            return bodies == null ? 0 : bodies.Length;
        }

        public static double[] BoundingBox(IModelDoc2 doc)
        {
            object[] bodies = ((IPartDoc)doc).GetBodies2(0, true) as object[];
            if (bodies == null || bodies.Length == 0) throw new InvalidOperationException("La pieza no contiene solidos");
            Body2 body = (Body2)bodies[0];
            double[] box = body.GetBodyBox() as double[];
            if (box == null || box.Length < 6) throw new InvalidOperationException("No se pudo leer bounding box");
            return box;
        }

        public static bool TryFilletNear(IModelDoc2 doc, double radius, double x, double y, double z, string name)
        {
            object[] bodies = ((IPartDoc)doc).GetBodies2(0, true) as object[];
            if (bodies == null || bodies.Length == 0) return false;
            Body2 body = (Body2)bodies[0];
            object[] edges = body.GetEdges() as object[];
            if (edges == null) return false;
            Edge best = null;
            double bestDistance = Double.MaxValue;
            foreach (object item in edges)
            {
                Edge edge = (Edge)item;
                double[] q = edge.GetClosestPointOn(x, y, z) as double[];
                if (q == null) continue;
                double d = Math.Sqrt(Math.Pow(q[0] - x, 2) + Math.Pow(q[1] - y, 2) + Math.Pow(q[2] - z, 2));
                if (d < bestDistance) { bestDistance = d; best = edge; }
            }
            if (best == null || bestDistance > 0.35) return false;
            doc.ClearSelection2(true);
            ISelectionMgr mgr = (ISelectionMgr)doc.SelectionManager;
            SelectData data = (SelectData)mgr.CreateSelectData();
            ((Entity)best).Select4(false, data);
            int result = doc.FeatureFillet3(radius, true, 0, false, 0, 0, null, false, false);
            doc.ClearSelection2(true);
            if (result == 0) return false;
            Feature feature = doc.IFeatureByPositionReverse(0);
            if (feature != null) feature.Name = name;
            doc.EditRebuild3();
            return true;
        }

        private static Feature CreateClosedSpline(IModelDoc2 doc, double[] points, string name)
        {
            ISketchManager sketch = doc.SketchManager;
            doc.ClearSelection2(true);
            sketch.Insert3DSketch(true);
            SketchSegment spline = sketch.CreateSpline2(points, true);
            if (spline == null) throw new InvalidOperationException("Fallo spline cerrada " + name);
            sketch.Insert3DSketch(true);
            Feature feature = doc.IFeatureByPositionReverse(0);
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

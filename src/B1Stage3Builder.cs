using System;
using System.Globalization;
using System.IO;
using SolidWorks.Interop.sldworks;

namespace NacelleSolidWorks
{
    internal sealed class B1Stage3Builder
    {
        private readonly SwSession session;
        private readonly B1Config cfg;
        private readonly Action<string> log;

        public B1Stage3Builder(SwSession session, B1Config cfg, Action<string> log)
        {
            this.session = session;
            this.cfg = cfg;
            this.log = log;
        }

        public B1BuildResult Build(B1BuildResult stage2)
        {
            IModelDoc2 doc = stage2.Document;
            string partPath = Path.Combine(stage2.OutputDirectory, "NACELA_DERECHA_B1_STAGE3_FINAL.SLDPRT");
            session.CloseIfOpen(partPath);

            AddLargeCowling(doc, +1, "EXTERIOR");
            ValidateSingleSolid(doc, "B1 capo exterior");
            AddLargeCowling(doc, -1, "INTERIOR");
            ValidateSingleSolid(doc, "B1 capo interior");
            AddFirewallLine(doc, +1, "EXTERIOR");
            AddFirewallLine(doc, -1, "INTERIOR");
            AddServicePanel(doc);
            ValidateSingleSolid(doc, "B1 Stage 3 final");

            SwGeometry.HideConstruction(doc);
            doc.EditRebuild3();

            int saveError = doc.SaveAs3(partPath, 0, 2);
            if (saveError != 0) throw new IOException("No se pudo guardar B1 Stage 3. Error=" + saveError);

            SaveReviewViews(doc, stage2.OutputDirectory, "NACELA_DERECHA_B1_STAGE3_FINAL");
            string reportPath = Path.Combine(stage2.OutputDirectory, "VALIDACION_B1_STAGE3.txt");
            File.WriteAllText(reportPath, BuildReport(doc));
            log("B1 Stage 3 final creado: " + partPath);

            return new B1BuildResult
            {
                Document = doc,
                PartPath = partPath,
                ReportPath = reportPath,
                OutputDirectory = stage2.OutputDirectory
            };
        }

        private void AddLargeCowling(IModelDoc2 doc, int sign, string label)
        {
            // Rebaje muy poco profundo. Marca un capo grande siguiendo el lateral sin
            // generar otra puerta abultada ni una placa independiente.
            Feature outer = B1Geometry.CreateRoundedRectangleSide(
                doc, sign * cfg.CowlOuterY, cfg.CowlX, cfg.CowlZ,
                cfg.CowlLength, cfg.CowlHeight, 0.110,
                "B1_11_CAPO_" + label + "_CONTORNO");
            Feature inner = B1Geometry.CreateRoundedRectangleSide(
                doc, sign * cfg.CowlInnerY, cfg.CowlX + 0.008, cfg.CowlZ,
                cfg.CowlLength * 0.975, cfg.CowlHeight * 0.965, 0.104,
                "B1_11_CAPO_" + label + "_FONDO");
            Feature toolFeature = SwGeometry.LoftTool(doc, new[] { outer, inner }, "B1_11_HERRAMIENTA_CAPO_" + label);
            SwGeometry.SubtractBodies(doc, SwGeometry.LargestSolidBody(doc), SwGeometry.BodyOf(toolFeature), "B1_11_CAPO_" + label + "_ENRASADO");
            Hide(doc, outer); Hide(doc, inner);

            bool f = SwGeometry.TryFilletNear(
                doc, 0.005, cfg.CowlX - cfg.CowlLength * 0.46,
                sign * (cfg.CowlOuterY - 0.020), cfg.CowlZ,
                "B1_11_FILETE_CAPO_" + label);
            log(f ? "B1: capo " + label + " marcado con borde redondeado." : "B1: capo " + label + " creado sin filete suplementario.");
        }

        private void AddFirewallLine(IModelDoc2 doc, int sign, string label)
        {
            Feature outer = B1Geometry.CreateRoundedRectangleSide(
                doc, sign * cfg.CowlOuterY, cfg.FirewallX, -0.080,
                cfg.FirewallGrooveWidth, 0.940, 0.006,
                "B1_12_FIREWALL_" + label + "_EXT");
            Feature inner = B1Geometry.CreateRoundedRectangleSide(
                doc, sign * cfg.CowlInnerY, cfg.FirewallX, -0.080,
                cfg.FirewallGrooveWidth, 0.910, 0.006,
                "B1_12_FIREWALL_" + label + "_INT");
            Feature toolFeature = SwGeometry.LoftTool(doc, new[] { outer, inner }, "B1_12_HERRAMIENTA_FIREWALL_" + label);
            SwGeometry.SubtractBodies(doc, SwGeometry.LargestSolidBody(doc), SwGeometry.BodyOf(toolFeature), "B1_12_LINEA_FIREWALL_" + label);
            Hide(doc, outer); Hide(doc, inner);
        }

        private void AddServicePanel(IModelDoc2 doc)
        {
            Feature outer = B1Geometry.CreateRoundedRectangleSide(
                doc, cfg.CowlOuterY, cfg.ServicePanelX, cfg.ServicePanelZ,
                cfg.ServicePanelLength, cfg.ServicePanelHeight, 0.045,
                "B1_13_PANEL_SERVICIO_CONTORNO");
            Feature inner = B1Geometry.CreateRoundedRectangleSide(
                doc, cfg.CowlInnerY, cfg.ServicePanelX + 0.005, cfg.ServicePanelZ,
                cfg.ServicePanelLength * 0.955, cfg.ServicePanelHeight * 0.930, 0.042,
                "B1_13_PANEL_SERVICIO_FONDO");
            Feature toolFeature = SwGeometry.LoftTool(doc, new[] { outer, inner }, "B1_13_HERRAMIENTA_PANEL_SERVICIO");
            SwGeometry.SubtractBodies(doc, SwGeometry.LargestSolidBody(doc), SwGeometry.BodyOf(toolFeature), "B1_13_PANEL_SERVICIO_ENRASADO");
            Hide(doc, outer); Hide(doc, inner);
        }

        private string BuildReport(IModelDoc2 doc)
        {
            return
                "NACELA B1 - STAGE 3 FINAL\r\n" +
                "Capos=2 grandes rebajados sobre la piel\r\n" +
                "Firewall=2 lineas funcionales\r\n" +
                "Panel_servicio=1 exterior\r\n" +
                "Solidos=" + SwGeometry.SolidBodyCount(doc) + "\r\n" +
                "Superficies=" + SwGeometry.SurfaceBodyCount(doc) + "\r\n";
        }

        private static void Hide(IModelDoc2 doc, Feature feature) { SwGeometry.HideFeatureSketch(doc, feature); }

        private static void ValidateSingleSolid(IModelDoc2 doc, string stage)
        {
            int solids = SwGeometry.SolidBodyCount(doc);
            int surfaces = SwGeometry.SurfaceBodyCount(doc);
            if (solids != 1 || surfaces != 0)
                throw new InvalidOperationException(stage + " invalido. Solidos=" + solids + " superficies=" + surfaces);
        }

        private static void SaveReviewViews(IModelDoc2 doc, string directory, string prefix)
        {
            SaveView(doc, "*Right", 4, Path.Combine(directory, prefix + "_LATERAL.bmp"));
            SaveView(doc, "*Front", 1, Path.Combine(directory, prefix + "_FRONTAL.bmp"));
            SaveView(doc, "*Top", 5, Path.Combine(directory, prefix + "_PLANTA.bmp"));
            SaveView(doc, "*Isometric", 7, Path.Combine(directory, prefix + "_ISO.bmp"));
        }

        private static void SaveView(IModelDoc2 doc, string name, int id, string path)
        {
            doc.ShowNamedView2(name, id);
            doc.ViewZoomtofit2();
            doc.GraphicsRedraw2();
            doc.SaveBMP(path, 1600, 1000);
        }

        private static string F(double value) { return value.ToString("0.000000", CultureInfo.InvariantCulture); }
    }
}

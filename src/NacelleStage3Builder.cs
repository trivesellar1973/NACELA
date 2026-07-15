using System;
using System.Globalization;
using System.IO;
using SolidWorks.Interop.sldworks;

namespace NacelleSolidWorks
{
    internal sealed class NacelleStage3Builder
    {
        private readonly SwSession session;
        private readonly NacelleConfig cfg;
        private readonly Action<string> log;

        public NacelleStage3Builder(SwSession session, NacelleConfig cfg, Action<string> log)
        {
            this.session = session;
            this.cfg = cfg;
            this.log = log;
        }

        public BuildResult Build(BuildResult stage2)
        {
            IModelDoc2 doc = stage2.Document;
            string partPath = Path.Combine(stage2.OutputDirectory, "NACELA_DERECHA_" + cfg.Revision + "_STAGE3_FINAL.SLDPRT");
            session.CloseIfOpen(partPath);

            AddMainCowlingRecess(doc, +1, "EXTERIOR");
            ValidateSingleSolid(doc, "Stage 3 despues de capo exterior");

            AddMainCowlingRecess(doc, -1, "INTERIOR");
            ValidateSingleSolid(doc, "Stage 3 despues de capo interior");

            AddFirewallGroove(doc, +1, "EXTERIOR");
            AddFirewallGroove(doc, -1, "INTERIOR");
            ValidateSingleSolid(doc, "Stage 3 despues de linea de firewall");

            AddOilServicePanel(doc);
            ValidateSingleSolid(doc, "Stage 3 despues de panel de aceite");

            SwGeometry.HideConstruction(doc);
            doc.EditRebuild3();

            int saveError = doc.SaveAs3(partPath, 0, 2);
            if (saveError != 0) throw new IOException("No se pudo guardar Stage 3. Error=" + saveError);

            SaveReviewViews(doc, stage2.OutputDirectory, "NACELA_DERECHA_" + cfg.Revision + "_STAGE3_FINAL");

            string reportPath = Path.Combine(stage2.OutputDirectory, "VALIDACION_STAGE3_" + cfg.Revision + ".txt");
            File.WriteAllText(reportPath, BuildReport(doc));
            log("Stage 3 con capos y paneles funcionales creado: " + partPath);

            return new BuildResult
            {
                Document = doc,
                PartPath = partPath,
                LogPath = reportPath,
                OutputDirectory = stage2.OutputDirectory
            };
        }

        private void AddMainCowlingRecess(IModelDoc2 doc, int sign, string label)
        {
            Feature outer = SwGeometry.CreateRoundedSideSection(
                doc, sign * cfg.CowlOuterY, cfg.CowlX, cfg.CowlZ,
                cfg.CowlLength, cfg.CowlHeight,
                "11_CAPO_" + label + "_CONTORNO_EXTERIOR");

            Feature inner = SwGeometry.CreateRoundedSideSection(
                doc, sign * cfg.CowlInnerY, cfg.CowlX + 0.010, cfg.CowlZ,
                cfg.CowlLength * 0.970, cfg.CowlHeight * 0.955,
                "11_CAPO_" + label + "_FONDO_RECESO");

            Feature toolFeature = SwGeometry.LoftTool(doc, new[] { outer, inner }, "11_HERRAMIENTA_RECESO_CAPO_" + label);
            Body2 main = SwGeometry.LargestSolidBody(doc);
            Body2 tool = SwGeometry.BodyOf(toolFeature);
            SwGeometry.SubtractBodies(doc, main, tool, "11_CAPO_PRINCIPAL_" + label + "_ENRASADO");
            SwGeometry.HideFeatureSketch(doc, outer);
            SwGeometry.HideFeatureSketch(doc, inner);

            bool fillet = SwGeometry.TryFilletNear(
                doc, 0.006, cfg.CowlX - cfg.CowlLength * 0.45,
                sign * (cfg.CowlOuterY - 0.025), cfg.CowlZ,
                "11_FILETE_CAPO_" + label + "_R006");
            log(fillet ? "Contorno de capo " + label + " redondeado." : "Aviso: filete del capo " + label + " omitido.");
        }

        private void AddFirewallGroove(IModelDoc2 doc, int sign, string label)
        {
            Feature outer = SwGeometry.CreateRoundedSideSection(
                doc, sign * cfg.CowlOuterY, cfg.FirewallX, -0.080,
                cfg.FirewallGrooveWidth, 0.960,
                "12_FIREWALL_" + label + "_EXTERIOR");

            Feature inner = SwGeometry.CreateRoundedSideSection(
                doc, sign * cfg.CowlInnerY, cfg.FirewallX, -0.080,
                cfg.FirewallGrooveWidth, 0.930,
                "12_FIREWALL_" + label + "_INTERIOR");

            Feature toolFeature = SwGeometry.LoftTool(doc, new[] { outer, inner }, "12_HERRAMIENTA_LINEA_FIREWALL_" + label);
            Body2 main = SwGeometry.LargestSolidBody(doc);
            Body2 tool = SwGeometry.BodyOf(toolFeature);
            SwGeometry.SubtractBodies(doc, main, tool, "12_LINEA_FIREWALL_" + label);
            SwGeometry.HideFeatureSketch(doc, outer);
            SwGeometry.HideFeatureSketch(doc, inner);
        }

        private void AddOilServicePanel(IModelDoc2 doc)
        {
            Feature outer = SwGeometry.CreateRoundedSideSection(
                doc, cfg.CowlOuterY, cfg.OilPanelX, cfg.OilPanelZ,
                cfg.OilPanelLength, cfg.OilPanelHeight,
                "13_PANEL_ACEITE_EXTERIOR");

            Feature inner = SwGeometry.CreateRoundedSideSection(
                doc, cfg.CowlInnerY, cfg.OilPanelX, cfg.OilPanelZ,
                cfg.OilPanelLength * 0.940, cfg.OilPanelHeight * 0.900,
                "13_PANEL_ACEITE_FONDO");

            Feature toolFeature = SwGeometry.LoftTool(doc, new[] { outer, inner }, "13_HERRAMIENTA_PANEL_ACEITE");
            Body2 main = SwGeometry.LargestSolidBody(doc);
            Body2 tool = SwGeometry.BodyOf(toolFeature);
            SwGeometry.SubtractBodies(doc, main, tool, "13_PANEL_SERVICIO_ACEITE_ENRASADO");
            SwGeometry.HideFeatureSketch(doc, outer);
            SwGeometry.HideFeatureSketch(doc, inner);
        }

        private string BuildReport(IModelDoc2 doc)
        {
            return
                "NACELA SOLIDWORKS - VALIDACION STAGE 3 FINAL A2\r\n" +
                "Revision=" + cfg.Revision + "\r\n" +
                "Capos=1 exterior y 1 interior, grandes y enrasados\r\n" +
                "Firewall=linea funcional lateral en ambos lados\r\n" +
                "Panel_servicio=aceite exterior\r\n" +
                "Metodo=rebajes nativos por loft; no son piezas pegadas\r\n" +
                "Solidos=" + SwGeometry.SolidBodyCount(doc) + "\r\n" +
                "Superficies=" + SwGeometry.SurfaceBodyCount(doc) + "\r\n" +
                "Estado=LISTO_PARA_REVISION_VISUAL_ENSAMBLAJE\r\n";
        }

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

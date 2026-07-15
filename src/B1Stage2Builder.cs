using System;
using System.Globalization;
using System.IO;
using SolidWorks.Interop.sldworks;

namespace NacelleSolidWorks
{
    internal sealed class B1Stage2Builder
    {
        private readonly SwSession session;
        private readonly B1Config cfg;
        private readonly Action<string> log;

        public B1Stage2Builder(SwSession session, B1Config cfg, Action<string> log)
        {
            this.session = session;
            this.cfg = cfg;
            this.log = log;
        }

        public B1BuildResult Build(B1BuildResult stage1)
        {
            IModelDoc2 doc = stage1.Document;
            string partPath = Path.Combine(stage1.OutputDirectory, "NACELA_DERECHA_B1_STAGE2_SISTEMAS.SLDPRT");
            session.CloseIfOpen(partPath);

            AddMainRoundedScoop(doc);
            ValidateSingleSolid(doc, "B1 toma principal");

            AddSideInlet(doc, +1, "EXTERIOR");
            ValidateSingleSolid(doc, "B1 toma lateral exterior");
            AddSideInlet(doc, -1, "INTERIOR");
            ValidateSingleSolid(doc, "B1 toma lateral interior");

            AddHighExhaust(doc, +1, "EXTERIOR");
            ValidateSingleSolid(doc, "B1 escape exterior");
            AddHighExhaust(doc, -1, "INTERIOR");
            ValidateSingleSolid(doc, "B1 escape interior");

            SwGeometry.HideConstruction(doc);
            doc.EditRebuild3();

            int saveError = doc.SaveAs3(partPath, 0, 2);
            if (saveError != 0) throw new IOException("No se pudo guardar B1 Stage 2. Error=" + saveError);

            SaveReviewViews(doc, stage1.OutputDirectory, "NACELA_DERECHA_B1_STAGE2");
            string reportPath = Path.Combine(stage1.OutputDirectory, "VALIDACION_B1_STAGE2.txt");
            File.WriteAllText(reportPath, BuildReport(doc));
            log("B1 Stage 2 creado: " + partPath);

            return new B1BuildResult
            {
                Document = doc,
                PartPath = partPath,
                ReportPath = reportPath,
                OutputDirectory = stage1.OutputDirectory
            };
        }

        private void AddMainRoundedScoop(IModelDoc2 doc)
        {
            // Carenado exterior que sobresale de la panza y genera la silueta rectangular
            // ovalada de la referencia. Se fusiona antes de abrir el conducto.
            Feature s0 = B1Geometry.CreateRoundedRectangleSectionX(
                doc, 0.180, 0.0, -0.360,
                0.560, 0.220, 0.075,
                "B1_07_SCOOP_EXTERIOR_FRENTE");
            Feature s1 = B1Geometry.CreateRoundedRectangleSectionX(
                doc, cfg.IntakeXMid, 0.0, cfg.IntakeZMid,
                cfg.IntakeScoopWidthMid, cfg.IntakeScoopHeightMid, 0.105,
                "B1_07_SCOOP_EXTERIOR_MAXIMO");
            Feature s2 = B1Geometry.CreateRoundedRectangleSectionX(
                doc, 0.800, 0.0, -0.500,
                0.640, 0.300, 0.095,
                "B1_07_SCOOP_EXTERIOR_TRASERO");
            SwGeometry.SimpleMergedLoft(doc, new[] { s0, s1, s2 }, "B1_07_CARENADO_TOMA_RECTANGULAR");
            Hide(doc, s0); Hide(doc, s1); Hide(doc, s2);

            // Conducto real. La primera seccion es la boca rounded rectangle; luego se
            // contrae y asciende hacia la interfaz del motor.
            Feature d0 = B1Geometry.CreateRoundedRectangleSectionX(
                doc, cfg.IntakeXFront, 0.0, cfg.IntakeZFront,
                cfg.IntakeOpeningWidth, cfg.IntakeOpeningHeight, cfg.IntakeCornerRadius,
                "B1_08_TOMA_PRINCIPAL_BOCA");
            Feature d1 = B1Geometry.CreateRoundedRectangleSectionX(
                doc, cfg.IntakeXMid, 0.0, cfg.IntakeZMid + 0.015,
                0.540, 0.250, 0.075,
                "B1_08_TOMA_PRINCIPAL_GARGANTA");
            Feature d2 = B1Geometry.CreateRoundedRectangleSectionX(
                doc, cfg.IntakeXInterface, 0.0, cfg.IntakeZInterface,
                cfg.IntakeInterfaceWidth, cfg.IntakeInterfaceHeight, 0.060,
                "B1_08_TOMA_INTERFAZ_MOTOR");
            Feature toolFeature = SwGeometry.LoftTool(doc, new[] { d0, d1, d2 }, "B1_08_HERRAMIENTA_DUCTO_PRINCIPAL");
            SwGeometry.SubtractBodies(doc, SwGeometry.LargestSolidBody(doc), SwGeometry.BodyOf(toolFeature), "B1_08_DUCTO_PRINCIPAL_INTEGRADO");
            Hide(doc, d0); Hide(doc, d1); Hide(doc, d2);

            bool f1 = SwGeometry.TryFilletNear(doc, 0.026, cfg.IntakeXFront + 0.030, cfg.IntakeOpeningWidth * 0.47, cfg.IntakeZFront, "B1_08_LABIO_TOMA_DER");
            bool f2 = SwGeometry.TryFilletNear(doc, 0.026, cfg.IntakeXFront + 0.030, -cfg.IntakeOpeningWidth * 0.47, cfg.IntakeZFront, "B1_08_LABIO_TOMA_IZQ");
            log(f1 || f2 ? "B1: toma inferior con labio redondeado." : "B1: toma inferior creada; filetes de labio omitidos por kernel.");
        }

        private void AddSideInlet(IModelDoc2 doc, int sign, string label)
        {
            // Housing corto y redondeado. El perfil interior parte dentro de la OML y el
            // exterior sale pocos milimetros, por lo que no queda como pod separado.
            Feature h0 = B1Geometry.CreateRoundedRectangleSide(
                doc, sign * 0.410, cfg.SideInletX, cfg.SideInletZ,
                cfg.SideInletLength, cfg.SideInletHeight, 0.060,
                "B1_09_TOMA_LATERAL_" + label + "_BASE");
            Feature h1 = B1Geometry.CreateRoundedRectangleSide(
                doc, sign * cfg.SideInletOuterY, cfg.SideInletX - 0.010, cfg.SideInletZ,
                cfg.SideInletLength * 0.92, cfg.SideInletHeight * 0.90, 0.055,
                "B1_09_TOMA_LATERAL_" + label + "_LABIO");
            SwGeometry.SimpleMergedLoft(doc, new[] { h0, h1 }, "B1_09_HOUSING_TOMA_LATERAL_" + label);
            Hide(doc, h0); Hide(doc, h1);

            Feature c0 = B1Geometry.CreateRoundedRectangleSide(
                doc, sign * cfg.SideInletInnerY, cfg.SideInletX + 0.020, cfg.SideInletZ - 0.005,
                cfg.SideInletOpenLength * 0.82, cfg.SideInletOpenHeight * 0.80, 0.040,
                "B1_09_TOMA_LATERAL_" + label + "_DUCTO_INTERIOR");
            Feature c1 = B1Geometry.CreateRoundedRectangleSide(
                doc, sign * (cfg.SideInletOuterY + 0.020), cfg.SideInletX, cfg.SideInletZ,
                cfg.SideInletOpenLength, cfg.SideInletOpenHeight, 0.045,
                "B1_09_TOMA_LATERAL_" + label + "_BOCA");
            Feature toolFeature = SwGeometry.LoftTool(doc, new[] { c0, c1 }, "B1_09_HERRAMIENTA_TOMA_LATERAL_" + label);
            SwGeometry.SubtractBodies(doc, SwGeometry.LargestSolidBody(doc), SwGeometry.BodyOf(toolFeature), "B1_09_TOMA_LATERAL_" + label + "_ABIERTA");
            Hide(doc, c0); Hide(doc, c1);
        }

        private void AddHighExhaust(IModelDoc2 doc, int sign, string label)
        {
            // Housing exterior semejante al rebaje rojo/blanco de la referencia CFD.
            Feature h0 = B1Geometry.CreateRoundedRectangleSide(
                doc, sign * cfg.ExhaustHousingInnerY, cfg.ExhaustX, cfg.ExhaustZ,
                cfg.ExhaustHousingLength, cfg.ExhaustHousingHeight, 0.075,
                "B1_10_ESCAPE_" + label + "_HOUSING_BASE");
            Feature h1 = B1Geometry.CreateRoundedRectangleSide(
                doc, sign * cfg.ExhaustOuterY, cfg.ExhaustX + 0.010, cfg.ExhaustZ,
                cfg.ExhaustHousingLength * 0.90, cfg.ExhaustHousingHeight * 0.88, 0.065,
                "B1_10_ESCAPE_" + label + "_HOUSING_EXTERIOR");
            SwGeometry.SimpleMergedLoft(doc, new[] { h0, h1 }, "B1_10_HOUSING_TERMICO_ESCAPE_" + label);
            Hide(doc, h0); Hide(doc, h1);

            Feature n0 = B1Geometry.CreateRoundedRectangleSide(
                doc, sign * cfg.ExhaustDuctInnerY, cfg.ExhaustX - 0.040, cfg.ExhaustZ + 0.005,
                cfg.ExhaustNozzleLength * 0.78, cfg.ExhaustNozzleHeight * 0.74, 0.040,
                "B1_10_ESCAPE_" + label + "_DUCTO_INTERIOR");
            Feature n1 = B1Geometry.CreateRoundedRectangleSide(
                doc, sign * (cfg.ExhaustOuterY + 0.025), cfg.ExhaustX + 0.015, cfg.ExhaustZ,
                cfg.ExhaustNozzleLength, cfg.ExhaustNozzleHeight, 0.050,
                "B1_10_ESCAPE_" + label + "_NOZZLE");
            Feature toolFeature = SwGeometry.LoftTool(doc, new[] { n0, n1 }, "B1_10_HERRAMIENTA_ESCAPE_" + label);
            SwGeometry.SubtractBodies(doc, SwGeometry.LargestSolidBody(doc), SwGeometry.BodyOf(toolFeature), "B1_10_ESCAPE_ALTO_" + label + "_ABIERTO");
            Hide(doc, n0); Hide(doc, n1);

            bool f = SwGeometry.TryFilletNear(
                doc, 0.014, cfg.ExhaustX, sign * (cfg.ExhaustOuterY - 0.010), cfg.ExhaustZ + cfg.ExhaustNozzleHeight * 0.45,
                "B1_10_LABIO_ESCAPE_" + label);
            log(f ? "B1: escape " + label + " con labio redondeado." : "B1: escape " + label + " creado sin filete suplementario.");
        }

        private string BuildReport(IModelDoc2 doc)
        {
            double roundedArea = cfg.IntakeOpeningWidth * cfg.IntakeOpeningHeight -
                (4.0 - Math.PI) * cfg.IntakeCornerRadius * cfg.IntakeCornerRadius;
            return
                "NACELA B1 - STAGE 2 SISTEMAS EXTERNOS\r\n" +
                "Toma_principal=rounded rectangle scoop con ducto ascendente\r\n" +
                "Area_toma_requerida=" + F(cfg.IntakeRequiredArea) + " m2\r\n" +
                "Area_toma_aproximada=" + F(roundedArea) + " m2\r\n" +
                "Tomas_laterales=2 housings redondeados con ducto real\r\n" +
                "Escapes=2 housings altos con nozzle interior\r\n" +
                "Diametro_equivalente_escape_previo=" + F(cfg.ExhaustEquivalentEach) + " m SOLO_REFERENCIA\r\n" +
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

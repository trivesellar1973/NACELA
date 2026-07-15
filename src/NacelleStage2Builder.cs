using System;
using System.Globalization;
using System.IO;
using SolidWorks.Interop.sldworks;

namespace NacelleSolidWorks
{
    internal sealed class NacelleStage2Builder
    {
        private readonly SwSession session;
        private readonly NacelleConfig cfg;
        private readonly Action<string> log;

        public NacelleStage2Builder(SwSession session, NacelleConfig cfg, Action<string> log)
        {
            this.session = session;
            this.cfg = cfg;
            this.log = log;
        }

        public BuildResult Build(BuildResult stage1)
        {
            IModelDoc2 doc = stage1.Document;
            string partPath = Path.Combine(stage1.OutputDirectory, "NACELA_DERECHA_" + cfg.Revision + "_STAGE2_SISTEMAS.SLDPRT");
            session.CloseIfOpen(partPath);

            AddMainChinIntake(doc);
            ValidateSingleSolid(doc, "Stage 2 despues de toma principal");

            AddRecessedSideExhaust(doc, +1, "EXTERIOR");
            ValidateSingleSolid(doc, "Stage 2 despues de escape exterior");

            AddRecessedSideExhaust(doc, -1, "INTERIOR");
            ValidateSingleSolid(doc, "Stage 2 despues de escape interior");

            AddAccessoryNacaDuct(doc, +1, "EXTERIOR");
            ValidateSingleSolid(doc, "Stage 2 despues de NACA exterior");

            AddAccessoryNacaDuct(doc, -1, "INTERIOR");
            ValidateSingleSolid(doc, "Stage 2 despues de NACA interior");

            SwGeometry.HideConstruction(doc);
            doc.EditRebuild3();

            int saveError = doc.SaveAs3(partPath, 0, 2);
            if (saveError != 0) throw new IOException("No se pudo guardar Stage 2. Error=" + saveError);

            SaveReviewViews(doc, stage1.OutputDirectory, "NACELA_DERECHA_" + cfg.Revision + "_STAGE2");

            string reportPath = Path.Combine(stage1.OutputDirectory, "VALIDACION_STAGE2_" + cfg.Revision + ".txt");
            File.WriteAllText(reportPath, BuildReport(doc));
            log("Stage 2 A2 con toma, escapes enrasados y NACA creado: " + partPath);

            return new BuildResult
            {
                Document = doc,
                PartPath = partPath,
                LogPath = reportPath,
                OutputDirectory = stage1.OutputDirectory
            };
        }

        private void AddMainChinIntake(IModelDoc2 doc)
        {
            // Primero se crea un bolsillo exterior poco profundo. Al cortarlo antes que el
            // ducto queda un contorno ancho y redondeado, semejante al labio de la referencia.
            Feature pocket0 = SwGeometry.CreateEllipseSectionX(
                doc, cfg.IntakeXOuter - 0.070, 0.0, cfg.IntakeZOuter - 0.005,
                cfg.IntakeLipPocketWidth, cfg.IntakeLipPocketHeight,
                "07_TOMA_CHIN_BOLSILLO_EXTERIOR");
            Feature pocket1 = SwGeometry.CreateEllipseSectionX(
                doc, cfg.IntakeXCapture + 0.020, 0.0, cfg.IntakeZOuter,
                cfg.IntakeLipPocketWidth * 0.94, cfg.IntakeLipPocketHeight * 0.90,
                "07_TOMA_CHIN_BOLSILLO_INTERIOR");
            Feature pocketToolFeature = SwGeometry.LoftTool(doc, new[] { pocket0, pocket1 }, "07_HERRAMIENTA_BOLSILLO_LABIO_CHIN");
            Body2 mainPocket = SwGeometry.LargestSolidBody(doc);
            Body2 pocketTool = SwGeometry.BodyOf(pocketToolFeature);
            SwGeometry.SubtractBodies(doc, mainPocket, pocketTool, "07_BOLSILLO_LABIO_TOMA_CHIN");
            SwGeometry.HideFeatureSketch(doc, pocket0);
            SwGeometry.HideFeatureSketch(doc, pocket1);

            // Ducto real: boca, captura y transicion al interfaz del motor.
            Feature outside = SwGeometry.CreateEllipseSectionX(
                doc, cfg.IntakeXOuter - 0.015, 0.0, cfg.IntakeZOuter,
                cfg.IntakeWidth * 1.015, cfg.IntakeHeight * 1.015,
                "07_TOMA_CHIN_BOCA");

            Feature capture = SwGeometry.CreateEllipseSectionX(
                doc, cfg.IntakeXCapture, 0.0, cfg.IntakeZOuter + 0.010,
                cfg.IntakeWidth, cfg.IntakeHeight,
                "07_TOMA_CHIN_CAPTURA");

            Feature engine = SwGeometry.CreateEllipseSectionX(
                doc, cfg.IntakeXInterface, 0.0, cfg.IntakeZInterface,
                0.420, 0.260,
                "08_DUCTO_ADMISION_INTERFAZ_MOTOR");

            Feature toolFeature = SwGeometry.LoftTool(doc, new[] { outside, capture, engine }, "08_HERRAMIENTA_DUCTO_CHIN");
            Body2 main = SwGeometry.LargestSolidBody(doc);
            Body2 tool = SwGeometry.BodyOf(toolFeature);
            SwGeometry.SubtractBodies(doc, main, tool, "07_TOMA_CHIN_Y_08_DUCTO_INTEGRADOS");

            SwGeometry.HideFeatureSketch(doc, outside);
            SwGeometry.HideFeatureSketch(doc, capture);
            SwGeometry.HideFeatureSketch(doc, engine);

            bool leftFillet = SwGeometry.TryFilletNear(
                doc, 0.026, cfg.IntakeXCapture - 0.120, cfg.IntakeWidth * 0.47, cfg.IntakeZOuter,
                "07_LABIO_TOMA_CHIN_IZQ_R026");
            bool rightFillet = SwGeometry.TryFilletNear(
                doc, 0.026, cfg.IntakeXCapture - 0.120, -cfg.IntakeWidth * 0.47, cfg.IntakeZOuter,
                "07_LABIO_TOMA_CHIN_DER_R026");
            log(leftFillet || rightFillet ? "Labio de toma principal redondeado." : "Aviso: filetes de toma omitidos; la abertura y el ducto siguen siendo nativos.");
        }

        private void AddRecessedSideExhaust(IModelDoc2 doc, int sign, string label)
        {
            double outerY = sign * cfg.ExhaustOuterY;
            double recessInnerY = sign * cfg.ExhaustRecessInnerY;
            double innerY = sign * cfg.ExhaustInnerY;

            // Bolsillo termico exterior: crea el marco ancho y enrasado visible en la
            // referencia verde sin agregar un pod ni un tubo circular sobresaliente.
            Feature bezelOuter = SwGeometry.CreateRoundedSideSection(
                doc, outerY, cfg.ExhaustX, cfg.ExhaustZ,
                cfg.ExhaustBezelLength, cfg.ExhaustBezelHeight,
                "09_HEAT_SHIELD_" + label + "_EXTERIOR");
            Feature bezelInner = SwGeometry.CreateRoundedSideSection(
                doc, recessInnerY, cfg.ExhaustX, cfg.ExhaustZ,
                cfg.ExhaustBezelLength * 0.96, cfg.ExhaustBezelHeight * 0.94,
                "09_HEAT_SHIELD_" + label + "_INTERIOR");
            Feature bezelToolFeature = SwGeometry.LoftTool(doc, new[] { bezelOuter, bezelInner }, "09_HERRAMIENTA_RECESO_TERMICO_" + label);
            Body2 mainBezel = SwGeometry.LargestSolidBody(doc);
            Body2 bezelTool = SwGeometry.BodyOf(bezelToolFeature);
            SwGeometry.SubtractBodies(doc, mainBezel, bezelTool, "09_RECESO_HEAT_SHIELD_" + label);
            SwGeometry.HideFeatureSketch(doc, bezelOuter);
            SwGeometry.HideFeatureSketch(doc, bezelInner);

            // Conducto interno y boca D-shaped compacta.
            Feature inner = SwGeometry.CreateRoundedSideSection(
                doc, innerY, cfg.ExhaustX - 0.045, cfg.ExhaustZ + 0.010,
                cfg.ExhaustLength * 0.82, cfg.ExhaustHeight * 0.76,
                "09_ESCAPE_" + label + "_INTERFAZ");
            Feature outer = SwGeometry.CreateRoundedSideSection(
                doc, outerY + sign * 0.015, cfg.ExhaustX, cfg.ExhaustZ,
                cfg.ExhaustLength, cfg.ExhaustHeight,
                "09_ESCAPE_" + label + "_BOCA");
            Feature toolFeature = SwGeometry.LoftTool(doc, new[] { inner, outer }, "09_HERRAMIENTA_ESCAPE_" + label);
            Body2 main = SwGeometry.LargestSolidBody(doc);
            Body2 tool = SwGeometry.BodyOf(toolFeature);
            SwGeometry.SubtractBodies(doc, main, tool, "09_ESCAPE_" + label + "_INTEGRADO");
            SwGeometry.HideFeatureSketch(doc, inner);
            SwGeometry.HideFeatureSketch(doc, outer);

            bool topFillet = SwGeometry.TryFilletNear(
                doc, 0.014, cfg.ExhaustX, sign * (cfg.ExhaustOuterY - 0.025), cfg.ExhaustZ + cfg.ExhaustHeight * 0.45,
                "09_LABIO_ESCAPE_" + label + "_R014");
            log(topFillet ? "Escape " + label + " terminado con labio redondeado." : "Aviso: filete escape " + label + " omitido.");
        }

        private void AddAccessoryNacaDuct(IModelDoc2 doc, int sign, string label)
        {
            Feature outer = SwGeometry.CreateNacaSideProfile(
                doc, sign * cfg.NacaOuterY, cfg.NacaX, cfg.NacaZ,
                cfg.NacaLength, cfg.NacaHeight, 1.0,
                "10_NACA_" + label + "_BOCA_FLUSH");

            Feature inner = SwGeometry.CreateNacaSideProfile(
                doc, sign * cfg.NacaInnerY, cfg.NacaX + 0.055, cfg.NacaZ - 0.018,
                cfg.NacaLength, cfg.NacaHeight, 0.64,
                "10_NACA_" + label + "_RAMPA_INTERIOR");

            Feature toolFeature = SwGeometry.LoftTool(doc, new[] { outer, inner }, "10_HERRAMIENTA_NACA_" + label);
            Body2 main = SwGeometry.LargestSolidBody(doc);
            Body2 tool = SwGeometry.BodyOf(toolFeature);
            SwGeometry.SubtractBodies(doc, main, tool, "10_TOMA_NACA_ACCESORIOS_" + label);
            SwGeometry.HideFeatureSketch(doc, outer);
            SwGeometry.HideFeatureSketch(doc, inner);
        }

        private string BuildReport(IModelDoc2 doc)
        {
            double intakeArea = Math.PI * cfg.IntakeWidth * cfg.IntakeHeight / 4.0;
            return
                "NACELA SOLIDWORKS - VALIDACION STAGE 2 SISTEMAS EXTERNOS A2\r\n" +
                "Revision=" + cfg.Revision + "\r\n" +
                "Toma_principal=chin intake inferior con bolsillo de labio y ducto loft\r\n" +
                "Area_toma_requerida=" + F(cfg.IntakeRequiredArea) + " m2\r\n" +
                "Area_toma_eliptica=" + F(intakeArea) + " m2\r\n" +
                "Escapes=2 laterales altos D-shaped en bolsillos termicos\r\n" +
                "Escape_visible_LxH=" + F(cfg.ExhaustLength) + " x " + F(cfg.ExhaustHeight) + " m\r\n" +
                "Diametro_equivalente_previo_por_salida=" + F(cfg.ExhaustEquivalentEach) + " m PENDIENTE_DE_REVISION\r\n" +
                "Tomas_NACA=2 pequenas para accesorios; no alimentan el motor\r\n" +
                "Solidos=" + SwGeometry.SolidBodyCount(doc) + "\r\n" +
                "Superficies=" + SwGeometry.SurfaceBodyCount(doc) + "\r\n" +
                "Estado=LISTO_PARA_STAGE3_CAPOS_Y_PANELES\r\n";
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SolidWorks.Interop.sldworks;

namespace NacelleSolidWorks
{
    internal sealed class B2BuildResult
    {
        public IModelDoc2 Document;
        public string PartPath;
        public string ReportPath;
        public string OutputDirectory;
    }

    internal sealed class B2Stage1Builder
    {
        private readonly SwSession session;
        private readonly B2Config cfg;
        private readonly string output;
        private readonly Action<string> log;

        public B2Stage1Builder(SwSession session, B2Config cfg, string repositoryRoot, Action<string> log)
        {
            this.session = session;
            this.cfg = cfg;
            this.output = Path.Combine(repositoryRoot, cfg.OutputDirectory, cfg.Revision);
            this.log = log;
        }

        public B2BuildResult Build()
        {
            Directory.CreateDirectory(output);
            string partPath = Path.Combine(output, "NACELA_DERECHA_B2_STAGE1_FRENTE_SOLIDO.SLDPRT");
            session.CloseIfOpen(partPath);

            IModelDoc2 doc = session.NewPart();
            AddEquations(doc);

            Feature envelope = SwGeometry.CreateEngineEnvelopeSketch(
                doc, 0.300, 0.300 + cfg.EngineLength, cfg.EngineWidth * 0.5,
                -0.080, cfg.EngineHeight * 0.5,
                "B2_01_ENVOLVENTE_MOTOR_REFERENCIA");
            SwGeometry.HideFeatureSketch(doc, envelope);

            BuildMainOml(doc);
            ValidateSingleSolid(doc, "B2 OML principal");

            BuildSpinnerBoss(doc);
            ValidateSingleSolid(doc, "B2 frente con boss de spinner");

            BuildClosedChinIntake(doc);
            ValidateSingleSolid(doc, "B2 frente con toma inferior solida");

            BuildWingSaddle(doc);
            ValidateSingleSolid(doc, "B2 frente y saddle");

            Feature axis = SwGeometry.CreateAxisSketch(doc, -0.120, cfg.XAft + 0.180, 0.0, 0.0, "B2_00_EJE_MOTOR_LOCAL");
            SwGeometry.HideFeatureSketch(doc, axis);

            doc.MaterialPropertyValues = new[] { cfg.SkinR, cfg.SkinG, cfg.SkinB, 1.0, 1.0, 0.42, 0.22, 0.0, 0.0 };
            SwGeometry.HideConstruction(doc);
            doc.EditRebuild3();
            ValidateSingleSolid(doc, "B2 Stage 1 final");

            int saveError = doc.SaveAs3(partPath, 0, 2);
            if (saveError != 0) throw new IOException("No se pudo guardar B2 Stage 1. Error=" + saveError);

            SaveReviewViews(doc, output, "NACELA_DERECHA_B2_STAGE1_FRENTE_SOLIDO");
            string reportPath = Path.Combine(output, "VALIDACION_B2_STAGE1.txt");
            File.WriteAllText(reportPath, BuildReport(doc));
            log("B2 Stage 1 creado desde cero: " + partPath);
            log("B2 mantiene boss y toma completamente cerrados; no hay caladuras ni shell.");

            return new B2BuildResult
            {
                Document = doc,
                PartPath = partPath,
                ReportPath = reportPath,
                OutputDirectory = output
            };
        }

        private void BuildMainOml(IModelDoc2 doc)
        {
            List<Feature> profiles = new List<Feature>();
            foreach (B2OmlSection section in cfg.OmlSections)
                profiles.Add(B2Geometry.CreateOmlSection(doc, section, "B2_02_OML_" + section.Name));

            List<Feature> guides = new List<Feature>
            {
                B2Geometry.CreateGuide(doc, cfg.OmlSections, 0, "B2_03_GUIA_CORONA"),
                B2Geometry.CreateGuide(doc, cfg.OmlSections, 1, "B2_03_GUIA_PANZA"),
                B2Geometry.CreateGuide(doc, cfg.OmlSections, 2, "B2_03_GUIA_EXTERIOR"),
                B2Geometry.CreateGuide(doc, cfg.OmlSections, 3, "B2_03_GUIA_INTERIOR")
            };

            SwGeometry.LoftWithGuides(doc, profiles, guides, "B2_04_OML_PRINCIPAL_LIMPIA");
            HideAll(doc, profiles);
            HideAll(doc, guides);
        }

        private void BuildSpinnerBoss(IModelDoc2 doc)
        {
            List<Feature> profiles = new List<Feature>();
            foreach (B2OmlSection section in cfg.NoseSections)
                profiles.Add(B2Geometry.CreateOmlSection(doc, section, "B2_05_NARIZ_" + section.Name));

            SwGeometry.SimpleMergedLoft(doc, profiles, "B2_05_BOSS_SPINNER_INTEGRADO");
            HideAll(doc, profiles);

            bool filletFront = SwGeometry.TryFilletNear(doc, 0.030, 0.055, 0.155, 0.000, "B2_05_FILETE_FLANGE_R030");
            bool filletBlend = SwGeometry.TryFilletNear(doc, 0.060, 0.330, 0.330, -0.070, "B2_05_FILETE_NARIZ_R060");
            log(filletFront || filletBlend
                ? "B2: boss circular y transicion oval integrados con filetes."
                : "B2: boss circular y transicion oval integrados por loft continuo.");
        }

        private void BuildClosedChinIntake(IModelDoc2 doc)
        {
            List<Feature> profiles = new List<Feature>();
            foreach (B2RoundedSection section in cfg.IntakeSections)
                profiles.Add(B2Geometry.CreateRoundedRectangleSectionX(doc, section, "B2_06_TOMA_SOLIDA_" + section.Name));

            SwGeometry.SimpleMergedLoft(doc, profiles, "B2_06_TOMA_INFERIOR_RECTANGULAR_OVALADA_SOLIDA");
            HideAll(doc, profiles);

            bool left = SwGeometry.TryFilletNear(doc, 0.035, 0.185, 0.205, -0.500, "B2_06_FILETE_TOMA_IZQUIERDO_R035");
            bool right = SwGeometry.TryFilletNear(doc, 0.035, 0.185, -0.205, -0.500, "B2_06_FILETE_TOMA_DERECHO_R035");
            bool aft = SwGeometry.TryFilletNear(doc, 0.055, 0.760, 0.250, -0.500, "B2_06_FILETE_TOMA_TRASERO_R055");
            log(left || right || aft
                ? "B2: toma inferior cerrada y blendada con la panza."
                : "B2: toma inferior cerrada fusionada mediante loft continuo.");
        }

        private void BuildWingSaddle(IModelDoc2 doc)
        {
            List<Feature> profiles = new List<Feature>();
            foreach (B2SaddleSection section in cfg.SaddleSections)
                profiles.Add(B2Geometry.CreateSaddleSection(doc, section, "B2_07_SADDLE_" + section.Name));

            SwGeometry.SimpleMergedLoft(doc, profiles, "B2_07_FAIRING_SUPERIOR_SUAVE");
            HideAll(doc, profiles);

            bool front = SwGeometry.TryFilletNear(doc, 0.050, 0.820, 0.260, 0.500, "B2_07_FILETE_SADDLE_DELANTERO");
            bool aft = SwGeometry.TryFilletNear(doc, 0.060, 2.080, 0.220, 0.520, "B2_07_FILETE_SADDLE_TRASERO");
            log(front || aft ? "B2: saddle superior blendado." : "B2: saddle superior loft continuo.");
        }

        private void AddEquations(IModelDoc2 doc)
        {
            IEquationMgr eq = (IEquationMgr)doc.GetEquationMgr();
            Add(eq, "B2_X_MONTAJE", F(cfg.AssemblyX) + "m");
            Add(eq, "B2_Y_MOTOR", F(cfg.YMotor) + "m");
            Add(eq, "B2_Z_EJE", F(cfg.ZAxis) + "m");
            Add(eq, "B2_L_NAC", F(cfg.Length) + "m");
            Add(eq, "B2_W_NAC_MAX", F(cfg.MaxWidth) + "m");
            Add(eq, "B2_H_NAC_OBJETIVO", F(cfg.MaxHeight) + "m");
            Add(eq, "B2_C_LOCAL", F(cfg.LocalChord) + "m");
            Add(eq, "B2_AREA_CAPTURA_FUTURA", F(cfg.IntakeRequiredArea) + "m^2");
        }

        private static void Add(IEquationMgr eq, string name, string value)
        {
            eq.Add2(-1, "\"" + name + "\" = " + value, true);
        }

        private string BuildReport(IModelDoc2 doc)
        {
            double[] box = SwGeometry.BoundingBox(doc);
            double aftRatio = (cfg.GlobalAft - cfg.LeadingEdgeX) / cfg.LocalChord;
            return
                "NACELA B2 - STAGE 1 FRENTE SOLIDO DESDE CERO\r\n" +
                "Revision=" + cfg.Revision + "\r\n" +
                "Estado_frente=SOLIDO_CERRADO_SIN_CALADURAS\r\n" +
                "Boss_spinner=circulo a elipses mediante loft fusionado\r\n" +
                "Toma_inferior=rectangulo redondeado solido mediante 8 secciones\r\n" +
                "Shell=NO_EJECUTADO\r\n" +
                "Conductos=NO_EJECUTADOS\r\n" +
                "Bounding_L=" + F(box[3] - box[0]) + " m\r\n" +
                "Bounding_W=" + F(box[4] - box[1]) + " m\r\n" +
                "Bounding_H=" + F(box[5] - box[2]) + " m\r\n" +
                "Montaje_X=" + F(cfg.AssemblyX) + " m\r\n" +
                "Montaje_Y=" + F(cfg.YMotor) + " m\r\n" +
                "Montaje_Z=" + F(cfg.ZAxis) + " m\r\n" +
                "X_aft_sobre_c=" + F(aftRatio) + "\r\n" +
                "Solidos=" + SwGeometry.SolidBodyCount(doc) + "\r\n" +
                "Superficies=" + SwGeometry.SurfaceBodyCount(doc) + "\r\n";
        }

        private static void HideAll(IModelDoc2 doc, IEnumerable<Feature> features)
        {
            foreach (Feature feature in features) SwGeometry.HideFeatureSketch(doc, feature);
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
            doc.SaveBMP(path, 1800, 1100);
        }

        private static string F(double value) { return value.ToString("0.000000", CultureInfo.InvariantCulture); }
    }
}

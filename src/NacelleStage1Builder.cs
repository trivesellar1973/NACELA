using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SolidWorks.Interop.sldworks;

namespace NacelleSolidWorks
{
    internal sealed class BuildResult
    {
        public IModelDoc2 Document;
        public string PartPath;
        public string LogPath;
        public string OutputDirectory;
    }

    internal sealed class NacelleStage1Builder
    {
        private readonly SwSession session;
        private readonly NacelleConfig cfg;
        private readonly string output;
        private readonly Action<string> log;

        public NacelleStage1Builder(SwSession session, NacelleConfig cfg, string repositoryRoot, Action<string> log)
        {
            this.session = session;
            this.cfg = cfg;
            this.output = Path.Combine(repositoryRoot, cfg.OutputDirectory, cfg.Revision);
            this.log = log;
        }

        public BuildResult Build()
        {
            Directory.CreateDirectory(output);
            string partPath = Path.Combine(output, "NACELA_DERECHA_" + cfg.Revision + "_STAGE1_OML.SLDPRT");
            session.CloseIfOpen(partPath);

            IModelDoc2 doc = session.NewPart();
            AddEquations(doc);

            // Envolvente interna de comprobacion. Es un croquis de construccion,
            // no un cuerpo visible ni un motor ficticio.
            double engineX1 = 0.300;
            double engineX2 = engineX1 + cfg.EngineLength;
            Feature envelope = SwGeometry.CreateEngineEnvelopeSketch(
                doc, engineX1, engineX2, cfg.EngineWidth * 0.5,
                -0.100, cfg.EngineHeight * 0.5,
                "01_ENVOLVENTE_MOTOR_PW127XT_M");
            SwGeometry.HideFeatureSketch(doc, envelope);

            List<Feature> profiles = new List<Feature>();
            foreach (NacelleSection section in cfg.Sections)
                profiles.Add(SwGeometry.CreateClosedSection(doc, section, 0.0, "02_OML_" + section.Name));

            List<Feature> guides = new List<Feature>
            {
                SwGeometry.CreateGuide(doc, cfg.Sections, 0.0, 0, "03_GUIA_CORONA"),
                SwGeometry.CreateGuide(doc, cfg.Sections, 0.0, 1, "03_GUIA_PANZA"),
                SwGeometry.CreateGuide(doc, cfg.Sections, 0.0, 2, "03_GUIA_EXTERIOR"),
                SwGeometry.CreateGuide(doc, cfg.Sections, 0.0, 3, "03_GUIA_INTERIOR")
            };

            SwGeometry.LoftWithGuides(doc, profiles, guides, "04_CUERPO_PRINCIPAL_OML_" + cfg.Revision);
            foreach (Feature feature in profiles) SwGeometry.HideFeatureSketch(doc, feature);
            foreach (Feature feature in guides) SwGeometry.HideFeatureSketch(doc, feature);

            AddFrontGearboxBoss(doc);
            AddUpperFairing(doc);

            Feature axis = SwGeometry.CreateAxisSketch(doc, -0.20, cfg.XAft + 0.20, 0.0, 0.0, "00_REF_EJE_MOTOR_LOCAL");
            SwGeometry.HideFeatureSketch(doc, axis);

            doc.MaterialPropertyValues = new[] { cfg.SkinR, cfg.SkinG, cfg.SkinB, 1.0, 1.0, 0.40, 0.20, 0.0, 0.0 };
            SwGeometry.HideConstruction(doc);
            doc.EditRebuild3();

            ValidateSingleSolid(doc, "Stage 1");
            double[] box = SwGeometry.BoundingBox(doc);
            double lx = box[3] - box[0];
            double wy = box[4] - box[1];
            double hz = box[5] - box[2];

            doc.ShowNamedView2("*Isometric", 7);
            doc.ViewZoomtofit2();
            int saveError = doc.SaveAs3(partPath, 0, 2);
            if (saveError != 0) throw new IOException("No se pudo guardar Stage 1. Error=" + saveError);

            SaveReviewViews(doc, output, "NACELA_DERECHA_" + cfg.Revision + "_STAGE1");
            string reportPath = Path.Combine(output, "VALIDACION_STAGE1_" + cfg.Revision + ".txt");
            File.WriteAllText(reportPath, BuildReport(lx, wy, hz, SwGeometry.SolidBodyCount(doc), SwGeometry.SurfaceBodyCount(doc)));

            log("Stage 1 OML creado: " + partPath);
            log("Bounding box local: L=" + F(lx) + " W=" + F(wy) + " H=" + F(hz));
            return new BuildResult { Document = doc, PartPath = partPath, LogPath = reportPath, OutputDirectory = output };
        }

        private void AddFrontGearboxBoss(IModelDoc2 doc)
        {
            Feature p0 = SwGeometry.CreateCircleSectionX(doc, 0.000, 0.0, 0.0, 0.340, "05_BOSS_FRONTAL_D340");
            Feature p1 = SwGeometry.CreateCircleSectionX(doc, 0.055, 0.0, 0.0, 0.420, "05_FLANGE_FRONTAL_D420");
            Feature p2 = SwGeometry.CreateEllipseSectionX(doc, 0.180, 0.0, -0.005, 0.500, 0.520, "05_TRANSICION_GEARBOX");
            SwGeometry.SimpleMergedLoft(doc, new[] { p0, p1, p2 }, "05_BOSS_Y_TRANSICION_GEARBOX");
            SwGeometry.HideFeatureSketch(doc, p0);
            SwGeometry.HideFeatureSketch(doc, p1);
            SwGeometry.HideFeatureSketch(doc, p2);

            Feature h0 = SwGeometry.CreateCircleSectionX(doc, -0.020, 0.0, 0.0, 0.120, "05_HUECO_EJE_FRENTE");
            Feature h1 = SwGeometry.CreateCircleSectionX(doc, 0.240, 0.0, 0.0, 0.120, "05_HUECO_EJE_INTERIOR");
            Feature toolFeature = SwGeometry.LoftTool(doc, new[] { h0, h1 }, "05_HERRAMIENTA_HUECO_EJE");
            Body2 main = SwGeometry.LargestSolidBody(doc);
            Body2 tool = SwGeometry.BodyOf(toolFeature);
            SwGeometry.SubtractBodies(doc, main, tool, "05_HUECO_CENTRAL_EJE_D120");
            SwGeometry.HideFeatureSketch(doc, h0);
            SwGeometry.HideFeatureSketch(doc, h1);

            bool fillet = SwGeometry.TryFilletNear(doc, 0.040, 0.080, 0.205, 0.0, "05_FILETE_BOSS_R040");
            log(fillet ? "Filete del boss creado." : "Aviso: filete del boss omitido; la transicion loft permanece continua.");
        }

        private void AddUpperFairing(IModelDoc2 doc)
        {
            List<Feature> fairingProfiles = new List<Feature>();
            foreach (FairingSection section in cfg.FairingSections)
                fairingProfiles.Add(SwGeometry.CreateFairingSection(doc, section, 0.0, "06_FAIRING_" + section.Name));

            SwGeometry.SimpleMergedLoft(doc, fairingProfiles, "06_FAIRING_SUPERIOR_CORTO_" + cfg.Revision);
            foreach (Feature feature in fairingProfiles) SwGeometry.HideFeatureSketch(doc, feature);

            bool fillet = SwGeometry.TryFilletNear(doc, 0.030, 1.45, 0.22, 0.39, "06_FILETE_FAIRING_R030");
            log(fillet ? "Filete del fairing creado." : "Aviso: filete del fairing omitido; no se agrego geometria de parche.");
        }

        private void AddEquations(IModelDoc2 doc)
        {
            IEquationMgr eq = (IEquationMgr)doc.GetEquationMgr();
            Add(eq, "X_LE_MOTOR_GLOBAL", F(cfg.LeadingEdgeX) + "m");
            Add(eq, "X_PROP_ADELANTE_BA", F(cfg.PropPlaneAheadLeadingEdge) + "m");
            Add(eq, "X_MONTAJE_NACELA", F(cfg.AssemblyX) + "m");
            Add(eq, "X_NAC_FWD_LOCAL", F(cfg.XFront) + "m");
            Add(eq, "X_NAC_AFT_LOCAL", F(cfg.XAft) + "m");
            Add(eq, "L_NAC", F(cfg.Length) + "m");
            Add(eq, "W_NAC_MAX", F(cfg.MaxWidth) + "m");
            Add(eq, "H_NAC_MAX", F(cfg.MaxHeight) + "m");
            Add(eq, "Y_MOTOR_DER", F(cfg.YMotor) + "m");
            Add(eq, "Z_EJE_MOTOR_GLOBAL", F(cfg.ZAxis) + "m");
            Add(eq, "GAP_ALA_NACELA", F(cfg.WingGap) + "m");
            Add(eq, "CUERDA_LOCAL", F(cfg.LocalChord) + "m");
            Add(eq, "L_ENV_MOTOR", F(cfg.EngineLength) + "m");
            Add(eq, "W_ENV_MOTOR", F(cfg.EngineWidth) + "m");
            Add(eq, "H_ENV_MOTOR", F(cfg.EngineHeight) + "m");
        }

        private static void Add(IEquationMgr mgr, string name, string value)
        {
            mgr.Add2(-1, "\"" + name + "\" = " + value, true);
        }

        private string BuildReport(double lx, double wy, double hz, int solids, int surfaces)
        {
            return
                "NACELA SOLIDWORKS - VALIDACION STAGE 1 OML\r\n" +
                "Revision=" + cfg.Revision + "\r\n" +
                "Metodo=Loft solido nativo con 10 perfiles cerrados y 4 guias continuas\r\n" +
                "Sistema_local=Origen en plano de helice y eje del motor\r\n" +
                "Montaje_global_X=" + F(cfg.AssemblyX) + " m\r\n" +
                "Montaje_global_Y=" + F(cfg.YMotor) + " m\r\n" +
                "Montaje_global_Z=" + F(cfg.ZAxis) + " m\r\n" +
                "X_global_nacela=[" + F(cfg.GlobalFront) + "," + F(cfg.GlobalAft) + "] m\r\n" +
                "BoundingBox_L=" + F(lx) + " m\r\n" +
                "BoundingBox_W=" + F(wy) + " m\r\n" +
                "BoundingBox_H=" + F(hz) + " m\r\n" +
                "Solidos=" + solids + "\r\n" +
                "Superficies=" + surfaces + "\r\n" +
                "Estado=OML_LISTO_PARA_STAGE2\r\n";
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

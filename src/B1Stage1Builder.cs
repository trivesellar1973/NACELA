using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SolidWorks.Interop.sldworks;

namespace NacelleSolidWorks
{
    internal sealed class B1BuildResult
    {
        public IModelDoc2 Document;
        public string PartPath;
        public string ReportPath;
        public string OutputDirectory;
    }

    internal sealed class B1Stage1Builder
    {
        private readonly SwSession session;
        private readonly B1Config cfg;
        private readonly string output;
        private readonly Action<string> log;

        public B1Stage1Builder(SwSession session, B1Config cfg, string repositoryRoot, Action<string> log)
        {
            this.session = session;
            this.cfg = cfg;
            this.output = Path.Combine(repositoryRoot, cfg.OutputDirectory, cfg.Revision);
            this.log = log;
        }

        public B1BuildResult Build()
        {
            Directory.CreateDirectory(output);
            string partPath = Path.Combine(output, "NACELA_DERECHA_B1_STAGE1_OML.SLDPRT");
            session.CloseIfOpen(partPath);

            IModelDoc2 doc = session.NewPart();
            AddEquations(doc);

            Feature envelope = SwGeometry.CreateEngineEnvelopeSketch(
                doc, 0.250, 0.250 + cfg.EngineLength, cfg.EngineWidth * 0.5,
                -0.100, cfg.EngineHeight * 0.5,
                "B1_01_ENVOLVENTE_PW127XT_M");
            SwGeometry.HideFeatureSketch(doc, envelope);

            List<Feature> profiles = new List<Feature>();
            foreach (B1Section section in cfg.Sections)
                profiles.Add(B1Geometry.CreateClosedSection(doc, section, "B1_02_OML_" + section.Name));

            List<Feature> guides = new List<Feature>
            {
                B1Geometry.CreateGuide(doc, cfg.Sections, 0, "B1_03_GUIA_CORONA"),
                B1Geometry.CreateGuide(doc, cfg.Sections, 1, "B1_03_GUIA_PANZA"),
                B1Geometry.CreateGuide(doc, cfg.Sections, 2, "B1_03_GUIA_EXTERIOR"),
                B1Geometry.CreateGuide(doc, cfg.Sections, 3, "B1_03_GUIA_INTERIOR")
            };

            SwGeometry.LoftWithGuides(doc, profiles, guides, "B1_04_OML_REFERENCIA_VERDE");
            HideAll(doc, profiles);
            HideAll(doc, guides);
            ValidateSingleSolid(doc, "B1 OML");

            AddGearboxNose(doc);
            ValidateSingleSolid(doc, "B1 OML con gearbox");

            AddWingSaddle(doc);
            ValidateSingleSolid(doc, "B1 OML con saddle");

            Feature axis = SwGeometry.CreateAxisSketch(doc, -0.120, cfg.XAft + 0.180, 0.0, 0.0, "B1_00_EJE_MOTOR_LOCAL");
            SwGeometry.HideFeatureSketch(doc, axis);

            doc.MaterialPropertyValues = new[] { cfg.SkinR, cfg.SkinG, cfg.SkinB, 1.0, 1.0, 0.42, 0.22, 0.0, 0.0 };
            SwGeometry.HideConstruction(doc);
            doc.EditRebuild3();

            ValidateSingleSolid(doc, "B1 Stage 1 final");
            int saveError = doc.SaveAs3(partPath, 0, 2);
            if (saveError != 0) throw new IOException("No se pudo guardar B1 Stage 1. Error=" + saveError);

            SaveReviewViews(doc, output, "NACELA_DERECHA_B1_STAGE1");
            string reportPath = Path.Combine(output, "VALIDACION_B1_STAGE1.txt");
            File.WriteAllText(reportPath, BuildReport(doc));
            log("B1 Stage 1 creado desde cero: " + partPath);

            return new B1BuildResult
            {
                Document = doc,
                PartPath = partPath,
                ReportPath = reportPath,
                OutputDirectory = output
            };
        }

        private void AddGearboxNose(IModelDoc2 doc)
        {
            Feature p0 = SwGeometry.CreateCircleSectionX(doc, 0.000, 0.0, 0.000, 0.240, "B1_05_BOSS_FRENTE_D240");
            Feature p1 = SwGeometry.CreateCircleSectionX(doc, 0.040, 0.0, 0.000, 0.320, "B1_05_FLANGE_D320");
            Feature p2 = SwGeometry.CreateEllipseSectionX(doc, 0.105, 0.0, 0.000, 0.420, 0.470, "B1_05_NARIZ_1");
            Feature p3 = SwGeometry.CreateEllipseSectionX(doc, 0.190, 0.0, -0.025, 0.570, 0.690, "B1_05_NARIZ_2");
            Feature p4 = SwGeometry.CreateEllipseSectionX(doc, 0.300, 0.0, -0.070, 0.730, 0.930, "B1_05_NARIZ_3");
            SwGeometry.SimpleMergedLoft(doc, new[] { p0, p1, p2, p3, p4 }, "B1_05_GEARBOX_Y_MORRO");
            HideAll(doc, new[] { p0, p1, p2, p3, p4 });

            Feature h0 = SwGeometry.CreateCircleSectionX(doc, -0.010, 0.0, 0.0, 0.105, "B1_05_EJE_HUECO_FRENTE");
            Feature h1 = SwGeometry.CreateCircleSectionX(doc, 0.300, 0.0, 0.0, 0.105, "B1_05_EJE_HUECO_INTERIOR");
            Feature toolFeature = SwGeometry.LoftTool(doc, new[] { h0, h1 }, "B1_05_HERRAMIENTA_EJE");
            SwGeometry.SubtractBodies(doc, SwGeometry.LargestSolidBody(doc), SwGeometry.BodyOf(toolFeature), "B1_05_HUECO_EJE");
            HideAll(doc, new[] { h0, h1 });

            bool f1 = SwGeometry.TryFilletNear(doc, 0.030, 0.075, 0.170, 0.000, "B1_05_FILETE_FLANGE_R030");
            bool f2 = SwGeometry.TryFilletNear(doc, 0.050, 0.245, 0.300, -0.040, "B1_05_FILETE_MORRO_R050");
            log((f1 || f2) ? "B1: transiciones de gearbox redondeadas." : "B1: loft del morro conservado sin filete adicional.");
        }

        private void AddWingSaddle(IModelDoc2 doc)
        {
            List<Feature> saddleProfiles = new List<Feature>();
            foreach (B1SaddleSection section in cfg.SaddleSections)
                saddleProfiles.Add(B1Geometry.CreateSaddleSection(doc, section, "B1_06_SADDLE_" + section.Name));

            SwGeometry.SimpleMergedLoft(doc, saddleProfiles, "B1_06_FAIRING_ALA_NACELA_PLANO");
            HideAll(doc, saddleProfiles);

            bool front = SwGeometry.TryFilletNear(doc, 0.055, 0.820, 0.300, 0.520, "B1_06_FILETE_SADDLE_DELANTERO");
            bool aft = SwGeometry.TryFilletNear(doc, 0.065, 2.150, 0.230, 0.520, "B1_06_FILETE_SADDLE_TRASERO");
            log(front || aft ? "B1: saddle fairing integrado al OML." : "B1: saddle fairing loft continuo sin filetes suplementarios.");
        }

        private void AddEquations(IModelDoc2 doc)
        {
            IEquationMgr eq = (IEquationMgr)doc.GetEquationMgr();
            Add(eq, "B1_X_MONTAJE", F(cfg.AssemblyX) + "m");
            Add(eq, "B1_Y_MOTOR", F(cfg.YMotor) + "m");
            Add(eq, "B1_Z_EJE", F(cfg.ZAxis) + "m");
            Add(eq, "B1_L_NAC", F(cfg.Length) + "m");
            Add(eq, "B1_W_NAC", F(cfg.MaxWidth) + "m");
            Add(eq, "B1_H_NAC", F(cfg.MaxHeight) + "m");
            Add(eq, "B1_C_LOCAL", F(cfg.LocalChord) + "m");
            Add(eq, "B1_AREA_CAPTURA", F(cfg.IntakeRequiredArea) + "m^2");
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
                "NACELA B1 - STAGE 1 OML DESDE CERO\r\n" +
                "Revision=" + cfg.Revision + "\r\n" +
                "Metodo=12 secciones nuevas + 4 guias + saddle con techo plano\r\n" +
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
            doc.SaveBMP(path, 1600, 1000);
        }

        private static string F(double value) { return value.ToString("0.000000", CultureInfo.InvariantCulture); }
    }
}

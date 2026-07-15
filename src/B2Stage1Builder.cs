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

            BuildUnifiedNoseAndOml(doc);
            ValidateSingleSolid(doc, "B2 nariz y OML unificadas");

            BuildClosedChinIntake(doc);
            ValidateSingleSolid(doc, "B2 frente con toma inferior solida");

            // Esta revision se concentra solo en el frente. El saddle anterior no se
            // reconstruye aqui para que una operacion secundaria no impida guardar la
            // nariz y la toma ya corregidas.
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
            log("B2 Stage 1 corregido y guardado: " + partPath);
            log("B2: boss circular integrado dentro del loft principal; toma inferior cerrada sumada por Combine/Add.");

            return new B2BuildResult
            {
                Document = doc,
                PartPath = partPath,
                ReportPath = reportPath,
                OutputDirectory = output
            };
        }

        private void BuildUnifiedNoseAndOml(IModelDoc2 doc)
        {
            // El fallo anterior ocurria al crear la nariz como un segundo loft con
            // Merge result. Ahora el circulo frontal, las elipses de transicion y la
            // OML posterior pertenecen a una sola secuencia ordenada de perfiles.
            List<B2OmlSection> sections = new List<B2OmlSection>();

            foreach (B2OmlSection section in cfg.NoseSections)
                if (section.X <= 0.340001) sections.Add(section);

            foreach (B2OmlSection section in cfg.OmlSections)
                if (section.X >= 0.439999) sections.Add(section);

            sections.Sort(delegate(B2OmlSection a, B2OmlSection b) { return a.X.CompareTo(b.X); });
            if (sections.Count < 12) throw new InvalidOperationException("Faltan perfiles para el loft unificado B2");

            List<Feature> profiles = new List<Feature>();
            List<Feature> guides = new List<Feature>();
            try
            {
                foreach (B2OmlSection section in sections)
                    profiles.Add(B2Geometry.CreateOmlSection(doc, section, "B2_02_FRENTE_OML_" + section.Name));

                guides.Add(B2Geometry.CreateGuide(doc, sections, 0, "B2_03_GUIA_CORONA_UNIFICADA"));
                guides.Add(B2Geometry.CreateGuide(doc, sections, 1, "B2_03_GUIA_PANZA_UNIFICADA"));
                guides.Add(B2Geometry.CreateGuide(doc, sections, 2, "B2_03_GUIA_EXTERIOR_UNIFICADA"));
                guides.Add(B2Geometry.CreateGuide(doc, sections, 3, "B2_03_GUIA_INTERIOR_UNIFICADA"));

                try
                {
                    SwGeometry.LoftWithGuides(doc, profiles, guides, "B2_04_NARIZ_CIRCULAR_A_OML_UNIFICADA");
                }
                catch (Exception guidedError)
                {
                    log("B2: loft con guias no aceptado; se usa loft de perfiles. Detalle: " + guidedError.Message);
                    doc.ClearSelection2(true);
                    SwGeometry.SimpleMergedLoft(doc, profiles, "B2_04_NARIZ_CIRCULAR_A_OML_UNIFICADA_FALLBACK");
                }
            }
            finally
            {
                HideAll(doc, profiles);
                HideAll(doc, guides);
            }

            bool frontFillet = SwGeometry.TryFilletNear(doc, 0.018, 0.010, 0.135, 0.000, "B2_04_FILETE_CARA_SPINNER_R018");
            log(frontFillet
                ? "B2: borde frontal del boss suavizado."
                : "B2: frente continuo generado sin filete suplementario.");
        }

        private void BuildClosedChinIntake(IModelDoc2 doc)
        {
            List<Feature> profiles = new List<Feature>();
            Feature intakeLoft = null;
            try
            {
                foreach (B2RoundedSection section in cfg.IntakeSections)
                    profiles.Add(B2Geometry.CreateRoundedRectangleSectionX(doc, section, "B2_05_TOMA_CERRADA_" + section.Name));

                // Se genera como cuerpo independiente para que SolidWorks no tenga que
                // resolver simultaneamente el loft y la fusion. Luego se suma por Combine.
                intakeLoft = SwGeometry.LoftTool(doc, profiles, "B2_05_CUERPO_TOMA_RECTANGULAR_OVALADA");
            }
            finally
            {
                HideAll(doc, profiles);
            }

            if (intakeLoft == null) throw new InvalidOperationException("No se creo el cuerpo cerrado de la toma inferior");
            if (SwGeometry.SolidBodyCount(doc) < 2)
                throw new InvalidOperationException("La toma no se genero como segundo cuerpo solido");

            Body2 mainBody = SwGeometry.LargestSolidBody(doc);
            Body2 intakeBody = SwGeometry.BodyOf(intakeLoft);
            B2BodyOps.AddBodies(doc, mainBody, intakeBody, "B2_06_UNION_OML_CON_TOMA_INFERIOR");

            bool left = SwGeometry.TryFilletNear(doc, 0.025, 0.220, 0.205, -0.430, "B2_06_FILETE_LABIO_IZQUIERDO_R025");
            bool right = SwGeometry.TryFilletNear(doc, 0.025, 0.220, -0.205, -0.430, "B2_06_FILETE_LABIO_DERECHO_R025");
            bool aft = SwGeometry.TryFilletNear(doc, 0.045, 0.840, 0.230, -0.455, "B2_06_FILETE_BLEND_TRASERO_R045");
            log(left || right || aft
                ? "B2: toma inferior solida unida y blendada con la panza."
                : "B2: toma inferior solida unida por Combine sin filetes adicionales.");
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
                "NACELA B2 - STAGE 1 FRENTE SOLIDO CORREGIDO\r\n" +
                "Revision=" + cfg.Revision + "\r\n" +
                "Nariz=UN_SOLO_LOFT_DESDE_CIRCULO_HASTA_OML\r\n" +
                "Toma_inferior=SUPERELIPSE_CERRADA_MAS_COMBINE_ADD\r\n" +
                "Saddle=POSPUESTO_PARA_REVISION_POSTERIOR\r\n" +
                "Shell=NO_EJECUTADO\r\n" +
                "Caladuras=NO_EJECUTADAS\r\n" +
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
            foreach (Feature feature in features)
                if (feature != null) SwGeometry.HideFeatureSketch(doc, feature);
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

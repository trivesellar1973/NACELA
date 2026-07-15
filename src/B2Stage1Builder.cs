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

            // La pieza verde se construye deliberadamente con tres operaciones simples:
            // cuerpo central, boss circular superior y toma rectangular redondeada inferior.
            // Cada operacion genera un solido independiente y la union se hace despues con
            // Combine/Add. No se usan guias, filetes automaticos ni un unico loft gigante.
            BuildCentralGreenBody(doc);
            ValidateSingleSolid(doc, "B2 cuerpo central verde");

            BuildCircularSpinnerBoss(doc);
            ValidateSingleSolid(doc, "B2 cuerpo con boss circular");

            BuildRoundedChinIntake(doc);
            ValidateSingleSolid(doc, "B2 cuerpo con toma inferior");

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
            log("B2 Stage 1 guardado: " + partPath);
            log("B2: cuerpo verde simple; circulo y toma inferior unidos al cuerpo central mediante lofts separados y Combine/Add.");

            return new B2BuildResult
            {
                Document = doc,
                PartPath = partPath,
                ReportPath = reportPath,
                OutputDirectory = output
            };
        }

        private void BuildCentralGreenBody(IModelDoc2 doc)
        {
            List<Feature> profiles = new List<Feature>();
            try
            {
                // Todas las secciones son elipses verdaderas con la misma costura en +Y.
                // Esto evita los quiebres y torsiones que aparecian con superelipses,
                // exponentes variables y cuatro curvas guia simultaneas.
                foreach (B2OmlSection section in cfg.OmlSections)
                {
                    profiles.Add(SwGeometry.CreateEllipseSectionX(
                        doc, section.X, 0.0, section.ZCenter,
                        section.Width, section.Height,
                        "B2_02_CUERPO_CENTRAL_" + section.Name));
                }

                SwGeometry.SimpleMergedLoft(doc, profiles, "B2_03_CUERPO_CENTRAL_VERDE_SIMPLE");
            }
            finally
            {
                HideAll(doc, profiles);
            }
        }

        private void BuildCircularSpinnerBoss(IModelDoc2 doc)
        {
            List<Feature> profiles = new List<Feature>();
            Feature noseLoft = null;
            try
            {
                foreach (B2OmlSection section in cfg.NoseSections)
                {
                    profiles.Add(SwGeometry.CreateEllipseSectionX(
                        doc, section.X, 0.0, section.ZCenter,
                        section.Width, section.Height,
                        "B2_04_NARIZ_CIRCULAR_" + section.Name));
                }

                // El primer perfil es el circulo frontal. Los siguientes solo hacen la
                // transicion progresiva hasta penetrar en el cuerpo central.
                noseLoft = SwGeometry.LoftTool(doc, profiles, "B2_05_LOFT_CIRCULO_A_CUERPO_CENTRAL");
            }
            finally
            {
                HideAll(doc, profiles);
            }

            if (noseLoft == null) throw new InvalidOperationException("No se creo el loft circular frontal");
            if (SwGeometry.SolidBodyCount(doc) < 2)
                throw new InvalidOperationException("La nariz circular no se genero como segundo cuerpo solido");

            Body2 mainBody = SwGeometry.LargestSolidBody(doc);
            Body2 noseBody = SwGeometry.BodyOf(noseLoft);
            B2BodyOps.AddBodies(doc, mainBody, noseBody, "B2_06_UNION_CIRCULO_CON_CUERPO_CENTRAL");
        }

        private void BuildRoundedChinIntake(IModelDoc2 doc)
        {
            List<Feature> profiles = new List<Feature>();
            Feature intakeLoft = null;
            try
            {
                foreach (B2RoundedSection section in cfg.IntakeSections)
                {
                    profiles.Add(B2Geometry.CreateRoundedRectangleSectionX(
                        doc, section, "B2_07_TOMA_RECTANGULAR_" + section.Name));
                }

                // La boca permanece cerrada. El ultimo perfil penetra en la panza para
                // que la union booleana sea estable y no deje una arista o cuello raro.
                intakeLoft = SwGeometry.LoftTool(doc, profiles, "B2_08_LOFT_RECTANGULO_REDONDEADO_A_PANZA");
            }
            finally
            {
                HideAll(doc, profiles);
            }

            if (intakeLoft == null) throw new InvalidOperationException("No se creo el loft de la toma inferior");
            if (SwGeometry.SolidBodyCount(doc) < 2)
                throw new InvalidOperationException("La toma inferior no se genero como segundo cuerpo solido");

            Body2 mainBody = SwGeometry.LargestSolidBody(doc);
            Body2 intakeBody = SwGeometry.BodyOf(intakeLoft);
            B2BodyOps.AddBodies(doc, mainBody, intakeBody, "B2_09_UNION_TOMA_CON_PANZA");
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
                "NACELA B2 - STAGE 1 CUERPO VERDE SIMPLE\r\n" +
                "Revision=" + cfg.Revision + "\r\n" +
                "Cuerpo_central=LOFT_ELIPTICO_SIN_GUIAS\r\n" +
                "Nariz=LOFT_SEPARADO_CIRCULO_A_ELIPSES_MAS_COMBINE_ADD\r\n" +
                "Toma_inferior=LOFT_RECTANGULO_REDONDEADO_MAS_COMBINE_ADD\r\n" +
                "Union=3_CUERPOS_SOLIDOS_INTERSECTADOS\r\n" +
                "Aft=INTERFAZ_ANCHA_SIN_BOATTAIL\r\n" +
                "Guias=NO_EJECUTADAS\r\n" +
                "Fillets=NO_EJECUTADOS\r\n" +
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

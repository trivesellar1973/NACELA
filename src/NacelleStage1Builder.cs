using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SolidWorks.Interop.sldworks;

namespace NacelleSolidWorks
{
    internal sealed class BuildResult
    {
        public string PartPath;
        public string LogPath;
        public string OutputDirectory;
    }

    internal sealed class NacelleStage1Builder
    {
        private readonly SwSession session;
        private readonly NacelleConfig cfg;
        private readonly string root;
        private readonly string output;
        private readonly Action<string> log;

        public NacelleStage1Builder(SwSession session, NacelleConfig cfg, string repositoryRoot, Action<string> log)
        {
            this.session = session;
            this.cfg = cfg;
            this.root = repositoryRoot;
            this.output = Path.Combine(repositoryRoot, cfg.OutputDirectory, cfg.Revision);
            this.log = log;
        }

        public BuildResult Build()
        {
            Directory.CreateDirectory(output);
            string partPath = Path.Combine(output, "NACELA_DERECHA_" + cfg.Revision + "_STAGE1.SLDPRT");
            session.CloseIfOpen(partPath);

            IModelDoc2 doc = session.NewPart();
            AddEquations(doc);

            List<Feature> profiles = new List<Feature>();
            foreach (NacelleSection s in cfg.Sections)
            {
                Feature p = SwGeometry.CreateClosedSection(doc, s, cfg.YMotor, "02_OML_" + s.Name);
                profiles.Add(p);
            }

            List<Feature> guides = new List<Feature>
            {
                SwGeometry.CreateGuide(doc, cfg.Sections, cfg.YMotor, 0, "03_GUIA_CORONA"),
                SwGeometry.CreateGuide(doc, cfg.Sections, cfg.YMotor, 1, "03_GUIA_PANZA"),
                SwGeometry.CreateGuide(doc, cfg.Sections, cfg.YMotor, 2, "03_GUIA_EXTERIOR"),
                SwGeometry.CreateGuide(doc, cfg.Sections, cfg.YMotor, 3, "03_GUIA_INTERIOR")
            };

            SwGeometry.LoftWithGuides(doc, profiles, guides, "04_CUERPO_PRINCIPAL_OML_" + cfg.Revision);
            foreach (Feature feature in profiles) SwGeometry.HideFeatureSketch(doc, feature);
            foreach (Feature feature in guides) SwGeometry.HideFeatureSketch(doc, feature);

            List<Feature> fairingProfiles = new List<Feature>();
            foreach (FairingSection s in cfg.FairingSections)
            {
                fairingProfiles.Add(SwGeometry.CreateFairingSection(doc, s, cfg.YMotor, "05_FAIRING_" + s.Name));
            }
            SwGeometry.SimpleMergedLoft(doc, fairingProfiles, "05_FAIRING_SUPERIOR_CORTO_" + cfg.Revision);
            foreach (Feature feature in fairingProfiles) SwGeometry.HideFeatureSketch(doc, feature);

            Feature axis = SwGeometry.CreateAxisSketch(doc, cfg.XFront - 0.20, cfg.XAft + 0.20, cfg.YMotor, cfg.ZAxis, "00_REF_EJE_MOTOR_DER");
            SwGeometry.HideFeatureSketch(doc, axis);

            bool filletOk = SwGeometry.TryFilletNear(doc, 0.035, -0.35, cfg.YMotor + 0.22, -0.25, "06_FILETE_FAIRING_R035");
            log(filletOk ? "Filete del fairing creado." : "Advertencia: el filete del fairing no se creo; el modelo continua sin parchear la geometria.");

            doc.MaterialPropertyValues = new[] { cfg.SkinR, cfg.SkinG, cfg.SkinB, 1.0, 1.0, 0.40, 0.20, 0.0, 0.0 };
            SwGeometry.HideConstruction(doc);
            doc.EditRebuild3();

            int solids = SwGeometry.SolidBodyCount(doc);
            int surfaces = SwGeometry.SurfaceBodyCount(doc);
            if (solids != 1 || surfaces != 0)
                throw new InvalidOperationException("Stage 1 invalido. Solidos=" + solids + " superficies=" + surfaces);

            double[] box = SwGeometry.BoundingBox(doc);
            double lx = box[3] - box[0];
            double wy = box[4] - box[1];
            double hz = box[5] - box[2];

            doc.ShowNamedView2("*Isometric", 7);
            doc.ViewZoomtofit2();
            int saveError = doc.SaveAs3(partPath, 0, 2);
            if (saveError != 0) throw new IOException("No se pudo guardar SLDPRT. Error=" + saveError);

            SaveReviewImage(doc, Path.Combine(output, "NACELA_DERECHA_" + cfg.Revision + "_ISO.bmp"));
            string reportPath = Path.Combine(output, "VALIDACION_STAGE1_" + cfg.Revision + ".txt");
            File.WriteAllText(reportPath, BuildReport(solids, surfaces, lx, wy, hz, filletOk));

            log("Pieza creada: " + partPath);
            log("Bounding box: L=" + F(lx) + " W=" + F(wy) + " H=" + F(hz));
            return new BuildResult { PartPath = partPath, LogPath = reportPath, OutputDirectory = output };
        }

        private void AddEquations(IModelDoc2 doc)
        {
            IEquationMgr eq = (IEquationMgr)doc.GetEquationMgr();
            Add(eq, "X_NAC_FWD", F(cfg.XFront) + "m");
            Add(eq, "X_NAC_AFT", F(cfg.XAft) + "m");
            Add(eq, "L_NAC", F(cfg.Length) + "m");
            Add(eq, "W_NAC_MAX", F(cfg.MaxWidth) + "m");
            Add(eq, "H_NAC_MAX", F(cfg.MaxHeight) + "m");
            Add(eq, "Y_MOTOR_DER", F(cfg.YMotor) + "m");
            Add(eq, "Z_EJE_MOTOR", F(cfg.ZAxis) + "m");
            Add(eq, "GAP_ALA_NACELA", F(cfg.WingGap) + "m");
            Add(eq, "L_ENV_MOTOR", F(cfg.EngineLength) + "m");
            Add(eq, "W_ENV_MOTOR", F(cfg.EngineWidth) + "m");
            Add(eq, "H_ENV_MOTOR", F(cfg.EngineHeight) + "m");
        }

        private static void Add(IEquationMgr mgr, string name, string value)
        {
            mgr.Add2(-1, "\"" + name + "\" = " + value, true);
        }

        private string BuildReport(int solids, int surfaces, double lx, double wy, double hz, bool filletOk)
        {
            return
                "NACELA SOLIDWORKS - VALIDACION STAGE 1\r\n" +
                "Revision=" + cfg.Revision + "\r\n" +
                "Metodo=Loft solido nativo con 10 perfiles cerrados y 4 guias continuas\r\n" +
                "X_global=[" + F(cfg.XFront) + "," + F(cfg.XAft) + "] m\r\n" +
                "Y_motor=" + F(cfg.YMotor) + " m\r\n" +
                "Z_eje=" + F(cfg.ZAxis) + " m\r\n" +
                "BoundingBox_L=" + F(lx) + " m\r\n" +
                "BoundingBox_W=" + F(wy) + " m\r\n" +
                "BoundingBox_H=" + F(hz) + " m\r\n" +
                "Solidos=" + solids + "\r\n" +
                "Superficies=" + surfaces + "\r\n" +
                "Filete_fairing=" + (filletOk ? "OK" : "OMITIDO_SIN_PARCHE") + "\r\n" +
                "Estado=LISTO_PARA_REVISION_VISUAL\r\n" +
                "Nota=No se generaron toma, escape ni capos hasta aprobar el OML.\r\n";
        }

        private static void SaveReviewImage(IModelDoc2 doc, string path)
        {
            doc.ShowNamedView2("*Isometric", 7);
            doc.ViewZoomtofit2();
            doc.GraphicsRedraw2();
            doc.SaveBMP(path, 1600, 1000);
        }

        private static string F(double value) { return value.ToString("0.000000", CultureInfo.InvariantCulture); }
    }
}

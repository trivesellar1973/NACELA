using System;
using System.IO;
using SolidWorks.Interop.sldworks;

namespace NacelleSolidWorks
{
    internal sealed class B1AssemblyReviewBuilder
    {
        private readonly SwSession session;
        private readonly B1Config cfg;
        private readonly Action<string> log;

        public B1AssemblyReviewBuilder(SwSession session, B1Config cfg, Action<string> log)
        {
            this.session = session;
            this.cfg = cfg;
            this.log = log;
        }

        public string Build(string outputDirectory, string nacellePartPath)
        {
            if (!File.Exists(cfg.SourceAssembly))
            {
                log("B1: no se encontro el ensamblaje base: " + cfg.SourceAssembly);
                log("B1: se conserva la pieza de nacela y se omite solamente la revision con ala.");
                return String.Empty;
            }

            string reviewAssembly = Path.Combine(outputDirectory, "ALA_REVIEW_NACELA_DER_B1.SLDASM");
            session.CloseIfOpen(reviewAssembly);
            IModelDoc2 doc = session.OpenAssembly(cfg.SourceAssembly);

            int saveError = doc.SaveAs3(reviewAssembly, 0, 2);
            if (saveError != 0) throw new IOException("No se pudo guardar el ensamblaje B1. Error=" + saveError);

            IAssemblyDoc assembly = (IAssemblyDoc)doc;
            int errors = 0, warnings = 0;
            IModelDoc2 part = (IModelDoc2)session.App.OpenDoc6(nacellePartPath, 1, 1, "", ref errors, ref warnings);
            if (part == null) throw new InvalidOperationException("No se pudo abrir la nacela B1 para insertar. Error=" + errors);

            int activateError = 0;
            session.App.ActivateDoc3(doc.GetTitle(), false, 0, ref activateError);
            Component2 component = assembly.AddComponent5(nacellePartPath, 0, "", false, "", 0, 0, 0);
            if (component == null) throw new InvalidOperationException("No se pudo insertar la nacela B1 en el ensamblaje");

            IMathUtility math = (IMathUtility)session.App.GetMathUtility();
            double[] transformData = new double[]
            {
                1.0, 0.0, 0.0,
                0.0, 1.0, 0.0,
                0.0, 0.0, 1.0,
                cfg.AssemblyX, cfg.YMotor, cfg.ZAxis,
                1.0, 0.0, 0.0, 0.0
            };
            MathTransform transform = (MathTransform)math.CreateTransform(transformData);
            if (transform == null) throw new InvalidOperationException("No se pudo crear la transformacion B1");
            component.Transform2 = transform;

            doc.ClearSelection2(true);
            component.Select4(false, null, false);
            assembly.FixComponent();
            doc.ClearSelection2(true);

            doc.EditRebuild3();
            int e = 0, w = 0;
            doc.Save3(1, ref e, ref w);
            if (e != 0) throw new IOException("Error guardando ensamble B1=" + e);

            SaveView(doc, "*Right", 4, Path.Combine(outputDirectory, "ALA_REVIEW_NACELA_DER_B1_LATERAL.bmp"));
            SaveView(doc, "*Front", 1, Path.Combine(outputDirectory, "ALA_REVIEW_NACELA_DER_B1_FRONTAL.bmp"));
            SaveView(doc, "*Top", 5, Path.Combine(outputDirectory, "ALA_REVIEW_NACELA_DER_B1_PLANTA.bmp"));
            SaveView(doc, "*Isometric", 7, Path.Combine(outputDirectory, "ALA_REVIEW_NACELA_DER_B1_ISO.bmp"));

            log("B1 ensamblaje de revision creado: " + reviewAssembly);
            log("B1 transformacion: X=" + cfg.AssemblyX.ToString("0.000000") +
                " Y=" + cfg.YMotor.ToString("0.000000") +
                " Z=" + cfg.ZAxis.ToString("0.000000"));
            return reviewAssembly;
        }

        private static void SaveView(IModelDoc2 doc, string name, int id, string path)
        {
            doc.ShowNamedView2(name, id);
            doc.ViewZoomtofit2();
            doc.GraphicsRedraw2();
            doc.SaveBMP(path, 1800, 1100);
        }
    }
}

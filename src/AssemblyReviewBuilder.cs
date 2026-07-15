using System;
using System.IO;
using SolidWorks.Interop.sldworks;

namespace NacelleSolidWorks
{
    internal sealed class AssemblyReviewBuilder
    {
        private readonly SwSession session;
        private readonly NacelleConfig cfg;
        private readonly Action<string> log;

        public AssemblyReviewBuilder(SwSession session, NacelleConfig cfg, Action<string> log)
        {
            this.session = session;
            this.cfg = cfg;
            this.log = log;
        }

        public string Build(string outputDirectory, string nacellePartPath)
        {
            // El ala es solamente una referencia para la revision de posicion. La nacela
            // ya fue creada desde cero antes de entrar a esta funcion. Si el ensamblaje
            // base no existe, no se invalida la generacion de la pieza.
            if (!File.Exists(cfg.SourceAssembly))
            {
                log("Advertencia: no se encontro el ensamblaje base: " + cfg.SourceAssembly);
                log("Se omite el ensamblaje de revision, pero la nacela SLDPRT queda generada.");
                return String.Empty;
            }

            string reviewAssembly = Path.Combine(outputDirectory, "ALA_REVIEW_NACELA_DER_" + cfg.Revision + ".SLDASM");
            session.CloseIfOpen(reviewAssembly);
            IModelDoc2 doc = session.OpenAssembly(cfg.SourceAssembly);

            int saveError = doc.SaveAs3(reviewAssembly, 0, 2);
            if (saveError != 0) throw new IOException("No se pudo guardar copia del ensamblaje. Error=" + saveError);

            IAssemblyDoc assembly = (IAssemblyDoc)doc;
            int errors = 0, warnings = 0;
            IModelDoc2 part = (IModelDoc2)session.App.OpenDoc6(nacellePartPath, 1, 1, "", ref errors, ref warnings);
            if (part == null) throw new InvalidOperationException("No se pudo abrir nacela para insertar. Error=" + errors);

            int activateError = 0;
            session.App.ActivateDoc3(doc.GetTitle(), false, 0, ref activateError);
            Component2 component = assembly.AddComponent5(nacellePartPath, 0, "", false, "", 0, 0, 0);
            if (component == null) throw new InvalidOperationException("No se pudo insertar la nacela en el ensamblaje");

            doc.ClearSelection2(true);
            component.Select4(false, null, false);
            assembly.FixComponent();
            doc.ClearSelection2(true);

            doc.EditRebuild3();
            int e = 0, w = 0;
            doc.Save3(1, ref e, ref w);
            if (e != 0) throw new IOException("Error guardando ensamble de revision=" + e);

            doc.ShowNamedView2("*Isometric", 7);
            doc.ViewZoomtofit2();
            doc.GraphicsRedraw2();
            doc.SaveBMP(Path.Combine(outputDirectory, "ALA_REVIEW_NACELA_DER_" + cfg.Revision + "_ISO.bmp"), 1800, 1100);
            log("Ensamblaje de revision creado: " + reviewAssembly);
            return reviewAssembly;
        }
    }
}

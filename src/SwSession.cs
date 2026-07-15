using System;
using System.IO;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;

namespace NacelleSolidWorks
{
    internal sealed class SwSession
    {
        public readonly ISldWorks App;
        public readonly Action<string> Log;

        private SwSession(ISldWorks app, Action<string> log)
        {
            App = app;
            Log = log;
        }

        public static SwSession Connect(Action<string> log)
        {
            ISldWorks app = null;
            try { app = (ISldWorks)Marshal.GetActiveObject("SldWorks.Application"); }
            catch { }

            if (app == null)
            {
                Type t = Type.GetTypeFromProgID("SldWorks.Application");
                if (t == null) throw new InvalidOperationException("SOLIDWORKS no esta registrado en Windows");
                app = (ISldWorks)Activator.CreateInstance(t);
                app.Visible = true;
                log("SOLIDWORKS iniciado por el generador.");
            }
            else
            {
                log("Conexion con SOLIDWORKS activo.");
            }
            return new SwSession(app, log);
        }

        public IModelDoc2 NewPart()
        {
            string template = App.GetUserPreferenceStringValue(8);
            if (String.IsNullOrWhiteSpace(template) || !File.Exists(template))
                throw new InvalidOperationException("No se encontro la plantilla predeterminada de pieza en SOLIDWORKS");
            IModelDoc2 doc = (IModelDoc2)App.NewDocument(template, 0, 0, 0);
            if (doc == null) throw new InvalidOperationException("SOLIDWORKS no pudo crear una pieza nueva");
            return doc;
        }

        public IModelDoc2 OpenAssembly(string file)
        {
            int errors = 0, warnings = 0;
            IModelDoc2 doc = (IModelDoc2)App.OpenDoc6(file, 2, 0, "", ref errors, ref warnings);
            if (doc == null) throw new InvalidOperationException("No se pudo abrir ensamblaje: " + file + " error=" + errors);
            int activateErrors = 0;
            App.ActivateDoc3(doc.GetTitle(), false, 0, ref activateErrors);
            return doc;
        }

        public void CloseIfOpen(string absolutePath)
        {
            IModelDoc2 doc = (IModelDoc2)App.GetOpenDocumentByName(absolutePath);
            if (doc != null) App.CloseDoc(doc.GetTitle());
        }
    }
}

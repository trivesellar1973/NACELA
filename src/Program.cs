using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NacelleSolidWorks
{
    internal static class Program
    {
        private static readonly List<string> messages = new List<string>();

        private static int Main(string[] args)
        {
            string command = args.Length > 0 ? args[0].ToLowerInvariant() : "stage1";
            string root = args.Length > 1 ? Path.GetFullPath(args[1]) : FindRepositoryRoot();
            string executionLog = Path.Combine(root, "ultimo_ejecucion.log");

            try
            {
                Log("Repositorio: " + root);
                NacelleConfig cfg = NacelleConfig.Load(root);
                Log("Revision: " + cfg.Revision);

                if (command != "stage1" && command != "review")
                    throw new InvalidOperationException("Esta revision solo habilita STAGE1. Toma, escapes y capos se agregaran despues de aprobar la captura.");

                SwSession session = SwSession.Connect(Log);
                BuildResult result = new NacelleStage1Builder(session, cfg, root, Log).Build();
                string assembly = new AssemblyReviewBuilder(session, cfg, Log).Build(result.OutputDirectory, result.PartPath);
                Log("RESULTADO_PIEZA=" + result.PartPath);
                Log("RESULTADO_ENSAMBLE=" + assembly);
                Log("RESULTADO_REPORTE=" + result.LogPath);
                Log("Ejecucion finalizada correctamente.");
                File.WriteAllText(executionLog, JoinMessages(), Encoding.UTF8);
                Console.WriteLine("\nLISTO. Envie una captura de la nacela en el ensamblaje para la siguiente revision.");
                return 0;
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex);
                try { File.WriteAllText(executionLog, JoinMessages(), Encoding.UTF8); } catch { }
                Console.Error.WriteLine(ex.Message);
                return 2;
            }
        }

        private static void Log(string message)
        {
            string line = DateTime.Now.ToString("HH:mm:ss") + " | " + message;
            messages.Add(line);
            Console.WriteLine(line);
        }

        private static string JoinMessages() { return String.Join(Environment.NewLine, messages.ToArray()); }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "config", "defaults.ini"))) return dir.FullName;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("No se encontro la raiz del repositorio");
        }
    }
}

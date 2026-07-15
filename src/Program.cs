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
            string command = args.Length > 0 ? args[0].ToLowerInvariant() : "review";
            string root = null;
            string executionLog = null;

            try
            {
                root = ResolveRepositoryRoot(args);
                executionLog = Path.Combine(root, "ultimo_ejecucion.log");

                Log("Repositorio: " + root);
                B1Config cfg = B1Config.Load(root);
                Log("Revision limpia activa: " + cfg.Revision);
                Log("Comando: " + command);

                if (command != "stage1" && command != "stage2" && command != "stage3" && command != "review")
                    throw new InvalidOperationException("Comando valido: stage1, stage2, stage3 o review.");

                SwSession session = SwSession.Connect(Log);

                B1BuildResult stage1 = new B1Stage1Builder(session, cfg, root, Log).Build();
                B1BuildResult finalResult = stage1;
                B1BuildResult stage2 = null;
                B1BuildResult stage3 = null;

                if (command == "stage2" || command == "stage3" || command == "review")
                {
                    stage2 = new B1Stage2Builder(session, cfg, Log).Build(stage1);
                    finalResult = stage2;
                }

                if (command == "stage3" || command == "review")
                {
                    stage3 = new B1Stage3Builder(session, cfg, Log).Build(stage2);
                    finalResult = stage3;
                }

                string assembly = new B1AssemblyReviewBuilder(session, cfg, Log).Build(finalResult.OutputDirectory, finalResult.PartPath);

                Log("RESULTADO_B1_STAGE1=" + stage1.PartPath);
                if (stage2 != null) Log("RESULTADO_B1_STAGE2=" + stage2.PartPath);
                if (stage3 != null) Log("RESULTADO_B1_STAGE3=" + stage3.PartPath);
                if (!String.IsNullOrWhiteSpace(assembly)) Log("RESULTADO_B1_ENSAMBLE=" + assembly);
                else Log("RESULTADO_B1_ENSAMBLE=OMITIDO_POR_FALTA_DE_ALA_BASE");
                Log("RESULTADO_B1_REPORTE=" + finalResult.ReportPath);
                Log("B1 finalizado correctamente.");

                File.WriteAllText(executionLog, JoinMessages(), Encoding.UTF8);
                Console.WriteLine("\nLISTO. La nacela B1 fue reconstruida desde cero.");
                Console.WriteLine("No se reutilizo la geometria A0/A1/A2.");
                if (!String.IsNullOrWhiteSpace(assembly))
                    Console.WriteLine("Se creo tambien el ensamblaje B1 en la estacion y=4.30 m.");
                return 0;
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex);
                try
                {
                    if (String.IsNullOrWhiteSpace(executionLog))
                    {
                        string fallback = !String.IsNullOrWhiteSpace(root) && Directory.Exists(root)
                            ? root
                            : AppDomain.CurrentDomain.BaseDirectory;
                        executionLog = Path.Combine(fallback, "ultimo_ejecucion.log");
                    }
                    File.WriteAllText(executionLog, JoinMessages(), Encoding.UTF8);
                }
                catch { }
                Console.Error.WriteLine(ex.Message);
                return 2;
            }
        }

        private static string ResolveRepositoryRoot(string[] args)
        {
            if (args.Length <= 1 || String.IsNullOrWhiteSpace(args[1]))
                return FindRepositoryRoot();

            string candidate = args[1].Trim();
            while (candidate.Length >= 2 && candidate[0] == '"' && candidate[candidate.Length - 1] == '"')
                candidate = candidate.Substring(1, candidate.Length - 2).Trim();

            candidate = candidate.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (candidate.Length == 2 && candidate[1] == ':') candidate += Path.DirectorySeparatorChar;
            if (candidate.Length == 0) return FindRepositoryRoot();

            string full = Path.GetFullPath(candidate);
            string marker = Path.Combine(full, "config", "defaults.ini");
            if (!File.Exists(marker))
                throw new DirectoryNotFoundException("La ruta indicada no es la raiz del repositorio: " + full);
            return full;
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace NacelleSolidWorks
{
    internal sealed class NacelleSection
    {
        public string Name;
        public double X;
        public double Width;
        public double Height;
        public double ZCenter;
        public double NSide;
        public double NTop;
        public double NBottom;
    }

    internal sealed class FairingSection
    {
        public string Name;
        public double X;
        public double Width;
        public double ZBottom;
        public double ZTop;
    }

    internal sealed class NacelleConfig
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string Revision { get { return Get("revision"); } }
        public string SourceAssembly { get { return Expand(Get("source_assembly")); } }
        public string OutputDirectory { get { return Get("output_directory"); } }
        public double XFront { get { return GetDouble("x_front_m"); } }
        public double XAft { get { return GetDouble("x_aft_m"); } }
        public double YMotor { get { return GetDouble("y_motor_m"); } }
        public double ZAxis { get { return GetDouble("z_axis_m"); } }
        public double WingGap { get { return GetDouble("wing_gap_m"); } }
        public double Length { get { return GetDouble("length_m"); } }
        public double MaxWidth { get { return GetDouble("max_width_m"); } }
        public double MaxHeight { get { return GetDouble("max_height_m"); } }
        public double EngineLength { get { return GetDouble("engine_length_m"); } }
        public double EngineWidth { get { return GetDouble("engine_width_m"); } }
        public double EngineHeight { get { return GetDouble("engine_height_m"); } }
        public double SkinR { get { return GetDouble("skin_r"); } }
        public double SkinG { get { return GetDouble("skin_g"); } }
        public double SkinB { get { return GetDouble("skin_b"); } }

        public List<NacelleSection> Sections = new List<NacelleSection>();
        public List<FairingSection> FairingSections = new List<FairingSection>();

        public static NacelleConfig Load(string repositoryRoot)
        {
            NacelleConfig cfg = new NacelleConfig();
            cfg.LoadFile(Path.Combine(repositoryRoot, "config", "defaults.ini"));
            string local = Path.Combine(repositoryRoot, "config", "local.ini");
            if (File.Exists(local)) cfg.LoadFile(local);
            cfg.ParseSections();
            cfg.Validate();
            return cfg;
        }

        private void LoadFile(string file)
        {
            if (!File.Exists(file)) throw new FileNotFoundException("No se encontro configuracion", file);
            foreach (string original in File.ReadAllLines(file))
            {
                string line = original.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();
                values[key] = value;
            }
        }

        private void ParseSections()
        {
            foreach (KeyValuePair<string, string> pair in values)
            {
                if (pair.Key.StartsWith("section.", StringComparison.OrdinalIgnoreCase))
                {
                    string[] p = pair.Value.Split(';');
                    if (p.Length != 7) throw new InvalidDataException(pair.Key + " debe tener 7 campos");
                    Sections.Add(new NacelleSection
                    {
                        Name = pair.Key.Substring("section.".Length),
                        X = D(p[0]), Width = D(p[1]), Height = D(p[2]), ZCenter = D(p[3]),
                        NSide = D(p[4]), NTop = D(p[5]), NBottom = D(p[6])
                    });
                }
                else if (pair.Key.StartsWith("fairing.", StringComparison.OrdinalIgnoreCase))
                {
                    string[] p = pair.Value.Split(';');
                    if (p.Length != 4) throw new InvalidDataException(pair.Key + " debe tener 4 campos");
                    FairingSections.Add(new FairingSection
                    {
                        Name = pair.Key.Substring("fairing.".Length),
                        X = D(p[0]), Width = D(p[1]), ZBottom = D(p[2]), ZTop = D(p[3])
                    });
                }
            }
            Sections.Sort(delegate(NacelleSection a, NacelleSection b) { return a.X.CompareTo(b.X); });
            FairingSections.Sort(delegate(FairingSection a, FairingSection b) { return a.X.CompareTo(b.X); });
        }

        private void Validate()
        {
            if (Sections.Count < 6) throw new InvalidDataException("Se requieren al menos 6 secciones OML");
            if (FairingSections.Count < 3) throw new InvalidDataException("Se requieren al menos 3 secciones de fairing");
            if (Math.Abs((XAft - XFront) - Length) > 0.002) throw new InvalidDataException("L_NAC no coincide con extremos X");
            if (Math.Abs(Sections[0].X - XFront) > 0.002) throw new InvalidDataException("Primera seccion no coincide con X frontal");
            if (Math.Abs(Sections[Sections.Count - 1].X - XAft) > 0.002) throw new InvalidDataException("Ultima seccion no coincide con X trasero");
            double maxW = 0.0, maxH = 0.0;
            foreach (NacelleSection s in Sections)
            {
                maxW = Math.Max(maxW, s.Width);
                maxH = Math.Max(maxH, s.Height);
                if (s.Width <= 0 || s.Height <= 0) throw new InvalidDataException("Seccion invalida " + s.Name);
                if (s.NSide < 2.0 || s.NTop < 2.0 || s.NBottom < 2.0) throw new InvalidDataException("Exponentes deben ser >=2 en " + s.Name);
            }
            if (Math.Abs(maxW - MaxWidth) > 0.003) throw new InvalidDataException("Ancho maximo no coincide");
            if (Math.Abs(maxH - MaxHeight) > 0.003) throw new InvalidDataException("Altura maxima no coincide");
        }

        private string Get(string key)
        {
            string value;
            if (!values.TryGetValue(key, out value)) throw new KeyNotFoundException("Falta parametro: " + key);
            return value;
        }

        private double GetDouble(string key) { return D(Get(key)); }
        private static double D(string value) { return double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture); }
        private static string Expand(string value) { return Environment.ExpandEnvironmentVariables(value); }
    }
}

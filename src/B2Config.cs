using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace NacelleSolidWorks
{
    internal sealed class B2OmlSection
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

    internal sealed class B2RoundedSection
    {
        public string Name;
        public double X;
        public double Width;
        public double Height;
        public double ZCenter;
        public double CornerRadius;
    }

    internal sealed class B2SaddleSection
    {
        public string Name;
        public double X;
        public double Width;
        public double ZBottom;
        public double ZTop;
    }

    internal sealed class B2Config
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string Revision { get { return Get("revision"); } }
        public string SourceAssembly { get { return Expand(Get("source_assembly")); } }
        public string OutputDirectory { get { return Get("output_directory"); } }

        public double LeadingEdgeX { get { return D("x_le_motor_global_m"); } }
        public double PropPlaneAheadLeadingEdge { get { return D("prop_plane_ahead_le_m"); } }
        public double AssemblyX { get { return LeadingEdgeX - PropPlaneAheadLeadingEdge; } }
        public double YMotor { get { return D("y_motor_m"); } }
        public double ZAxis { get { return D("z_axis_global_m"); } }
        public double LocalChord { get { return D("local_chord_m"); } }

        public double XFront { get { return D("x_front_local_m"); } }
        public double XAft { get { return D("x_aft_local_m"); } }
        public double Length { get { return D("length_m"); } }
        public double MaxWidth { get { return D("max_width_m"); } }
        public double MaxHeight { get { return D("max_height_m"); } }
        public double GlobalAft { get { return AssemblyX + XAft; } }

        public double EngineLength { get { return D("engine_length_m"); } }
        public double EngineWidth { get { return D("engine_width_m"); } }
        public double EngineHeight { get { return D("engine_height_m"); } }
        public double IntakeRequiredArea { get { return D("intake_required_area_m2"); } }

        public double SkinR { get { return D("skin_r"); } }
        public double SkinG { get { return D("skin_g"); } }
        public double SkinB { get { return D("skin_b"); } }

        public readonly List<B2OmlSection> OmlSections = new List<B2OmlSection>();
        public readonly List<B2OmlSection> NoseSections = new List<B2OmlSection>();
        public readonly List<B2RoundedSection> IntakeSections = new List<B2RoundedSection>();
        public readonly List<B2SaddleSection> SaddleSections = new List<B2SaddleSection>();

        public static B2Config Load(string repositoryRoot)
        {
            B2Config cfg = new B2Config();
            cfg.LoadFile(Path.Combine(repositoryRoot, "config", "defaults.ini"));
            string local = Path.Combine(repositoryRoot, "config", "local.ini");
            if (File.Exists(local)) cfg.LoadFile(local);
            cfg.ParseGeometry();
            cfg.Validate();
            return cfg;
        }

        private void LoadFile(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("No se encontro configuracion", path);
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                values[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
            }
        }

        private void ParseGeometry()
        {
            foreach (KeyValuePair<string, string> pair in values)
            {
                if (pair.Key.StartsWith("oml.", StringComparison.OrdinalIgnoreCase))
                    OmlSections.Add(ParseOml(pair.Key, pair.Value, "oml."));
                else if (pair.Key.StartsWith("nose.", StringComparison.OrdinalIgnoreCase))
                    NoseSections.Add(ParseOml(pair.Key, pair.Value, "nose."));
                else if (pair.Key.StartsWith("intake.", StringComparison.OrdinalIgnoreCase))
                {
                    string[] p = pair.Value.Split(';');
                    if (p.Length != 5) throw new InvalidDataException(pair.Key + " debe tener 5 campos");
                    IntakeSections.Add(new B2RoundedSection
                    {
                        Name = pair.Key.Substring("intake.".Length),
                        X = Parse(p[0]), Width = Parse(p[1]), Height = Parse(p[2]),
                        ZCenter = Parse(p[3]), CornerRadius = Parse(p[4])
                    });
                }
                else if (pair.Key.StartsWith("saddle.", StringComparison.OrdinalIgnoreCase))
                {
                    string[] p = pair.Value.Split(';');
                    if (p.Length != 4) throw new InvalidDataException(pair.Key + " debe tener 4 campos");
                    SaddleSections.Add(new B2SaddleSection
                    {
                        Name = pair.Key.Substring("saddle.".Length),
                        X = Parse(p[0]), Width = Parse(p[1]),
                        ZBottom = Parse(p[2]), ZTop = Parse(p[3])
                    });
                }
            }

            OmlSections.Sort(delegate(B2OmlSection a, B2OmlSection b) { return a.X.CompareTo(b.X); });
            NoseSections.Sort(delegate(B2OmlSection a, B2OmlSection b) { return a.X.CompareTo(b.X); });
            IntakeSections.Sort(delegate(B2RoundedSection a, B2RoundedSection b) { return a.X.CompareTo(b.X); });
            SaddleSections.Sort(delegate(B2SaddleSection a, B2SaddleSection b) { return a.X.CompareTo(b.X); });
        }

        private static B2OmlSection ParseOml(string key, string value, string prefix)
        {
            string[] p = value.Split(';');
            if (p.Length != 7) throw new InvalidDataException(key + " debe tener 7 campos");
            return new B2OmlSection
            {
                Name = key.Substring(prefix.Length),
                X = Parse(p[0]), Width = Parse(p[1]), Height = Parse(p[2]), ZCenter = Parse(p[3]),
                NSide = Parse(p[4]), NTop = Parse(p[5]), NBottom = Parse(p[6])
            };
        }

        private void Validate()
        {
            if (!String.Equals(Revision, "B2", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("La configuracion activa debe ser B2");
            if (OmlSections.Count < 10) throw new InvalidDataException("B2 requiere al menos 10 secciones OML");
            if (NoseSections.Count < 6) throw new InvalidDataException("B2 requiere al menos 6 secciones de nariz");
            if (IntakeSections.Count < 7) throw new InvalidDataException("B2 requiere al menos 7 secciones de toma solida");
            if (SaddleSections.Count < 8) throw new InvalidDataException("B2 requiere al menos 8 secciones saddle");
            if (Math.Abs((XAft - XFront) - Length) > 0.001) throw new InvalidDataException("Longitud B2 inconsistente");
            if (Math.Abs(OmlSections[OmlSections.Count - 1].X - XAft) > 0.001)
                throw new InvalidDataException("La ultima seccion OML no coincide con X aft");

            double maxW = 0.0;
            foreach (B2OmlSection section in OmlSections)
            {
                maxW = Math.Max(maxW, section.Width);
                if (section.Width <= 0.0 || section.Height <= 0.0) throw new InvalidDataException("Seccion OML invalida");
            }
            if (Math.Abs(maxW - MaxWidth) > 0.002) throw new InvalidDataException("Ancho maximo B2 inconsistente");
            if (EngineWidth + 0.180 > MaxWidth) throw new InvalidDataException("Falta margen lateral para el motor");

            double aftRatio = (GlobalAft - LeadingEdgeX) / LocalChord;
            if (aftRatio > 0.530) throw new InvalidDataException("La cola B2 supera x/c=0.53");
        }

        private string Get(string key)
        {
            string value;
            if (!values.TryGetValue(key, out value)) throw new KeyNotFoundException("Falta parametro B2: " + key);
            return value;
        }

        private double D(string key) { return Parse(Get(key)); }
        private static double Parse(string value) { return double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture); }
        private static string Expand(string value) { return Environment.ExpandEnvironmentVariables(value); }
    }
}

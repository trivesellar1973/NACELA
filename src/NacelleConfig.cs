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

        public double LeadingEdgeX { get { return GetDouble("x_le_motor_global_m"); } }
        public double PropPlaneAheadLeadingEdge { get { return GetDouble("prop_plane_ahead_le_m"); } }
        public double AssemblyX { get { return LeadingEdgeX - PropPlaneAheadLeadingEdge; } }
        public double YMotor { get { return GetDouble("y_motor_m"); } }
        public double ZAxis { get { return GetDouble("z_axis_global_m"); } }
        public double WingGap { get { return GetDouble("wing_gap_m"); } }
        public double LocalChord { get { return GetDouble("local_chord_m"); } }

        public double XFront { get { return GetDouble("x_front_local_m"); } }
        public double XAft { get { return GetDouble("x_aft_local_m"); } }
        public double GlobalFront { get { return AssemblyX + XFront; } }
        public double GlobalAft { get { return AssemblyX + XAft; } }
        public double Length { get { return GetDouble("length_m"); } }
        public double MaxWidth { get { return GetDouble("max_width_m"); } }
        public double MaxHeight { get { return GetDouble("max_height_m"); } }
        public double EngineLength { get { return GetDouble("engine_length_m"); } }
        public double EngineWidth { get { return GetDouble("engine_width_m"); } }
        public double EngineHeight { get { return GetDouble("engine_height_m"); } }

        public double IntakeRequiredArea { get { return GetDouble("intake_required_area_m2"); } }
        public double IntakeWidth { get { return GetDouble("intake_width_m"); } }
        public double IntakeHeight { get { return GetDouble("intake_height_m"); } }
        public double IntakeXOuter { get { return GetDouble("intake_x_outer_m"); } }
        public double IntakeXCapture { get { return GetDouble("intake_x_capture_m"); } }
        public double IntakeXInterface { get { return GetDouble("intake_x_interface_m"); } }
        public double IntakeZOuter { get { return GetDouble("intake_z_outer_m"); } }
        public double IntakeZInterface { get { return GetDouble("intake_z_interface_m"); } }
        public double IntakeLipPocketWidth { get { return GetDouble("intake_lip_pocket_width_m"); } }
        public double IntakeLipPocketHeight { get { return GetDouble("intake_lip_pocket_height_m"); } }

        public double ExhaustX { get { return GetDouble("exhaust_x_m"); } }
        public double ExhaustZ { get { return GetDouble("exhaust_z_m"); } }
        public double ExhaustLength { get { return GetDouble("exhaust_length_m"); } }
        public double ExhaustHeight { get { return GetDouble("exhaust_height_m"); } }
        public double ExhaustOuterY { get { return GetDouble("exhaust_outer_y_m"); } }
        public double ExhaustInnerY { get { return GetDouble("exhaust_inner_y_m"); } }
        public double ExhaustRecessInnerY { get { return GetDouble("exhaust_recess_inner_y_m"); } }
        public double ExhaustBezelLength { get { return GetDouble("exhaust_bezel_length_m"); } }
        public double ExhaustBezelHeight { get { return GetDouble("exhaust_bezel_height_m"); } }
        public double ExhaustEquivalentEach { get { return GetDouble("exhaust_equiv_each_m"); } }

        public double NacaX { get { return GetDouble("naca_x_m"); } }
        public double NacaZ { get { return GetDouble("naca_z_m"); } }
        public double NacaLength { get { return GetDouble("naca_length_m"); } }
        public double NacaHeight { get { return GetDouble("naca_height_m"); } }
        public double NacaOuterY { get { return GetDouble("naca_outer_y_m"); } }
        public double NacaInnerY { get { return GetDouble("naca_inner_y_m"); } }

        public double CowlX { get { return GetDouble("cowl_x_m"); } }
        public double CowlZ { get { return GetDouble("cowl_z_m"); } }
        public double CowlLength { get { return GetDouble("cowl_length_m"); } }
        public double CowlHeight { get { return GetDouble("cowl_height_m"); } }
        public double CowlOuterY { get { return GetDouble("cowl_outer_y_m"); } }
        public double CowlInnerY { get { return GetDouble("cowl_inner_y_m"); } }
        public double OilPanelX { get { return GetDouble("oil_panel_x_m"); } }
        public double OilPanelZ { get { return GetDouble("oil_panel_z_m"); } }
        public double OilPanelLength { get { return GetDouble("oil_panel_length_m"); } }
        public double OilPanelHeight { get { return GetDouble("oil_panel_height_m"); } }
        public double FirewallX { get { return GetDouble("firewall_x_m"); } }
        public double FirewallGrooveWidth { get { return GetDouble("firewall_groove_width_m"); } }

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
                values[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
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
            if (Sections.Count < 10) throw new InvalidDataException("Se requieren al menos 10 secciones OML");
            if (FairingSections.Count < 5) throw new InvalidDataException("Se requieren al menos 5 secciones de fairing");
            if (Math.Abs((XAft - XFront) - Length) > 0.002) throw new InvalidDataException("L_NAC no coincide con extremos locales X");
            if (Sections[0].X < XFront - 0.001 || Sections[0].X > XFront + 0.150) throw new InvalidDataException("La primera seccion debe quedar cerca del plano de helice");
            if (Math.Abs(Sections[Sections.Count - 1].X - XAft) > 0.002) throw new InvalidDataException("Ultima seccion no coincide con X trasero local");

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

            double ellipseArea = Math.PI * IntakeWidth * IntakeHeight / 4.0;
            if (ellipseArea + 0.001 < IntakeRequiredArea)
                throw new InvalidDataException("La toma principal no alcanza el area de captura requerida");

            double aftRatio = (GlobalAft - LeadingEdgeX) / LocalChord;
            if (aftRatio > 0.535)
                throw new InvalidDataException("La nacela termina demasiado atras: x/c=" + aftRatio.ToString("0.000", CultureInfo.InvariantCulture));

            if (EngineWidth > MaxWidth - 0.180)
                throw new InvalidDataException("No queda margen lateral suficiente para la envolvente del motor");
            if (ExhaustInnerY >= ExhaustOuterY)
                throw new InvalidDataException("Geometria de escape invalida");
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

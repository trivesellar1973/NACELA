using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace NacelleSolidWorks
{
    internal sealed class B1Section
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

    internal sealed class B1SaddleSection
    {
        public string Name;
        public double X;
        public double Width;
        public double ZBottom;
        public double ZTop;
    }

    internal sealed class B1Config
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
        public double WingGap { get { return D("wing_gap_m"); } }
        public double LocalChord { get { return D("local_chord_m"); } }

        public double XFront { get { return D("x_front_local_m"); } }
        public double XAft { get { return D("x_aft_local_m"); } }
        public double Length { get { return D("length_m"); } }
        public double MaxWidth { get { return D("max_width_m"); } }
        public double MaxHeight { get { return D("max_height_m"); } }
        public double GlobalFront { get { return AssemblyX + XFront; } }
        public double GlobalAft { get { return AssemblyX + XAft; } }

        public double EngineLength { get { return D("engine_length_m"); } }
        public double EngineWidth { get { return D("engine_width_m"); } }
        public double EngineHeight { get { return D("engine_height_m"); } }

        public double IntakeRequiredArea { get { return D("intake_required_area_m2"); } }
        public double IntakeOpeningWidth { get { return D("intake_opening_width_m"); } }
        public double IntakeOpeningHeight { get { return D("intake_opening_height_m"); } }
        public double IntakeCornerRadius { get { return D("intake_corner_radius_m"); } }
        public double IntakeXFront { get { return D("intake_x_front_m"); } }
        public double IntakeXMid { get { return D("intake_x_mid_m"); } }
        public double IntakeXInterface { get { return D("intake_x_interface_m"); } }
        public double IntakeZFront { get { return D("intake_z_front_m"); } }
        public double IntakeZMid { get { return D("intake_z_mid_m"); } }
        public double IntakeZInterface { get { return D("intake_z_interface_m"); } }
        public double IntakeScoopWidthMid { get { return D("intake_scoop_width_mid_m"); } }
        public double IntakeScoopHeightMid { get { return D("intake_scoop_height_mid_m"); } }
        public double IntakeInterfaceWidth { get { return D("intake_interface_width_m"); } }
        public double IntakeInterfaceHeight { get { return D("intake_interface_height_m"); } }

        public double SideInletX { get { return D("side_inlet_x_m"); } }
        public double SideInletZ { get { return D("side_inlet_z_m"); } }
        public double SideInletLength { get { return D("side_inlet_length_m"); } }
        public double SideInletHeight { get { return D("side_inlet_height_m"); } }
        public double SideInletOpenLength { get { return D("side_inlet_open_length_m"); } }
        public double SideInletOpenHeight { get { return D("side_inlet_open_height_m"); } }
        public double SideInletOuterY { get { return D("side_inlet_outer_y_m"); } }
        public double SideInletInnerY { get { return D("side_inlet_inner_y_m"); } }

        public double ExhaustX { get { return D("exhaust_x_m"); } }
        public double ExhaustZ { get { return D("exhaust_z_m"); } }
        public double ExhaustHousingLength { get { return D("exhaust_housing_length_m"); } }
        public double ExhaustHousingHeight { get { return D("exhaust_housing_height_m"); } }
        public double ExhaustNozzleLength { get { return D("exhaust_nozzle_length_m"); } }
        public double ExhaustNozzleHeight { get { return D("exhaust_nozzle_height_m"); } }
        public double ExhaustOuterY { get { return D("exhaust_outer_y_m"); } }
        public double ExhaustHousingInnerY { get { return D("exhaust_housing_inner_y_m"); } }
        public double ExhaustDuctInnerY { get { return D("exhaust_duct_inner_y_m"); } }
        public double ExhaustEquivalentEach { get { return D("exhaust_equiv_each_m"); } }

        public double CrownNacaX { get { return D("crown_naca_x_m"); } }
        public double CrownNacaY { get { return D("crown_naca_y_m"); } }
        public double CrownNacaZOuter { get { return D("crown_naca_z_outer_m"); } }
        public double CrownNacaZInner { get { return D("crown_naca_z_inner_m"); } }
        public double CrownNacaLength { get { return D("crown_naca_length_m"); } }
        public double CrownNacaWidth { get { return D("crown_naca_width_m"); } }

        public double CowlX { get { return D("cowl_x_m"); } }
        public double CowlZ { get { return D("cowl_z_m"); } }
        public double CowlLength { get { return D("cowl_length_m"); } }
        public double CowlHeight { get { return D("cowl_height_m"); } }
        public double CowlOuterY { get { return D("cowl_outer_y_m"); } }
        public double CowlInnerY { get { return D("cowl_inner_y_m"); } }
        public double ServicePanelX { get { return D("service_panel_x_m"); } }
        public double ServicePanelZ { get { return D("service_panel_z_m"); } }
        public double ServicePanelLength { get { return D("service_panel_length_m"); } }
        public double ServicePanelHeight { get { return D("service_panel_height_m"); } }
        public double FirewallX { get { return D("firewall_x_m"); } }
        public double FirewallGrooveWidth { get { return D("firewall_groove_width_m"); } }

        public double SkinR { get { return D("skin_r"); } }
        public double SkinG { get { return D("skin_g"); } }
        public double SkinB { get { return D("skin_b"); } }

        public readonly List<B1Section> Sections = new List<B1Section>();
        public readonly List<B1SaddleSection> SaddleSections = new List<B1SaddleSection>();

        public static B1Config Load(string repositoryRoot)
        {
            B1Config cfg = new B1Config();
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
                if (pair.Key.StartsWith("section.", StringComparison.OrdinalIgnoreCase))
                {
                    string[] p = pair.Value.Split(';');
                    if (p.Length != 7) throw new InvalidDataException(pair.Key + " debe tener 7 campos");
                    Sections.Add(new B1Section
                    {
                        Name = pair.Key.Substring("section.".Length),
                        X = Parse(p[0]), Width = Parse(p[1]), Height = Parse(p[2]), ZCenter = Parse(p[3]),
                        NSide = Parse(p[4]), NTop = Parse(p[5]), NBottom = Parse(p[6])
                    });
                }
                else if (pair.Key.StartsWith("saddle.", StringComparison.OrdinalIgnoreCase))
                {
                    string[] p = pair.Value.Split(';');
                    if (p.Length != 4) throw new InvalidDataException(pair.Key + " debe tener 4 campos");
                    SaddleSections.Add(new B1SaddleSection
                    {
                        Name = pair.Key.Substring("saddle.".Length),
                        X = Parse(p[0]), Width = Parse(p[1]), ZBottom = Parse(p[2]), ZTop = Parse(p[3])
                    });
                }
            }
            Sections.Sort(delegate(B1Section a, B1Section b) { return a.X.CompareTo(b.X); });
            SaddleSections.Sort(delegate(B1SaddleSection a, B1SaddleSection b) { return a.X.CompareTo(b.X); });
        }

        private void Validate()
        {
            if (!String.Equals(Revision, "B1", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("La configuracion activa debe ser B1");
            if (Sections.Count < 12) throw new InvalidDataException("B1 requiere al menos 12 secciones OML");
            if (SaddleSections.Count < 8) throw new InvalidDataException("B1 requiere al menos 8 secciones saddle");
            if (Math.Abs((XAft - XFront) - Length) > 0.001) throw new InvalidDataException("Longitud inconsistente");
            if (Math.Abs(Sections[Sections.Count - 1].X - XAft) > 0.001) throw new InvalidDataException("La ultima seccion no coincide con el extremo trasero");

            double maxW = 0.0;
            double maxH = 0.0;
            foreach (B1Section section in Sections)
            {
                maxW = Math.Max(maxW, section.Width);
                maxH = Math.Max(maxH, section.Height);
                if (section.Width <= 0.0 || section.Height <= 0.0) throw new InvalidDataException("Seccion invalida: " + section.Name);
            }
            if (Math.Abs(maxW - MaxWidth) > 0.002) throw new InvalidDataException("Ancho maximo B1 inconsistente");
            if (Math.Abs(maxH - MaxHeight) > 0.002) throw new InvalidDataException("Altura maxima B1 inconsistente");

            double roundedArea = IntakeOpeningWidth * IntakeOpeningHeight -
                (4.0 - Math.PI) * IntakeCornerRadius * IntakeCornerRadius;
            if (roundedArea < IntakeRequiredArea)
                throw new InvalidDataException("La toma B1 no alcanza el area requerida");

            double aftRatio = (GlobalAft - LeadingEdgeX) / LocalChord;
            if (aftRatio > 0.530) throw new InvalidDataException("La cola supera x/c=0.53");
            if (EngineWidth + 0.180 > MaxWidth) throw new InvalidDataException("Falta margen lateral de motor");
            if (EngineHeight > MaxHeight) throw new InvalidDataException("La envolvente de motor supera la altura de nacela");
        }

        private string Get(string key)
        {
            string value;
            if (!values.TryGetValue(key, out value)) throw new KeyNotFoundException("Falta parametro B1: " + key);
            return value;
        }

        private double D(string key) { return Parse(Get(key)); }
        private static double Parse(string value) { return double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture); }
        private static string Expand(string value) { return Environment.ExpandEnvironmentVariables(value); }
    }
}

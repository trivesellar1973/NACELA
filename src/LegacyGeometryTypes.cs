namespace NacelleSolidWorks
{
    // Tipos mínimos conservados únicamente porque SwGeometry contiene dos helpers
    // genéricos antiguos con estas firmas. El flujo B1 no los instancia ni los usa.
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
}

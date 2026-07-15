namespace NacelleSolidWorks
{
    // Tipos de compatibilidad exclusivos de la infraestructura comun.
    // La geometria activa B1 usa B1Section y B1SaddleSection en B1Geometry.
    // Estas clases solo permiten compilar las firmas antiguas aun presentes en
    // SwGeometry; no se cargan desde config ni intervienen en el modelo B1.
    internal sealed class NacelleSection
    {
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
        public double X;
        public double Width;
        public double ZBottom;
        public double ZTop;
    }
}

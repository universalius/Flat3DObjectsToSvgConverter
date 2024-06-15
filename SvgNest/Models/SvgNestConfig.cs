namespace SvgNest.Models
{
    public class SvgNestDocument
    {
        public int Width { get; set; } = 300;
        public int Height { get; set; } = 400;
    }

    public class SvgNestRotations
    {
        public int Count { get; set; } = 4;
        public bool UseOnlyOddAngles { get; set; }
    }

    public class SvgNestConfig
    {
        public double CurveTolerance { get; set; } = 0.3;
        public double Spacing { get; set; } = 0;
        public SvgNestRotations Rotations { get; set; } = new SvgNestRotations();
        public int PopulationSize { get; set; } = 10;
        public int MutationRate { get; set; } = 10;
        public bool UseHoles { get; set; } = false;
        public bool ExploreConcave { get; set; } = false;
        public SvgNestDocument Document { get; set; } = new SvgNestDocument();
    }
}

namespace SvgNest.Models
{
    public class SvgNestDocument
    {
        public int Width { get; set; } = 300;
        public int Height { get; set; } = 400;
    }

    public class SvgNestConfig
    {
        public double CurveTolerance { get; set; } = 0.3;
        public int Spacing { get; set; } = 0;
        public int Rotations { get; set; } = 4;
        public int PopulationSize { get; set; } = 10;
        public int MutationRate { get; set; } = 10;
        public bool UseHoles { get; set; } = false;
        public bool ExploreConcave { get; set; } = false;
        public SvgNestDocument Document { get; set; } = new SvgNestDocument();
    }
}

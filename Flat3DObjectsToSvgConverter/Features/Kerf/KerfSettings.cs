namespace Flat3DObjectsToSvgConverter.Features.Kerf;

public class BeamSize
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class KerfSettings
{
    public bool Enabled { get; set; } = false;

    public BeamSize BeamSize { get; set; }
}

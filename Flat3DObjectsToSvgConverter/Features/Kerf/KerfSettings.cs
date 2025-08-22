namespace Flat3DObjectsToSvgConverter.Features.Kerf;

public class BeamSize
{
    public double Width { get; set; }
    public double Height { get; set; }
}

public class BeamCenter
{
    public double Y { get; set; }
}

public class KerfSettings
{
    public bool Enabled { get; set; } = false;

    public BeamSize BeamSize { get; set; }

    public BeamCenter BeamCenter { get; set; }
}

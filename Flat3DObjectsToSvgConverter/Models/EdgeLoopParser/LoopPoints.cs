using GeometRi;

namespace Flat3DObjectsToSvgConverter.Models.EdgeLoopParser;

public class LoopPoints
{
    public IEnumerable<Point3d> Points { get; set; }

    public bool IsResized { get; set; }

    public LoopPoints Clone()
    {
        var newPoints = new List<Point3d>(Points.Select(p => new Point3d(p.X, p.Y, p.Z)));
        var clone = new LoopPoints
        {
            Points = newPoints,
            IsResized = IsResized
        };

        return clone;
    }
}

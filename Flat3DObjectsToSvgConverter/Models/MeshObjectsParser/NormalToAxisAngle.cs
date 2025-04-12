using GeometRi;
using System.Drawing;

namespace Flat3DObjectsToSvgConverter.Models.MeshObjectsParser
{
    public class NormalToAxisAngle
    {
        public string Axis { get; set; }
        public double Angle { get; set; }
        public Vector3d Vector { get; set; }
        public Point NormalPoint { get; set; }
        public List<AxisOrientation> OrthogonalAxises { get; set; }
    }
}

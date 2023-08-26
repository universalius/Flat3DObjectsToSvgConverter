using Flat3DObjectsToSvgConverter.Services;
using GeometRi;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flat3DObjectsToSvgConverter.Models.ObjectsLabelsPreciseLocatorAndSvgConverter
{
    public class PathIntersection
    {
        public LoopPolygon LoopPolygon { get; set; }
        public DoublePoint IntersectionPoint { get; set; }
        public DoublePoint[] IntersectionLine { get; set; }
        public bool HasIntersection { get; set; }

    }

}

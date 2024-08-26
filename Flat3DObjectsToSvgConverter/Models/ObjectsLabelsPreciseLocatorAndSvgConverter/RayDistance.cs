using ClipperLib;
using Flat3DObjectsToSvgConverter.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flat3DObjectsToSvgConverter.Models.ObjectsLabelsPreciseLocatorAndSvgConverter
{
    public class RayDistance
    {
        public DoublePoint[] RaySegment { get; set; }
        public int RayId { get; set; }
        public PathIntersection Main { get; set; }
        public LoopPolygon Neighbor { get; set; }
        public double Distance { get; set; }
        public bool HasIntersection { get; set; }
    }
}

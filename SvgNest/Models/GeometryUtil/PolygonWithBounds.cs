using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvgNest.Models.GeometryUtil
{
    public class PolygonWithBounds
    {
        public int Id { get; set; }
        public DoublePoint[] Points { get; set; }

        public PolygonBounds Bounds { get; set; }
    }
}

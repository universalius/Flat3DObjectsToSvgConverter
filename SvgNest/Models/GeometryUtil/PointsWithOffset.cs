using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvgNest.Models.GeometryUtil
{
    public class PointsWithOffset
    {
        public DoublePoint[] Points { get; set; }
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }

        public PointsWithOffset Clone()
        {
            return new PointsWithOffset
            {
                Points = Points.ToList().ToArray(),
                OffsetX = (double)OffsetX,
                OffsetY = (double)OffsetY
            };
        }
    }
}

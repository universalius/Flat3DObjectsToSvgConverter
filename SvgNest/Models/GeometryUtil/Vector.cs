using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvgNest.Models.GeometryUtil
{
    public class Vector : DoublePoint
    {
        public PointWithMark Start { get; set; }
        public PointWithMark End { get; set; }
    }
}

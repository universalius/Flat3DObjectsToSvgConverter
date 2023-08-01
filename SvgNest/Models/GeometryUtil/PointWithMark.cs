using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvgNest.Models.GeometryUtil
{
    public class PointWithMark : DoublePoint
    {
        public bool Marked { get; set; }
    }
}

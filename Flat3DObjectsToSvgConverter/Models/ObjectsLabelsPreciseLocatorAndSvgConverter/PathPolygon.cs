using ClipperLib;
using SvgNest.Models.GeometryUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flat3DObjectsToSvgConverter.Models.ObjectsLabelsPreciseLocatorAndSvgConverter
{
    public class PathPolygon : PolygonWithBounds
    {
        public DoublePoint[][] Lines { get; set; }
        public DoublePoint Center { get; set; }
    }
}

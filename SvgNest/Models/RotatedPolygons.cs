using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DPath = System.Collections.Generic.List<ClipperLib.DoublePoint>;


namespace SvgNest.Models
{
    public class RotatedPolygons
    {
        public DPath Points { get; set; }

        public List<RotatedPolygons> Children { get; set; }

        public double Rotation { get; set; }

        public int Source { get; set; }

        public int Id { get; set; }
    }
}

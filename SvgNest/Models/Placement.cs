using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;

namespace SvgNest.Models
{
    public class Placement : DoublePoint
    {
        public int Id { get; set; }
        public double Rotation { get; set; }
        public List<Path> Nfp { get; set; }
    }
}

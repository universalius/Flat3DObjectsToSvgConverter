using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SvgNest.PlacementWorker;

namespace SvgNest.Models
{
    public class PlacementsFitness : DoublePoint
    {
        public List<List<Placement>> Placements { get; set; }
        public List<RotatedPolygons> Paths { get; set; }
        public double Fitness { get; set; }
        public double Area { get; set; }
    }
}

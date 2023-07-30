using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvgNest.Models.SvgNest
{
    public class Node
    {
        public int id { get; set; }

        public List<DoublePoint> points { get; set; }

        public List<Node> children { get; set; }

        public Node parent { get; set; }

        public double rotation { get; set; }

        public int source { get; set; }
    }
}

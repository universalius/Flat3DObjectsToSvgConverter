using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvgNest.Models
{
    public class Node
    {
        public int Id { get; set; }

        public List<DoublePoint> Points { get; set; }

        public List<Node> Children { get; set; }

        public Node Parent { get; set; }

        public double Rotation { get; set; }

        public int Source { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvgNest.Models
{
    public class NodesPair
    {
        public Node A { get; set; }
        public Node B { get; set; }
        public SvgNestPair Key { get; set; }
    }
}

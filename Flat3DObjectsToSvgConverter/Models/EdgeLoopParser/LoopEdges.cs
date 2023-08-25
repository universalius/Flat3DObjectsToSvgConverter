using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flat3DObjectsToSvgConverter.Models.EdgeLoopParser
{
    public class LoopEdges
    {
        public int Id { get; set; }
        public IEnumerable<EdgeFace> Edges { get; set; }
    }
}

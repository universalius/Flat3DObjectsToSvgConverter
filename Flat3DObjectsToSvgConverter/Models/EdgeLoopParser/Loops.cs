using Flat3DObjectsToSvgConverter.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flat3DObjectsToSvgConverter.Models.EdgeLoopParser
{
    public class Loops
    {
        public LoopEdges Main { get; set; }
        public IEnumerable<LoopEdges> Children { get; set; }
    }
}

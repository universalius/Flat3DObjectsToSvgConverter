using ObjParser.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flat3DObjectsToSvgConverter.Models.EdgeLoopParser
{
    public class VertexFaces
    {
        public Vertex Vertex { get; set; }
        public IEnumerable<Face> Faces { get; set; }
    }
}

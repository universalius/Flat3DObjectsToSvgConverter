using ObjParser.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flat3DObjectsToSvgConverter.Models.MeshObjectsParser
{
    public class MeshObject
    {
        public IEnumerable<Vertex> Verts { get; set; }
        public IEnumerable<Face> Faces { get; set; }
        public string Axis { get; set; }
    }
}

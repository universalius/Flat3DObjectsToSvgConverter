using ObjParser.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flat3DObjectsToSvgConverter.Models.EdgeLoopParser
{
    public class EdgeFace
    {
        public Vertex FirstVertex { get; set; }
        public Vertex SecondVertex { get; set; }
        public Face Face { get; set; }

        public Vertex[] Edge => new[] { FirstVertex, SecondVertex };
    }
}

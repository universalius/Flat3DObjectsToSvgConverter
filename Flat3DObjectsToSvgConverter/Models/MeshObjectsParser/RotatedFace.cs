using GeometRi;
using ObjParser.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flat3DObjectsToSvgConverter.Models.MeshObjectsParser
{
    public class RotatedFace
    {
        public Face Face { get; set; }
        public List<Vertex> Verts { get; set; }
        public Plane3d Plane { get; set; }
        public Point3d RoundedAngles { get; set; }
        public Point3d Angles { get; set; }
        public Point3d NormalDirection { get; set; }
    }
}

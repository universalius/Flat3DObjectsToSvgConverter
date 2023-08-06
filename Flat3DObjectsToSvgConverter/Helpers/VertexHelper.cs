using ObjParser.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flat3DObjectsToSvgConverter.Helpers
{
    public static class VertexHelper
    {
        public const int ScaleGain= 100000;

        public static int ToInt(this double coord, int scale = ScaleGain) {
            return (int)(coord * scale);
        }

        public static Vertex ToIntCoords(this Vertex vertex, int scale = ScaleGain)
        {
            return new Vertex
            {
                Index = vertex.Index,
                X = vertex.X.ToInt(scale),
                Y = vertex.Y.ToInt(scale),
                Z = vertex.Z.ToInt(scale),
            };
        }
    }
}

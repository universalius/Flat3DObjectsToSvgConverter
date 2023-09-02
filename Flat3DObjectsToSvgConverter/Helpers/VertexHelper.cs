using ClipperLib;
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
        public const int ScaleGain = 100000;

        public static int ToInt(this double coord, int scale = ScaleGain, bool round = false)
        {
            var scaled = coord * scale;
            return round ? (int)Math.Round(scaled, 1) : (int)scaled;
        }

        public static DoublePoint ToInt(this DoublePoint point, int scale)
        {
            return new DoublePoint((int)point.X * scale, (int)point.Y * scale);
        }

        public static DoublePoint Scale(this DoublePoint point, double scale)
        {
            return new DoublePoint(point.X * scale, point.Y * scale);
        }

        public static Vertex ToIntCoords(this Vertex vertex, int scale = ScaleGain, bool round = false)
        {
            return new Vertex
            {
                Index = vertex.Index,
                X = vertex.X.ToInt(scale, round),
                Y = vertex.Y.ToInt(scale, round),
                Z = vertex.Z.ToInt(scale, round),
            };
        }
    }
}

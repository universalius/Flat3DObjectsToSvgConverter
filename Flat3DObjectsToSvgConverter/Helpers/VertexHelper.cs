using ClipperLib;
using GeometRi;
using ObjParser.Types;
using System.Drawing;

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
            return new DoublePoint(Math.Floor(point.X * scale), Math.Floor(point.Y * scale));
        }

        public static DoublePoint Scale(this DoublePoint point, double scale)
        {
            return new DoublePoint(point.X * scale, point.Y * scale);
        }

        public static Point3d ToPoint3d(this DoublePoint point, double scale = 1)
        {
            return new Point3d((int)point.X * scale, (int)point.Y * scale, 0);
        }

        public static Point3d ToPoint3d(this PointF point)
        {
            return new Point3d(point.X, point.Y, 0);
        }

        public static PointF ToPointF(this Point3d point)
        {
            return new PointF((float)point.X, (float)point.Y);
        }

        public static DoublePoint ToDoublePoint(this Point3d point)
        {
            return new DoublePoint(point.X, point.Y);
        }

        public static DoublePoint ToDoublePoint(this PointF point)
        {
            return new DoublePoint(point.X, point.Y);
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

        public static PointF[] ToPointFs(this Segment3d[] segments)
        {
            var newPoints = new List<PointF> { segments[0].P1.ToPointF() };

            newPoints.AddRange(segments.Select((s, j) => s.P2.ToPointF()));

            return newPoints.ToArray();
        }
    }
}

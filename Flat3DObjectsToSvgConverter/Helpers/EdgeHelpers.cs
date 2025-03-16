using ClipperLib;
using Flat3DObjectsToSvgConverter.Models.EdgeLoopParser;
using GeometRi;


namespace Flat3DObjectsToSvgConverter.Helpers
{
    public static class EdgeHelpers
    {
        public static DoublePoint[][] ToEdges(this DoublePoint[] points, int? scale = null)
        {
            var count = points.Count();
            var edges = points.Select((p, i) =>
            {
                if (i == count - 1)
                    return new DoublePoint[2] { new DoublePoint(p.X, p.Y), new DoublePoint(points[0].X, points[0].Y) };

                return new DoublePoint[2] { new DoublePoint(p.X, p.Y), new DoublePoint(points[i + 1].X, points[i + 1].Y) };
            }).ToArray();

            if (scale != null && scale != 1)
            {
                edges = edges.Select((e, i) => e.Select(p => p.ToInt(scale.Value)).ToArray()).ToArray();
            }

            return edges;
        }

        public static Segment3d[] ToSegments(this LoopPoints loop)
        {
            var points = loop.Points.ToArray();
            var doublePoints = points.Select(p => p.ToDoublePoint()).ToArray();
            var pointsCount = points.Count();
            var segments = points.Select((p, j) =>
            {
                var nextPointIndex = j + 1;
                return nextPointIndex != pointsCount ?
                    new Segment3d(new Point3d(p.X, p.Y, 0), new Point3d(points[nextPointIndex].X, points[nextPointIndex].Y, 0)) :
                    null;
            }).Where(l => l != null).ToArray();

            return segments;
        }
    }
}

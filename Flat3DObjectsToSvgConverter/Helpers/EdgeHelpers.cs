using ClipperLib;


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
    }
}

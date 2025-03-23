using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models;
using Flat3DObjectsToSvgConverter.Models.EdgeLoopParser;
using GeometRi;
using System.Drawing;

namespace Flat3DObjectsToSvgConverter.Services
{
    public class ObjectLoopsPointsReducer
    {
        public ObjectLoopsPointsReducer() { }

        public void RemoveRedundantPoints(IEnumerable<MeshObjects> meshes)
        {
            meshes.ToList().ForEach(mesh =>
            {
                mesh.Objects.ToList().ForEach(obj =>
                {
                    foreach (var (l, i) in obj.Loops.Select((v, i) => (v, i)))
                    {
                        var reducedPoints = RemoveDuplicatedPoints(l, i, mesh);

                        var segments = reducedPoints.ToSegments().ToList();
                        var nextSegmentIndex = 1;
                        var notReducedSegmentsCount = segments.Count;

                        for (int k = 0; k < segments.Count(); k++)
                        {
                            if (nextSegmentIndex == segments.Count())
                            {
                                break;
                            }

                            var segment = segments[k];
                            var nextSegment = segments[nextSegmentIndex];

                            if (segment.ToLine.DistanceTo(nextSegment.P2) <= 0.01)
                            {
                                segments.RemoveRange(k, 2);
                                segments.Insert(k, new Segment3d(segment.P1, nextSegment.P2));
                                k--;
                            }
                            else
                            {
                                nextSegmentIndex++;
                            }
                        }

                        var firstSegment = segments.First();
                        var lastSegment = segments.Last();
                        if (lastSegment.P1.BelongsTo(firstSegment.ToLine))
                        {
                            segments.Remove(firstSegment);
                            segments.Remove(lastSegment);
                            segments.Add(new Segment3d(lastSegment.P1, firstSegment.P2));
                        }

                        if (notReducedSegmentsCount != segments.Count())
                        {
                            var newPoints = segments.ToArray().ToPoint3ds();

                            Console.WriteLine($"    Removed {l.Points.Count() - newPoints.Length} redundant point(s) " +
                                $"for mesh {mesh.MeshName} loop {i}");

                            l.Points = newPoints;
                        }
                    }
                });
            });

            Console.WriteLine();
        }

        private Point3d[] RemoveDuplicatedPoints(LoopPoints l, int i, MeshObjects mesh)
        {
            var points = l.Points.ToArray();
            var reducedPoints = points.ToArray().Select((p, j) =>
            {
                if (j == points.Length - 1)
                {
                    return p;
                }

                var tolerance = 0.02;
                var next = points[j + 1];
                var xSame = Math.Abs(p.X - next.X) <= tolerance;
                var ySame = Math.Abs(p.Y - next.Y) <= tolerance;

                if (xSame && ySame)
                {
                    return null;
                }

                return p;
            }).Where(p => p != null).ToArray();

            if (points.Count() != reducedPoints.Length)
            {
                Console.WriteLine($"    Removed {points.Count() - reducedPoints.Length} duplicated point(s) " +
        $"for mesh {mesh.MeshName} loop {i}");
            }

            return reducedPoints;
        }
    }
}

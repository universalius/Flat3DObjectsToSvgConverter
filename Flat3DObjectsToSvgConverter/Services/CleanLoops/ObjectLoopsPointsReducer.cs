using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models;
using GeometRi;

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
                        var segments = l.ToSegments().ToList();
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
                            var newPoints = segments.ToArray().ToPointFs();

                            Console.WriteLine($"    Removed {l.Points.Count() - newPoints.Length} redundant point(s) " +
                                $"for mesh {mesh.MeshName} loop {i}");

                            l.Points = newPoints;
                        }
                    }
                });
            });

            Console.WriteLine();
        }
    }
}

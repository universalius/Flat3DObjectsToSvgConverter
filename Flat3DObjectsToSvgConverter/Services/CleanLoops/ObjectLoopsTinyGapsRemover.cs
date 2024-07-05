using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models;
using Flat3DObjectsToSvgConverter.Models.EdgeLoopParser;
using GeometRi;
using System.Drawing;

namespace Flat3DObjectsToSvgConverter.Services
{
    public class ObjectLoopsTinyGapsRemover
    {
        public ObjectLoopsTinyGapsRemover() { }

        public void ReplaceGapsWithLine(IEnumerable<MeshObjects> meshes)
        {
            meshes.ToList().ForEach(mesh =>
            {
                mesh.Objects.ToList().ForEach(obj =>
                {
                    var mainLoop = obj.Loops.First();

                    var points = mainLoop.Points.ToArray();
                    var pointsCount = points.Count();
                    var segments = points.Select((p, j) =>
                    {
                        var nextPointIndex = j + 1;
                        return nextPointIndex != pointsCount ?
                            new Segment3d(new Point3d(p.X, p.Y, 0), new Point3d(points[nextPointIndex].X, points[nextPointIndex].Y, 0)) :
                            null;
                    }).Where(l => l != null).ToList();

                    var segmentsCount = segments.Count;
                    var gapsSegments = segments.Select(s =>
                    {
                        if (s.Length <= 0.5)
                        {
                            var tinySegmentIndex = segments.IndexOf(s);
                            var leftSegmentIndex = tinySegmentIndex - 1 < 0 ? segments.Count - 1 : tinySegmentIndex - 1;
                            var leftSegment = segments[leftSegmentIndex];

                            var rightSegmentIndex = tinySegmentIndex + 1 < segments.Count ? tinySegmentIndex + 1 : 0;
                            var rightSegment = segments[rightSegmentIndex];

                            var firstAngle = leftSegment.AngleToDeg(s);
                            var secondAngle = rightSegment.AngleToDeg(s);
                            if (IsOrtogonal(firstAngle) && IsOrtogonal(secondAngle))
                            {
                                var vectorsFacingOppositeDirection = leftSegment.ToVector.Dot(rightSegment.ToVector) < 0;

                                return vectorsFacingOppositeDirection ? new List<Segment3d>
                                {
                                    leftSegment, s, rightSegment
                                } : null;
                            }

                            return null;
                        }

                        return null;
                    }).Where(s => s != null).ToList();

                    gapsSegments.ForEach(gs =>
                    {
                        var firstSegment = gs[0];
                        var secondSegment = gs[1];
                        var thirdSegment = gs[2];

                        var firstSegmentIndex = segments.IndexOf(firstSegment);
                        segments.Insert(firstSegmentIndex, new Segment3d(firstSegment.P1, thirdSegment.P2));

                        gs.ForEach(s =>
                        {
                            segments.Remove(s);
                        });

                        mainLoop.Points = segments.Select((s, j) => s.P2.ToPointF());

                        var allLoops = new List<LoopPoints>();
                        allLoops.AddRange(obj.Loops);

                        var shiftVector = secondSegment.ToVector.Mult(0.5);

                        var gapSector = firstSegment.Translate(shiftVector);
                        allLoops.Add(new LoopPoints
                        {
                            Points = new List<PointF>()
                            {
                                gapSector.P1.ToPointF(),
                                gapSector.P2.ToPointF()
                            }
                        });

                        obj.Loops = allLoops;

                        Console.WriteLine($"    Removed tiny gap for mesh {mesh.MeshName} main loop");
                    });
                });
            });

            Console.WriteLine();
        }

        private bool IsOrtogonal(double angle)
        {
            return (angle > 89 && angle <= 90) || (angle >= 0 && angle <= 1);
        }
    }
}

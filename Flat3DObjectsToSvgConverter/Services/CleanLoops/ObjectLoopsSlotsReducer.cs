using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models;
using Flat3DObjectsToSvgConverter.Models.EdgeLoopParser;
using GeometRi;
using SvgNest.Utils;
using System.Drawing;

namespace Flat3DObjectsToSvgConverter.Services
{
    public class ObjectLoopsSlotsReducer
    {
        public ObjectLoopsSlotsReducer() { }

        public void CloseSlots(IEnumerable<MeshObjects> meshes)
        {
            meshes.ToList().ForEach(mesh =>
            {
                mesh.Objects.ToList().ForEach(obj =>
                {
                    var mainLoop = obj.Loops.First();

                    var points = mainLoop.Points.ToArray();
                    var doublePoints = points.Select(p => p.ToDoublePoint()).ToArray();
                    var pointsCount = points.Count();
                    var segments = points.Select((p, j) =>
                    {
                        var nextPointIndex = j + 1;
                        return nextPointIndex != pointsCount ?
                            new Segment3d(new Point3d(p.X, p.Y, 0), new Point3d(points[nextPointIndex].X, points[nextPointIndex].Y, 0)) :
                            null;
                    }).Where(l => l != null).ToList();

                    var ortogonalSegments = new List<Segment3d[]>();
                    for (int k = 0; k < segments.Count(); k++)
                    {
                        var firstSegment = segments[k];
                        var secondSegment = k + 1 < segments.Count() ? segments[k + 1] : segments[0];
                        var angle = firstSegment.AngleToDeg(secondSegment);
                        if (IsOrtogonal(angle))
                        {
                            ortogonalSegments.Add(new[] { firstSegment, secondSegment });
                        }
                    }

                    var axisZVector = new Vector3d(0.0, 0.0, 1.0);

                    var slotCandidates = ortogonalSegments.SelectMany(s => s)
                    .Distinct()
                    .Select(s => new
                    {
                        Segment = s,
                        Neighbors = ortogonalSegments.Where(pair => pair.Contains(s))
                    })
                    .Where(ol => ol.Neighbors.Count() >= 2)
                    .Where(p =>
                    {
                        var segment = p.Segment;
                        var shiftVector = segment.Direction.Rotate(new Rotation(axisZVector, 90.0 * Math.PI / 180));
                        var shiftedSector = segment.Translate(shiftVector.Normalized.Mult(0.1));

                        var neighborVectors = p.Neighbors.Select(n => n.Except(new[] { segment }).First().ToVector);

                        var vectorsFacingOppositeDirection = neighborVectors.First().Dot(neighborVectors.Last()) < 0;

                        var a = GeometryUtil.PointInPolygon(shiftedSector.P1.ToDoublePoint(), doublePoints) ?? false;
                        return vectorsFacingOppositeDirection && a;
                    }).ToList();


                    slotCandidates.ForEach(sc =>
                    {
                        //var neighbors = sc.Neighbors.Select(n => n.Except(new[] { sc.Segment }).First());

                        var firstSegment = sc.Neighbors.First(pair => pair[1].Equals(sc.Segment))[0];
                        var secondSegment = sc.Segment;
                        var thirdSegment = sc.Neighbors.First(pair => pair[0].Equals(sc.Segment))[1];

                        var firstSegmentIndex = segments.IndexOf(firstSegment);
                        segments.Insert(firstSegmentIndex, new Segment3d(firstSegment.P1, thirdSegment.P2));

                        new List<Segment3d> { firstSegment, secondSegment, thirdSegment }.ForEach(s =>
                        {
                            segments.Remove(s);
                        });

                        mainLoop.Points = segments.Select((s, j) => s.P2.ToPointF());

                        var allLoops = new List<LoopPoints>();
                        allLoops.AddRange(obj.Loops);

                        allLoops.Add(new LoopPoints
                        {
                            Points = new List<PointF>()
                            {
                                firstSegment.P1.ToPointF(),
                                firstSegment.P2.ToPointF(),
                                secondSegment.P2.ToPointF(),
                                thirdSegment.P2.ToPointF(),
                            }
                        });

                        obj.Loops = allLoops;
                    });



                    //gapsSegments.ForEach(gs => 
                    //{
                    //    var firstSegment = gs[0];
                    //    var secondSegment = gs[1];
                    //    var thirdSegment = gs[2];

                    //    var firstSegmentIndex = segments.IndexOf(firstSegment);
                    //    segments.Insert(firstSegmentIndex, new Segment3d(firstSegment.P1, thirdSegment.P2));

                    //    gs.ForEach(s =>
                    //    {
                    //        segments.Remove(s);
                    //    });

                    //    mainLoop.Points = segments.Select((s, j) => s.P2.ToPointF());

                    //    var allLoops = new List<LoopPoints>();
                    //    allLoops.AddRange(obj.Loops);

                    //    var shiftVector = secondSegment.ToVector.Mult(0.5);

                    //    var gapSector = firstSegment.Translate(shiftVector);
                    //    allLoops.Add(new LoopPoints
                    //    {
                    //        Points = new List<PointF>()
                    //        {
                    //            gapSector.P1.ToPointF(),
                    //            gapSector.P2.ToPointF()
                    //        }
                    //    });

                    //    obj.Loops = allLoops;

                    //    Console.WriteLine($"    Removed tiny gap for mesh {mesh.MeshName} main loop");
                    //});
                });
            });

            Console.WriteLine();
        }

        private bool IsOrtogonal(double angle)
        {
            return (angle > 89 && angle <= 90);
        }

    }
}

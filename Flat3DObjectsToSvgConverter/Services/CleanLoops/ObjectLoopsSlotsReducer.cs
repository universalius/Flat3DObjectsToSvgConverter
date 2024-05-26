using ClipperLib;
using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models;
using Flat3DObjectsToSvgConverter.Models.EdgeLoopParser;
using GeometRi;
using SvgNest.Utils;
using System.Drawing;
using System.Linq;

namespace Flat3DObjectsToSvgConverter.Services
{
    public class ObjectLoopsSlotsReducer
    {
        private Vector3d _axisZVector = new Vector3d(0.0, 0.0, 1.0);
        private Vector3d _axisYVector = new Vector3d(0.0, 1.0, 0.0);

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

                    if (!ortogonalSegments.Any())
                        return;

                    var rectangularClosingSlotLoops = CloseRectangularSlots(doublePoints, ortogonalSegments, segments, mesh.MeshName);

                    var allLoops = new List<LoopPoints>();
                    allLoops.AddRange(obj.Loops);
                    allLoops.AddRange(rectangularClosingSlotLoops);
                    obj.Loops = allLoops;

                    //var slotCandidates = ortogonalSegments.SelectMany(s => s)
                    //    .Distinct()
                    //    .Select(s => new
                    //    {
                    //        Segment = s,
                    //        Neighbors = ortogonalSegments.Where(pair => pair.Contains(s))
                    //    })
                    //    .Where(ol => ol.Neighbors.Count() >= 2)
                    //    .Where(p =>
                    //    {
                    //        var segment = p.Segment;


                    //        //var shiftVector = segment.Direction.Rotate(new Rotation(_axisZVector, rotationAngle));
                    //        //var shiftedSector = segment.Translate(shiftVector.Normalized.Mult(0.1));

                    //        var neighborSegments = p.Neighbors.Select(n => n.Except(new[] { segment }).First()).ToArray();
                    //        var neighborVectors = neighborSegments.Select(n => n.ToVector).ToArray();

                    //        var vectorsFacingOppositeDirection = neighborVectors.First().Dot(neighborVectors.Last()) < 0;

                    //        var neighborPoints = new Point3d[] { neighborSegments[0].P1, neighborSegments[0].P2, neighborSegments[1].P1, neighborSegments[1].P2 }
                    //            .Where(p => p != segment.P1 && p != segment.P2)
                    //            .ToArray();

                    //        var closingSegment = new Segment3d(neighborPoints[0], neighborPoints[1]).Scale(0.9);

                    //        var a = GeometryUtil.PointInPolygon(closingSegment.P1.ToDoublePoint(), doublePoints) ?? false;
                    //        var a1 = GeometryUtil.PointInPolygon(closingSegment.P2.ToDoublePoint(), doublePoints) ?? false;
                    //        return segment.Length <= 10 && vectorsFacingOppositeDirection && !(a || a1);
                    //    }).ToList();

                    //int slotIndex = 1;
                    ////var reducedSegments = new List<Segment3d>(segments);
                    ////var prevSlotSegments = new List<Segment3d>();
                    //Segment3d prevClosedSlotSegment = null;
                    //List<Point3d> prevOriginalSlotPoints = null;

                    //List<Segment3d> segmentsForDeletion = null;

                    //slotCandidates.ForEach(sc =>
                    //{
                    //    var allLoops = new List<LoopPoints>();
                    //    allLoops.AddRange(obj.Loops);

                    //    try
                    //    {
                    //        var firstSegment = sc.Neighbors.First(pair => pair[1].Equals(sc.Segment))[0];
                    //        var secondSegment = sc.Segment;
                    //        var thirdSegment = sc.Neighbors.First(pair => pair[0].Equals(sc.Segment))[1];

                    //        var firstSegmentIndex = segments.IndexOf(firstSegment);

                    //        Segment3d closedSlotSegment = null;
                    //        List<Point3d> originalSlotPoints = null;

                    //        if (firstSegmentIndex != -1)
                    //        {
                    //            closedSlotSegment = new Segment3d(firstSegment.P1, thirdSegment.P2);
                    //            segments.Insert(firstSegmentIndex, closedSlotSegment);

                    //            segmentsForDeletion = new List<Segment3d> { firstSegment, secondSegment, thirdSegment };

                    //            originalSlotPoints = new List<Point3d> { firstSegment.P1, firstSegment.P2, secondSegment.P2, thirdSegment.P2 };
                    //        }
                    //        else
                    //        {
                    //            var prevClosedSlotSegmentIndex = segments.IndexOf(prevClosedSlotSegment);

                    //            closedSlotSegment = new Segment3d(prevClosedSlotSegment.P1, thirdSegment.P2);
                    //            segments.Insert(prevClosedSlotSegmentIndex, closedSlotSegment);

                    //            segmentsForDeletion = new List<Segment3d> { prevClosedSlotSegment, secondSegment, thirdSegment };

                    //            originalSlotPoints = new List<Point3d>(prevOriginalSlotPoints) { thirdSegment.P2 };

                    //            allLoops.Remove(allLoops.Last());
                    //        }

                    //        prevClosedSlotSegment = closedSlotSegment;
                    //        prevOriginalSlotPoints = originalSlotPoints;

                    //        segmentsForDeletion.ForEach(s =>
                    //        {
                    //            segments.Remove(s);
                    //        });

                    //        //mainLoop.Points = segments.Select((s, j) => s.P2.ToPointF());

                    //        //allLoops.Add(new LoopPoints
                    //        //{
                    //        //    Points = originalSlotPoints.Select(p => p.ToPointF()).ToArray()
                    //        //});


                    //        allLoops.Add(new LoopPoints
                    //        {
                    //            Points = new PointF[] { closedSlotSegment.P1.ToPointF(), closedSlotSegment.P2.ToPointF() }
                    //        });

                    //        obj.Loops = allLoops;
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        Console.WriteLine($"    Failed to close slot {slotIndex} for mesh {mesh.MeshName} main loop");
                    //        throw;
                    //    }

                    //    Console.WriteLine($"    Closed slot {slotIndex} for mesh {mesh.MeshName} main loop");

                    //    slotIndex++;
                    //});
                });
            });

            Console.WriteLine();
        }

        private List<LoopPoints> CloseRectangularSlots(DoublePoint[] doublePoints, List<Segment3d[]> ortogonalSegments, List<Segment3d> segments, string meshName)
        {
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


                    //var shiftVector = segment.Direction.Rotate(new Rotation(_axisZVector, rotationAngle));
                    //var shiftedSector = segment.Translate(shiftVector.Normalized.Mult(0.1));

                    var neighborSegments = p.Neighbors.Select(n => n.Except(new[] { segment }).First()).ToArray();
                    var neighborVectors = neighborSegments.Select(n => n.ToVector).ToArray();

                    var vectorsFacingOppositeDirection = neighborVectors.First().Dot(neighborVectors.Last()) < 0;

                    var neighborPoints = new Point3d[] { neighborSegments[0].P1, neighborSegments[0].P2, neighborSegments[1].P1, neighborSegments[1].P2 }
                        .Where(p => p != segment.P1 && p != segment.P2)
                        .ToArray();

                    var closingSegment = new Segment3d(neighborPoints[0], neighborPoints[1]).Scale(0.9);

                    var a = GeometryUtil.PointInPolygon(closingSegment.P1.ToDoublePoint(), doublePoints) ?? false;
                    var a1 = GeometryUtil.PointInPolygon(closingSegment.P2.ToDoublePoint(), doublePoints) ?? false;
                    return segment.Length <= 10 && vectorsFacingOppositeDirection && !(a || a1);
                }).ToList();

            int slotIndex = 1;
            //var reducedSegments = new List<Segment3d>(segments);
            //var prevSlotSegments = new List<Segment3d>();
            Segment3d prevClosedSlotSegment = null;
            List<Point3d> prevOriginalSlotPoints = null;

            List<Segment3d> segmentsForDeletion = null;

            var closingLoops = new List<LoopPoints>();

            slotCandidates.ForEach(sc =>
            {
                try
                {
                    var firstSegment = sc.Neighbors.First(pair => pair[1].Equals(sc.Segment))[0];
                    var secondSegment = sc.Segment;
                    var thirdSegment = sc.Neighbors.First(pair => pair[0].Equals(sc.Segment))[1];

                    var firstSegmentIndex = segments.IndexOf(firstSegment);

                    Segment3d closedSlotSegment = null;
                    List<Point3d> originalSlotPoints = null;

                    if (firstSegmentIndex != -1)
                    {
                        closedSlotSegment = new Segment3d(firstSegment.P1, thirdSegment.P2);
                        segments.Insert(firstSegmentIndex, closedSlotSegment);

                        segmentsForDeletion = new List<Segment3d> { firstSegment, secondSegment, thirdSegment };

                        originalSlotPoints = new List<Point3d> { firstSegment.P1, firstSegment.P2, secondSegment.P2, thirdSegment.P2 };
                    }
                    else
                    {
                        var prevClosedSlotSegmentIndex = segments.IndexOf(prevClosedSlotSegment);

                        closedSlotSegment = new Segment3d(prevClosedSlotSegment.P1, thirdSegment.P2);
                        segments.Insert(prevClosedSlotSegmentIndex, closedSlotSegment);

                        segmentsForDeletion = new List<Segment3d> { prevClosedSlotSegment, secondSegment, thirdSegment };

                        originalSlotPoints = new List<Point3d>(prevOriginalSlotPoints) { thirdSegment.P2 };

                        closingLoops.Remove(closingLoops.Last());
                    }

                    prevClosedSlotSegment = closedSlotSegment;
                    prevOriginalSlotPoints = originalSlotPoints;

                    segmentsForDeletion.ForEach(s =>
                    {
                        segments.Remove(s);
                    });

                    //mainLoop.Points = segments.Select((s, j) => s.P2.ToPointF());

                    //allLoops.Add(new LoopPoints
                    //{
                    //    Points = originalSlotPoints.Select(p => p.ToPointF()).ToArray()
                    //});

                    Console.WriteLine($"    Closed rectangular slot {slotIndex} for mesh {meshName} main loop");

                    slotIndex++;

                    closingLoops.Add(new LoopPoints
                    {
                        Points = new PointF[] { closedSlotSegment.P1.ToPointF(), closedSlotSegment.P2.ToPointF() }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    Failed to close slot {slotIndex} for mesh {meshName} main loop");
                    throw;
                }
            });

            return closingLoops;
        }

        private bool IsOrtogonal(double angle)
        {
            return (angle > 89 && angle <= 90);
        }
    }
}

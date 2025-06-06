﻿using ClipperLib;
using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models;
using GeometRi;
using SvgLib;
using SvgNest;
using SvgNest.Utils;
using System.Xml;

namespace Flat3DObjectsToSvgConverter.Features.CloseSlots;

public class ObjectLoopsSlotsCutter()
{
    public string CloseSlots(string svg)
    {
        SvgParser svgParser = new SvgParser();
        SvgDocument svgDocument = SvgFileHelpers.ParseSvgString(svg);

        var groupElements = svgDocument.Element.GetElementsByTagName("g").Cast<XmlElement>().ToList();
        groupElements.ForEach(element =>
        {
            var group = new SvgGroup(element);
            var pathes = element.GetElementsByTagName("path").Cast<XmlElement>().Select(p => new SvgPath(p)).ToList();
            var mainPath = pathes.FirstOrDefault(p => p.HasClass("main"));

            var idParts = mainPath.Id.Split("-");

            var pol = svgParser.Polygonify(mainPath.Element).ToList();
            pol.Add(pol.First());

            var closingSlotsSegments = GetClosingSlotsSegments(pol.ToArray(), idParts.First());
            if (closingSlotsSegments != null)
            {
                for (int i = 0; i < closingSlotsSegments.Count; i++)
                {
                    var line = closingSlotsSegments[i];

                    var id = $"{string.Join("-", idParts.Take(idParts.Length - 1))}-{pathes.Count + i}";

                    var gapLinePath = group.AddPath();
                    gapLinePath.Id = id;
                    gapLinePath.D = line.ToPathString();
                    gapLinePath.CopyStyles(mainPath);
                    gapLinePath.AddData("parentId", mainPath.Id);
                    gapLinePath.AddClass("closed-slot");
                }
            }
        });

        Console.WriteLine();

        return svgDocument.Element.OuterXml;
    }

    private List<Point3d[]> GetClosingSlotsSegments(DoublePoint[] mainLoopPoints, string meshName)
    {
        var points = mainLoopPoints.Select(p => p.ToPoint3d()).ToArray();
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
            return null;

        var rectangularClosingSlotLoops = CloseRectangularSlots(doublePoints, ortogonalSegments, segments, meshName);
        var closing2mmSlotLoops = CloseSlots(ortogonalSegments, meshName);

        return rectangularClosingSlotLoops.Concat(closing2mmSlotLoops).ToList();
    }

    private List<Point3d[]> CloseRectangularSlots(DoublePoint[] doublePoints, List<Segment3d[]> ortogonalSegments, List<Segment3d> segments, string meshName)
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
                var neighborSegments = p.Neighbors.Select(n => n.Except([segment]).First()).ToArray();
                var neighborVectors = neighborSegments.Select(n => n.ToVector).ToArray();

                var vectorsFacingOppositeDirection = neighborVectors.First().Dot(neighborVectors.Last()) < 0;

                var neighborPoints = new Point3d[] { neighborSegments[0].P1, neighborSegments[0].P2, neighborSegments[1].P1, neighborSegments[1].P2 }
                    .Where(p => p != segment.P1 && p != segment.P2)
                    .ToArray();

                var closingSegment = new Segment3d(neighborPoints[0], neighborPoints[1]).Scale(0.9);

                var a = GeometryUtil.PointInPolygon(closingSegment.P1.ToDoublePoint(), doublePoints) ?? true;
                var a1 = GeometryUtil.PointInPolygon(closingSegment.P2.ToDoublePoint(), doublePoints) ?? true;
                return segment.Length <= 10 && vectorsFacingOppositeDirection && !(a || a1);
            }).ToList();

        int slotIndex = 1;
        Segment3d prevClosedSlotSegment = null;
        List<Point3d> prevOriginalSlotPoints = null;

        List<Segment3d> segmentsForDeletion = null;

        var closingLoops = new List<Point3d[]>();

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

                Console.WriteLine($"    Closed rectangular slot {slotIndex} for mesh {meshName} main loop");

                slotIndex++;

                closingLoops.Add([closedSlotSegment.P1, closedSlotSegment.P2]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Failed to close slot {slotIndex} for mesh {meshName} main loop");
                throw;
            }
        });

        return closingLoops;
    }

    private bool IsSlotSegments(Point3d point, Segment3d segment)
    {
        if (segment.P1 == point || segment.P2 == point)
            return false;

        var a = point.DistanceTo(segment.ToLine) <= 0.01; // point lying on line with precision 0.01
        var b = point.DistanceTo(segment) < 2;
        return a && b;
    }

    private List<Point3d[]> CloseSlots(List<Segment3d[]> ortogonalSegments, string meshName)
    {
        var slots = new List<Segment3d[]>();
        for (int i = 0; i < ortogonalSegments.Count; i++)
        {
            var pair1 = ortogonalSegments[i];

            for (int j = i + 1; j < ortogonalSegments.Count; j++)
            {
                var pair2 = ortogonalSegments[j];

                var slot = pair1.Select(s1 =>
                {
                    var slotSegment = pair2.FirstOrDefault(s2 => IsSlotSegments(s2.P1, s1));
                    if (slotSegment != null)
                    {
                        var slotCandidates = new Segment3d[] { s1, slotSegment };
                        return slotCandidates;
                    }

                    return null;
                }).FirstOrDefault(s => s != null);

                if (slot != null)
                {
                    slots.Add(slot);
                    break;
                }
            }
        }

        return slots.Select((slotSegments, i) =>
        {
            Console.WriteLine($"    Closed less then 2mm slot {i + 1} for mesh {meshName} main loop");

            return new Point3d[] { slotSegments[0].P2, slotSegments[1].P1 };
        }).ToList();
    }

    private bool IsOrtogonal(double angle)
    {
        return angle > 89 && angle <= 90;
    }
}

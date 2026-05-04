using ClipperLib;
using Flat3DObjectsToSvgConverter.Helpers;
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

    private List<Segment3d> GetClosingSlotsSegments(DoublePoint[] mainLoopPoints, string meshName)
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

    // This method finds pairs of ortogonal segments that can be closed with a segment to form a rectangular slot.
    // It checks if the closing segment would lie outside the main loop and if the bottom original segment is less then 10mm.
    private List<Segment3d> CloseRectangularSlots(DoublePoint[] doublePoints, List<Segment3d[]> ortogonalSegments, List<Segment3d> segments, string meshName)
    {
        var closingLoops = ortogonalSegments.SelectMany(s => s)
            .Distinct()
            .Select(s => new
            {
                Segment = s,
                Neighbors = ortogonalSegments.Where(pair => pair.Contains(s))
            })
            .Where(ol => ol.Neighbors.Count() >= 2)
            .Select(p =>
            {
                var segment = p.Segment;
                if (segment.Length > 10)
                {
                    return null;
                }

                var neighborSegments = p.Neighbors.Select(n => n.Except([segment]).First()).ToArray();
                var neighborVectors = neighborSegments.Select(n => n.ToVector).ToArray();

                var vectorsFacingOppositeDirection = neighborVectors.First().Dot(neighborVectors.Last()) < 0;
                if (!vectorsFacingOppositeDirection){
                    return null;
                }

                var neighborPoints = new Point3d[] { neighborSegments[0].P1, neighborSegments[0].P2, neighborSegments[1].P1, neighborSegments[1].P2 }
                    .Where(p => p != segment.P1 && p != segment.P2)
                    .ToArray();

                var closingSegment = new Segment3d(neighborPoints[0], neighborPoints[1]);
                var probClosingSegment = closingSegment.Scale(0.9);

                var p1Inside = GeometryUtil.PointInPolygon(probClosingSegment.P1.ToDoublePoint(), doublePoints) ?? true;
                var p2Inside = GeometryUtil.PointInPolygon(probClosingSegment.P2.ToDoublePoint(), doublePoints) ?? true;
                return !(p1Inside || p2Inside) ? closingSegment : null;
            })
            .Where(s => s != null)
            .ToList();

        int slotIndex = 1;
        closingLoops.ForEach(s =>
        {
            Console.WriteLine($"    Closed rectangular slot {slotIndex} for mesh {meshName} main loop");
            slotIndex++;
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

    private List<Segment3d> CloseSlots(List<Segment3d[]> ortogonalSegments, string meshName)
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

            return new Segment3d(slotSegments[0].P2, slotSegments[1].P1);
        }).ToList();
    }

    private bool IsOrtogonal(double angle)
    {
        return angle > 89 && angle <= 90;
    }
}

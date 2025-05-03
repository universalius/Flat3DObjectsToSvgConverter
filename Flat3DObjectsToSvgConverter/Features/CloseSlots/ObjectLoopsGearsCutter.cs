using ClipperLib;
using Flat3DObjectsToSvgConverter.Helpers;
using GeometRi;
using SvgLib;
using SvgNest;
using SvgNest.Utils;
using System.Xml;

namespace Flat3DObjectsToSvgConverter.Features.CloseSlots;

public class ObjectLoopsGearsCutter()
{
    public string CutTeeth(string svg)
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

            var cutTeethSegments = GetCutTeethSegments(pol.ToArray(), idParts.First());
            if (cutTeethSegments != null)
            {
                for (int i = 0; i < cutTeethSegments.Length; i++)
                {
                    var line = cutTeethSegments[i];

                    var id = $"{string.Join("-", idParts.Take(idParts.Length - 1))}-{pathes.Count + i}";

                    var gapLinePath = group.AddPath();
                    gapLinePath.Id = id;
                    gapLinePath.D = line.ToPoint3ds().ToPathString();
                    gapLinePath.CopyStyles(mainPath);
                    gapLinePath.AddData("parentId", mainPath.Id);
                    gapLinePath.AddClass("cut-tooth");
                }
            }
        });

        Console.WriteLine();

        return svgDocument.Element.OuterXml;
    }

    private Segment3d[] GetCutTeethSegments(DoublePoint[] mainLoopPoints, string meshName)
    {
        var points = mainLoopPoints.Select(p => p.ToPoint3d()).ToArray();
        var bounds = GeometryUtil.GetPolygonBounds(points.Select(p => p.ToDoublePoint()).ToArray());
        var center = new Point3d(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2, 0);
        var radius = 0.0;

        if (Math.Abs(bounds.Width - bounds.Height) > 0.01)
        {
            var tolerance = 0.01;
            var point1 = points.First(p => Math.Abs(p.Y - bounds.Y) <= tolerance);
            var point2 = points.First(p => Math.Abs(p.X - bounds.X) <= tolerance);
            var point3 = points.First(p => Math.Abs(p.X - (bounds.X + bounds.Width)) <= tolerance);

            Circle3d circle;
            try
            {
                circle = new Circle3d(point1, point2, point3);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    Failed to cut teeth for mesh {meshName}, circle3d throws exception");
                return null;
            }

            center = circle.Center;
            radius = circle.R;
        }
        else
        {
            radius = bounds.Width / 2;
        }

        var radiuses = points.Select((p, i) => new { Point = p, Radius = center.DistanceTo(p), Id = i }).ToArray();
        var toothTopPoints = radiuses.Where(p => p.Radius >= radius - 0.1 && p.Radius <= radius + 0.1).OrderBy(p => p.Id).ToArray();

        if (toothTopPoints.Count() > 6)
        {
            var toothPointPairs = new List<(Point3d Point, int Id)[]>();

            for (int i = 0; i < toothTopPoints.Length; i++)
            {
                var firstPoint = toothTopPoints[i];

                if (i == toothTopPoints.Length - 1)
                {
                    var lastPair = toothPointPairs.Last();
                    if ((lastPair.Length == 1 ? lastPair[0] : lastPair[1]).Id != firstPoint.Id)
                    {
                        toothPointPairs.Add([(firstPoint.Point, firstPoint.Id)]);
                        break;
                    }
                }

                var secondPointIndex = i + 1;
                var secondPoint = secondPointIndex >= toothTopPoints.Length ? null : toothTopPoints[secondPointIndex];

                if (secondPoint.Id - firstPoint.Id == 1)
                {
                    toothPointPairs.Add([(firstPoint.Point, firstPoint.Id), (secondPoint.Point, secondPoint.Id)]);
                    i++;
                }
                else
                {
                    toothPointPairs.Add([(firstPoint.Point, firstPoint.Id)]);
                }
            }

            if (!toothPointPairs.Any(tpp =>
            {

                var pairIndex = toothPointPairs.IndexOf(tpp);
                var secondPairIndex = pairIndex + 1;

                if (pairIndex == toothPointPairs.Count - 1)
                    return false;

                return (tpp.Length == 1 ? tpp[0].Id + 1 : tpp[1].Id + 1) == toothPointPairs[secondPairIndex][0].Id; //check for circle object
            }))
            {
                var loopsSegments = toothPointPairs.Select((tpp, j) =>
                {
                    if (j == toothPointPairs.Count - 1)
                        return null;

                    var nextToothIndex = j + 1;
                    var nextToothPair = toothPointPairs[nextToothIndex];
                    return new Segment3d
                    (
                        tpp.Length == 1 ? tpp[0].Point : tpp[1].Point,
                        nextToothPair[0].Point
                    );
                }).Where(l => l != null).ToArray();

                var loopsSegmentsLengths = loopsSegments.Select(s => s.Length).ToArray();
                var minSegmentLength = loopsSegmentsLengths.Min();
                var loopsWithSameLength = loopsSegmentsLengths.Where(l => l >= minSegmentLength - 0.1 && l <= minSegmentLength + 0.1).ToArray();

                if (loopsWithSameLength.Length < 6)
                    return null;

                Console.WriteLine($"    Cut {loopsSegments.Length} teeth for mesh {meshName}");

                return loopsSegments;
            }
        }

        return null;
    }
}

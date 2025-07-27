using ClipperLib;
using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models;
using Flat3DObjectsToSvgConverter.Models.EdgeLoopParser;
using GeometRi;
using SvgLib;
using SvgNest;
using System.Xml;

namespace Flat3DObjectsToSvgConverter.Features.CleanLoops;

public class ObjectLoopsTinyGapsRemover(IOFileService file)
{
    public string ReplaceGapsWithLine(string svg)
    {
        SvgParser svgParser = new SvgParser();
        SvgDocument svgDocument = SvgFileHelpers.ParseSvgString(svg);

        var groupElements = svgDocument.Element.GetElementsByTagName("g").Cast<XmlElement>().ToList();
        groupElements.ForEach(element =>
        {
            var group = new SvgGroup(element);
            var pathes = element.GetElementsByTagName("path").Cast<XmlElement>().Select(p => new SvgPath(p)).ToList();
            var mainPath = pathes.FirstOrDefault(p => p.HasClass("main"));

            var pol = svgParser.Polygonify(mainPath.Element).ToList();
            pol.Add(pol.First());

            var (mainLoop, gapLines) = GetReplacedGapsWithLines(pol.ToArray());

            mainPath.D = mainLoop.ToPathString();

            for (int i = 0; i < gapLines.Count; i++)
            {
                var line = gapLines[i];

                var idParts = mainPath.Id.Split("-");
                var id = $"{string.Join("-", idParts.Take(idParts.Length - 1))}-{pathes.Count + i}";

                var gapLinePath = group.AddPath();
                gapLinePath.Id = id;
                gapLinePath.D = line.ToPathString();
                gapLinePath.CopyStyles(mainPath);
                gapLinePath.AddData("parentId", mainPath.Id);
            }
        });

        file.SaveSvg("compacted_kerfed_with_tiny_gaps", svgDocument.Element.OuterXml);

        return svgDocument.Element.OuterXml;
    }

    public (Point3d[] mainLoop, List<Point3d[]> gapLines) GetReplacedGapsWithLines(DoublePoint[] mainLoopPoints)
    {
        var points = mainLoopPoints.Select(p => p.ToPoint3d()).ToArray();
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

        var gapLines = new List<Point3d[]>();
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

            var shiftVector = secondSegment.ToVector.Mult(0.5);
            var gapSector = firstSegment.Translate(shiftVector);
            gapLines.Add(
            [
                gapSector.P1,
                gapSector.P2
            ]);
        });

        return (segments.Select((s, j) => s.P2).ToArray(), gapLines);
    }

    private bool IsOrtogonal(double angle)
    {
        return angle > 89 && angle <= 90 || angle >= 0 && angle <= 1;
    }
}

using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models;
using Flat3DObjectsToSvgConverter.Models.EdgeLoopParser;
using Flat3DObjectsToSvgConverter.Services.Parse3dObjects;
using GeometRi;
using Microsoft.Extensions.Options;
using SvgLib;
using SvgNest.Utils;
using System.Xml;

namespace Flat3DObjectsToSvgConverter.Services.Kerf;

public class KerfApplier(IOptions<KerfSettings> options,
    ObjectLoopsToSvgConverter objectLoopsToSvgConverter,
    IOFileService file)
{
    public void ApplyKerf(IEnumerable<MeshObjects> meshes)
    {
        var config = options.Value;

        if (!config.Enabled)
            return;

        var updatedMeshes = meshes.Select(m => new { Origin = m, Kerfed = m.Clone() }).ToArray();
        updatedMeshes.ToList().ForEach(mesh =>
        {
            mesh.Kerfed.Objects.ToList().ForEach(obj =>
            {
                var loops = obj.Loops.ToArray();
                for (var i = 0; i < loops.Length; i++)
                {
                    var loop = loops[i];
                    var kerfSegments = GetKerfedLoop(loop, i == 0);
                    loop.Points = kerfSegments.Select(ks => ks.SegmentWithKerf).ToArray().ToPoint3ds();
                }
            });
        });

        SaveToFile(updatedMeshes.Select(m => m.Origin).ToArray(), updatedMeshes.Select(m => m.Kerfed).ToArray());

        updatedMeshes.ToList().ForEach(mesh =>
        {
            mesh.Origin.Objects = mesh.Kerfed.Objects;
        });

        Console.WriteLine();
    }

    private KerfSegment[] GetKerfedLoop(LoopPoints loop, bool mainLoop)
    {
        var config = options.Value;

        var segments = loop.ToSegments();
        var points = loop.Points.ToArray();
        var doublePoints = points.Select(p => p.ToDoublePoint()).ToArray();

        var kerfSegments = segments.Select((s, i) =>
        {
            var kerfSegment = new KerfSegment(s);

            var p1 = s.P1;
            var p2 = s.P2;

            var tolerance = 0.05;
            var xSame = Math.Abs(p1.X - p2.X) <= tolerance;
            var ySame = Math.Abs(p1.Y - p2.Y) <= tolerance;
            var shift = 0.0;

            if (xSame)
            {
                shift = config.Y;
            }

            if (ySame)
            {
                shift = config.X;
            }

            if (!(xSame || ySame))
            {
                shift = config.XY;
            }

            var vector = new Vector3d(p1, p2);
            Vector3d[] orthogonalVectors = [
                new Vector3d(vector.Y,-vector.X, 0.0, vector.Coord), // clockwise 
                            new Vector3d(-vector.Y,vector.X, 0.0, vector.Coord) // counterclockwise 
            ];

            var orthogonalVector = orthogonalVectors.First(v =>
            {
                var shiftVector = v.Normalized.Mult(0.1);
                var shiftedSegment = s.Translate(shiftVector);
                var pointInPolygon = GeometryUtil.PointInPolygon(shiftedSegment.Center.ToDoublePoint(), doublePoints) ?? true;

                return mainLoop ? !pointInPolygon : pointInPolygon;
            });

            var shiftVector = orthogonalVector.Normalized.Mult(shift);
            kerfSegment.ShiftedSegment = s.Translate(shiftVector);

            return kerfSegment;
        }).ToArray();

        var length = kerfSegments.Length;
        for (var i = 0; i < length; i++)
        {
            var last = length - 1;
            var next = i + 1;

            if (i == last)
            {
                break;
            }

            var line1 = kerfSegments[i].ShiftedSegment.ToLine;
            var line2 = kerfSegments[next].ShiftedSegment.ToLine;

            var crossPoint = line1 == line2 ? // line1 and line 2 lies on same line
                    kerfSegments[i].ShiftedSegment.P2 : line1.IntersectionWith(line2) as Point3d;

            if (crossPoint == null)
            {
                throw new Exception("crossPoint for kerf was not found");
            }

            if (i == 0)
            {
                var lastLine = kerfSegments[last].ShiftedSegment.ToLine;
                var startPoint = line1 == lastLine ?
                        kerfSegments[0].ShiftedSegment.P1 : line1.IntersectionWith(lastLine) as Point3d;

                kerfSegments[0].SegmentWithKerf = new Segment3d(startPoint, crossPoint);
                kerfSegments[1].SegmentWithKerf = new Segment3d(crossPoint, new Point3d());
                kerfSegments[last].SegmentWithKerf = new Segment3d(new Point3d(), startPoint);
                continue;
            }

            kerfSegments[i].SegmentWithKerf.P2 = crossPoint;

            if (next != last)
            {
                kerfSegments[next].SegmentWithKerf = new Segment3d(crossPoint, new Point3d());
            }
            else
            {
                kerfSegments[last].SegmentWithKerf.P1 = crossPoint;
            }
        }

        return kerfSegments;
    }

    private void SaveToFile(MeshObjects[] originMeshes, MeshObjects[] kefedMeshes)
    {
        var svg = objectLoopsToSvgConverter.Convert(originMeshes, new PathStroke("red", "0.1"));
        SvgDocument svgDocumentOriginal = SvgFileHelpers.ParseSvgString(svg).Clone(true);

        svg = objectLoopsToSvgConverter.Convert(kefedMeshes, new PathStroke("green", "0.1"));
        SvgDocument svgDocumentKerfed = SvgFileHelpers.ParseSvgString(svg).Clone(true);

        var originalGroups = svgDocumentOriginal.Element.GetElementsByTagName("g").Cast<XmlElement>();
        svgDocumentKerfed.Element.GetElementsByTagName("g").Cast<XmlElement>().ToList().ForEach(e =>
        {
            var newNode = svgDocumentOriginal._document.ImportNode(e, true) as XmlElement;

            var originalGroup = originalGroups.First(e => e.GetAttribute("id") == newNode.GetAttribute("id"));

            newNode.SetAttribute("transform", originalGroup.GetAttribute("transform"));

            svgDocumentOriginal._document.DocumentElement.AppendChild(newNode);
        });

        file.SaveSvg("kerfed", svgDocumentOriginal._document.OuterXml);
    }

    public class KerfSegment
    {
        public KerfSegment(Segment3d segment)
        {
            Segment = segment;
        }

        public Segment3d Segment { get; set; }
        public Segment3d ShiftedSegment { get; set; }
        public Segment3d SegmentWithKerf { get; set; }
    }
}

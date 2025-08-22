using ClipperLib;
using Flat3DObjectsToSvgConverter.Features.Parse3dObjects;
using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models;
using GeometRi;
using Microsoft.Extensions.Options;
using SvgLib;
using SvgNest;
using SvgNest.Utils;
using System.Xml;

namespace Flat3DObjectsToSvgConverter.Features.Kerf;

public class KerfApplier(IOptions<KerfSettings> options,
    ObjectLoopsToSvgConverter objectLoopsToSvgConverter,
    IOFileService file)
{
    public string ApplyKerf(string svg)
    {
        var config = options.Value;

        if (!config.Enabled)
            return svg;

        SvgParser svgParser = new SvgParser();

        SvgDocument svgDocument = SvgFileHelpers.ParseSvgString(svg);
        var kerfedSvgDocument = CloneSvgDocumentRoot(svgDocument);
        var debugSvgDocument = CloneSvgDocumentRoot(svgDocument);

        var groupElements = svgDocument.Element.GetElementsByTagName("g").Cast<XmlElement>().ToList();

        groupElements.ForEach(element =>
        {
            var group = new SvgGroup(element);
            var pathes = group.Element.GetElementsByTagName("path").Cast<XmlElement>().ToList();
            var kerfedPathes = pathes
                .Select(pe => new
                {
                    Path = new SvgPath(pe),
                    KerfedPath = new SvgPath(pe).Clone(true)
                }).ToList();

            if (group.Transform != "translate(0 0)")
            {
                var kerfedGroup = kerfedSvgDocument.AddGroup();
                var debugGroup = debugSvgDocument.AddGroup();

                kerfedPathes.ForEach(p =>
                {
                    var kerfedPath = p.KerfedPath;
                    var path = p.Path;
                    List<XmlElement> elements = [path.Element, kerfedPath.Element];

                    elements.ForEach(e =>
                    {
                        e.SetAttribute("transform", group.Transform);
                        svgParser.ApplyTransform(e);
                    });

                    var pol = svgParser.Polygonify(kerfedPath.Element).ToList();
                    pol.Add(pol.First());

                    var kerfSegments = GetKerfedLoop(pol.ToArray(), kerfedPath.HasClass("main"));
                    var kerfedPoints = kerfSegments.Select(ks => ks.SegmentWithKerf).ToArray().ToPoint3ds();

                    kerfedPath.D = kerfedPoints.ToPathString();

                    var newNode = kerfedSvgDocument._document.ImportNode(kerfedPath.Element, true) as XmlElement;
                    kerfedGroup.Element.AppendChild(newNode);

                    path.SetStyle("stroke-width", "0.05");
                    path.SetStyle("stroke", "red");

                    kerfedPath.SetStyle("stroke-width", "0.05");
                    kerfedPath.SetStyle("stroke", "green");

                    elements.ForEach(e =>
                    {
                        newNode = debugSvgDocument._document.ImportNode(e, true) as XmlElement;
                        debugGroup.Element.AppendChild(newNode);
                    });

                    var debug = true;
                    if (debug)
                        DebugKerf(kerfSegments, debugGroup);
                });
            }
        });

        file.SaveSvg("kerfed", debugSvgDocument.Element.OuterXml);

        return kerfedSvgDocument.Element.OuterXml;
    }

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
                    var kerfSegments = GetKerfedLoop(loop.Points.Select(p => p.ToDoublePoint()).ToArray(), i == 0);
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

    private KerfSegment[] GetKerfedLoop(DoublePoint[] doublePoints, bool mainLoop)
    {
        var config = options.Value;

        var points = doublePoints.Select(p => p.ToPoint3d()).ToArray();
        var segments = points.ToSegments();

        var kerfSegments = segments.Select((s, i) =>
        {
            var kerfSegment = new KerfSegment(s);

            var p1 = s.P1;
            var p2 = s.P2;

            var vector = new Vector3d(p1, p2);

            var tolerance = 0.005;
            var xSame = Math.Abs(p1.X - p2.X) <= tolerance;
            var ySame = Math.Abs(p1.Y - p2.Y) <= tolerance;
            var shift = 0.0;
            var halfBeamWidth = config.BeamSize.Width / 2;
            var halfBeamHeight = config.BeamSize.Height / 2;
            var beamCenterY = config.BeamCenter.Y;

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

            var beamShiftY = 0.0;
            if (orthogonalVector.Y < 0)
            {
                beamShiftY = mainLoop ? halfBeamHeight + beamCenterY : halfBeamHeight + beamCenterY;
            }
            else
            {
                beamShiftY = mainLoop ? halfBeamHeight - beamCenterY : halfBeamHeight - beamCenterY;
            }

            if (xSame)
            {
                shift = halfBeamWidth;
            }

            if (ySame)
            {
                shift = beamShiftY;
            }

            if (!(xSame || ySame))
            {
                var axisXVector = new Vector3d(1.0, 0.0, 0.0);
                var alfa = vector.AngleTo(new Line3d(vector.ToPoint, axisXVector));
                var d = halfBeamWidth * Math.Tan(alfa);
                shift = (beamShiftY + d) * Math.Sin(Math.PI / 2 - alfa);
            }

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

    private void DebugKerf(KerfSegment[] kerfSegments, SvgGroup debugGroup)
    {
        var shiftedSegments = kerfSegments.Select(ks => ks.ShiftedSegment).ToArray();
        for (var i = 0; i < shiftedSegments.Length; i++)
        {
            var shiftedPath = debugGroup.AddPath();
            var shiftedPoints = new Point3d[] { shiftedSegments[i].P1, shiftedSegments[i].P2 };
            shiftedPath.D = shiftedPoints.ToPathString();
            shiftedPath.SetStyle("stroke-width", "0.05");
            shiftedPath.SetStyle("stroke", "black");
        }
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

    private SvgDocument CloneSvgDocumentRoot(SvgDocument svgDocument)
    {
        var newSvgDocument = SvgDocument.Create();
        newSvgDocument.Units = "mm";
        newSvgDocument.Width = svgDocument.Width;
        newSvgDocument.Height = svgDocument.Height;
        newSvgDocument.ViewBox = svgDocument.ViewBox;

        return newSvgDocument;
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

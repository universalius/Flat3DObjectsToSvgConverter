﻿using ClipperLib;
using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models;
using Flat3DObjectsToSvgConverter.Services.Parse3dObjects;
using GeometRi;
using Microsoft.Extensions.Options;
using SvgLib;
using SvgNest.Utils;
using System.Linq;
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
                var mainLoop = obj.Loops.First();
                var segments = mainLoop.ToSegments();
                var points = mainLoop.Points.ToArray();
                var doublePoints = points.Select(p => p.ToDoublePoint()).ToArray();

                var center = GeometryUtil.GetPolygonCentroid(
                        points.Select(p => p.ToDoublePoint()).ToArray())
                    .ToPoint3d();

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

                        //double Scale(double center, double value)
                        //{
                        //    var gain = 1.1;
                        //    return center + (value - center) * gain;
                        //}

                        //Point3d ScalePoint(Point3d center, Point3d p)
                        //{
                        //    return new Point3d(Scale(center.X, p.X), Scale(center.Y, p.Y), 0);
                        //}

                        //var gain = 1.1;
                        //var x = center.X + (s.Center.X - center.X) * gain;
                        //var y = center.Y + (s.Center.Y - center.Y) * gain;
                        //var scaleDirection = new Vector3d(s.Center, new Point3d(x, y, 0));

                        //kerfSegment.SegmentWithKerf = new Segment3d(ScalePoint(center, p1), ScalePoint(center, p2));

                        Vector3d[] orthogonalVectors = [
                            new Vector3d(vector.Y,-vector.X, 0.0, vector.Coord), // clockwise 
                            new Vector3d(-vector.Y,vector.X, 0.0, vector.Coord) // counterclockwise 
                        ];

                        var orthogonalVector = orthogonalVectors.First(v =>
                        {
                            var shiftVector = v.Normalized.Mult(0.1);
                            var shiftedSegment = s.Translate(shiftVector);
                            var pointInPolygon = GeometryUtil.PointInPolygon(shiftedSegment.Center.ToDoublePoint(), doublePoints) ?? true;

                            return !pointInPolygon;
                        });

                        var shiftVector = orthogonalVector.Normalized.Mult(shift);
                        kerfSegment.ShiftedSegment = s.Translate(shiftVector);


                        //var direction = GetDirection(s, center);

                        //Vector3d[] orthogonalVectors = [
                        //    new Vector3d(vector.Y,-vector.X, 0.0, vector.Coord), // clockwise 
                        //    new Vector3d(-vector.Y,vector.X, 0.0, vector.Coord) // counterclockwise 
                        //];

                        //var orthogonalVector = direction > 0 ? orthogonalVectors[0] : orthogonalVectors[1];

                        //var shiftVector = orthogonalVector.Normalized.Mult(shift);
                        //kerfSegment.ShiftedSegment = s.Translate(shiftVector);

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

                mainLoop.Points = kerfSegments.Select(ks => ks.SegmentWithKerf).ToArray().ToPoint3ds();
            });
        });

        SaveToFile(updatedMeshes.Select(m => m.Origin).ToArray(), updatedMeshes.Select(m => m.Kerfed).ToArray());

        Console.WriteLine();
    }

    private int GetDirection(Segment3d segment, Point3d center)
    {
        var p1 = segment.P1;
        var p2 = segment.P2;
        var vector = new Vector3d(p1, p2);
        var xVector = new Vector3d(1, 0, 0);

        var centerVector = new Vector3d(center, p1);

        var directionVector = centerVector.Cross(vector);

        var direction = directionVector.Z > 0 ? 1 : -1;

        var angle = xVector.AngleToDeg(vector);
        if ((vector.X <= 0 && vector.Y > 0) || (vector.X > 0 && vector.Y > 0))
        {
            angle = 360 - angle;
        }

        //if (angle >= 270 && 
        //    //angle == 270 &&
        //    center.X > p1.X)
        //{
        //    direction *= -1;
        //}

        //if (// angle > 0 && angle <= 90 && 
        //    angle == 90 &&
        //    center.X < p1.X)
        //{
        //    direction *= -1;
        //}

        return direction;
        //if (vector.X > 0 && vector.Y > 0)
        //    return -1;

        //if (vector.X > 0 && vector.Y < 0)
        //    return 1;

        //return 1;
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

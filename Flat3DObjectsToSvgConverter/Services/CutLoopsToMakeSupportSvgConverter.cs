using ClipperLib;
using Flat3DObjectsToSvgConverter.Common.Extensions;
using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models.ObjectsLabelsPreciseLocatorAndSvgConverter;
using GeometRi;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using ObjParserExecutor.Helpers;
using SvgLib;
using SvgNest;
using SvgNest.Models.GeometryUtil;
using SvgNest.Utils;
using System.Diagnostics;
using System.Globalization;
using System.Xml;

namespace Flat3DObjectsToSvgConverter.Services
{
    public class CutLoopsToMakeSupportSvgConverter
    {
        private CultureInfo culture = new CultureInfo("en-US", false);
        private readonly SvgParser _svgParser;
        private readonly ILogger<CutLoopsToMakeSupportSvgConverter> _logger;
        private readonly IOFileService _file;

        private readonly int _gain = 100000;

        public CutLoopsToMakeSupportSvgConverter(ILogger<CutLoopsToMakeSupportSvgConverter> logger, IOFileService file)
        {
            _file = file;
            _svgParser = new SvgParser();
            _logger = logger;
        }

        public async Task<string> Convert(string svg)
        {
            var plane = new Plane3d(new Point3d(), new Point3d(), new Point3d());

            var watch = Stopwatch.StartNew();
            Console.WriteLine("Start cut loops to make support!");
            Console.WriteLine();

            SvgDocument svgDocument = ParseSvgString(svg);
            var groupElements = svgDocument.Element.GetElementsByTagName("g").Cast<XmlElement>().ToArray();

            var loops = groupElements.Select((element, i) =>
            {
                var group = new SvgGroup(element);
                var pathes = group.Element.GetElementsByTagName("path").Cast<XmlElement>()
                .Select(pe => new SvgPath(pe));

                var path = pathes.FirstOrDefault(p => p.GetClasses().Contains("main"));
                if (path == null)
                {
                    throw new Exception("At least one path in a group should have main class");
                }

                var newPath = path.Clone();

                if (group.Transform != "translate(0 0)")
                {
                    newPath.Transform = group.Transform;
                    _svgParser.ApplyTransform(newPath.Element);
                }

                return new LoopPolygon
                {
                    LoopPath = new LoopPath
                    {
                        Path = newPath,
                        ParentGroupId = i
                    },
                };
            }).ToList();

            var cuttingPoints = GetCutingPoints(loops);

            var newSvgDocument = svgDocument.Clone(true);
            var pathElements = newSvgDocument.Element.GetElementsByTagName("path").Cast<XmlElement>().ToArray();

            cuttingPoints.Take(1).ToList().ForEach(cps =>
            {
                var pathId = cps.First().PathId;
                var path = new SvgPath(pathElements.First(e => e.GetAttribute("id") == pathId));
                var group = new SvgGroup(path.Element.ParentNode as XmlElement);

                var pathPoints = path.D.Replace("M ", "").Replace("z", "").Split("L ").ToList();
                pathPoints.Add(pathPoints.First());

                string startPoint;
                var holes = cps.OrderBy(cp => cp.LineId).Select(cp =>
                {
                    DoublePoint[] holeLine;
                    var holeLineLength = 0.5;

                    var firstPointId = cp.LineId - 1;
                    var secondPointId = cp.LineId + 1;

                    var point1 = pathPoints[firstPointId];
                    var point2 = pathPoints[secondPointId];

                    //var subPathTotalPoints = secondPointId+1;
                    var nextSubPathFirstPointAlreadyIncluded = true;

                    var line = new DoublePoint[2]
                    {
                        point1.ToDoublePoint(),
                        point2.ToDoublePoint(),
                    };

                    var lineLength = GeometryUtil.GetSegmentLineLength(line[0], line[1]);
                    var subLineLength = cp.RelativeIntersection * lineLength;

                    var angleInRadians = GeometryUtil.GetLineVectorXAngle(line[0], line[1]);
                    var holeXShift = holeLineLength * Math.Cos(angleInRadians);
                    var holeYShift = holeLineLength * Math.Sin(angleInRadians);

                    if (cp.RelativeIntersection < 0.1)
                    {
                        //subPathTotalPoints -= 2;
                        holeLine = new DoublePoint[]
                        {
                            null,
                            new DoublePoint(
                                line[0].X + holeXShift,
                                line[0].Y + holeYShift),
                        };
                    }

                    if (cp.RelativeIntersection > 0.9)
                    {
                        //subPathTotalPoints -= 1;
                        //nextSubPathFirstPointAlreadyIncluded = false;
                        holeLine = new DoublePoint[]
                        {
                            new DoublePoint(
                                line[1].X - holeXShift,
                                line[1].Y - holeYShift),
                            null,
                        };
                    }
                    else
                    {
                        //subPathTotalPoints -= 1;
                        var cutPoint = new DoublePoint(
                                line[0].X + subLineLength * Math.Cos(angleInRadians),
                                line[0].Y + subLineLength * Math.Sin(angleInRadians));

                        var halfHoleXShift = holeXShift / 2;
                        var halfHoleYShift = holeYShift / 2;

                        holeLine = new DoublePoint[]
                        {
                            new DoublePoint(
                                cutPoint.X - halfHoleXShift,
                                cutPoint.Y -  halfHoleYShift),
                            new DoublePoint(
                                cutPoint.X + halfHoleXShift,
                                cutPoint.Y + halfHoleYShift),
                        };
                    }

                    return new
                    {
                        PathId = $"{path.Id}-{cp.RayId}",
                        CutLine = new
                        {
                            FirstPointId = firstPointId,
                            SecondPointId = secondPointId
                        },
                        HoleLine = holeLine,
                        //NextSubPathFirstPointAlreadyIncluded = nextSubPathFirstPointAlreadyIncluded
                    };
                }).ToList();

                var i = 0;
                holes.ForEach(h =>
                {
                    var skip = i == 0 ? 0 : holes[i - 1].CutLine.SecondPointId;
                    var subPathPoints = pathPoints.Skip(skip).Take(h.CutLine.FirstPointId + 1).ToList();

                    if (i != 0)
                    {
                        var startPoint = holes[i - 1].HoleLine[1];
                        if (startPoint != null)
                        {
                            subPathPoints.Prepend($"{startPoint.X} {startPoint.Y}");
                        }
                    }

                    var lastPoint = h.HoleLine[0];
                    if (lastPoint != null)
                    {
                        subPathPoints.Add($"{lastPoint.X} {lastPoint.Y}");
                    }

                    var subPath = group.AddPath();
                    subPath.Id = h.PathId;
                    subPath.D = $"M {string.Join(" ", subPathPoints)}";
                    subPath.CopyStyles(path);
                    i++;
                });

                group.Element.RemoveChild(path.Element);
            });

            watch.Stop();
            Console.WriteLine($"Finished cut loops to make support! Took - {watch.ElapsedMilliseconds / 1000.0} sec");
            Console.WriteLine();

            _file.SaveSvg("cut", newSvgDocument.Element.OuterXml);

            return null;
        }

        //private void AddLabelPathToGroup(LabelSvgGroup labelPath, SvgGroup group)
        //{
        //    var letterWidth = _svgLetters.First().Width;
        //    var letterHeight = _svgLetters.First().Height;
        //    int i = 0;
        //    var label = labelPath.Label;
        //    var shiftByX = 0.0;
        //    label.ToList().ForEach(c =>
        //    {
        //        var s = c.ToString();
        //        if (s != " ")
        //        {
        //            shiftByX = i * letterWidth;

        //            var path = group.AddPath();
        //            path.D = _svgLetters.FirstOrDefault(p => p.Path.Id == s).Path.D;
        //            path.Fill = "#000000";
        //            path.Transform = $"translate({shiftByX.ToString(culture)})";
        //        }

        //        i++;
        //    });
        //    var labelWidth = shiftByX + letterWidth;
        //    group.Transform = $"translate({(labelPath.GroupLocation.X - labelWidth).ToString(culture)} {(labelPath.GroupLocation.Y - letterHeight).ToString(culture)})";
        //}

        private DoublePoint GetPolygonCenter(LoopPolygon loop)
        {
            var bounds = loop.Polygon.Bounds;
            var initialCenter = new DoublePoint(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
            if (GeometryUtil.PointInPolygon(initialCenter, loop.Polygon.Points) ?? false)
            {
                return initialCenter;
            }

            var axis = new DoublePoint[2] {
                new DoublePoint(bounds.X, initialCenter.Y).ToInt(_gain),
                new DoublePoint(bounds.X + bounds.Width, initialCenter.Y).ToInt(_gain)
            };
            var intersections = loop.Polygon.Lines
                .Select(l => GeometryUtil.LineIntersect(axis[0], axis[1], l[0], l[1]))
                .Where(intersection => intersection != null).ToArray();

            if (intersections.Count() == 1)
            {
                throw new Exception("Found only one point for center axis intercection");
            }

            var closestPoints = intersections.OrderBy(i => i.X).Take(2).Select(i => i.Scale(1.0 / _gain)).ToArray();
            var newCenter = new DoublePoint(closestPoints[0].X + (closestPoints[1].X - closestPoints[0].X) / 2, initialCenter.Y);
            return newCenter;
        }

        private List<CuttingPoint[]> GetCutingPoints(List<LoopPolygon> loops)
        {
            loops.ForEach(l =>
            {
                var points = _svgParser.Polygonify(l.LoopPath.Path.Element);
                var bounds = GeometryUtil.GetPolygonBounds(points);

                l.Polygon = new PathPolygon
                {
                    Points = points,
                    Bounds = bounds,
                    Lines = points.Select((p, i) =>
                    {
                        if (i == points.Length - 1)
                            return new DoublePoint[2] { new DoublePoint(p.X, p.Y).ToInt(_gain), new DoublePoint(points[0].X, points[0].Y).ToInt(_gain) };

                        return new DoublePoint[2] { new DoublePoint(p.X, p.Y).ToInt(_gain), new DoublePoint(points[i + 1].X, points[i + 1].Y).ToInt(_gain) };

                    }).ToArray(),
                };
                l.Polygon.Center = GetPolygonCenter(l);

                //l.LoopPath.ScaledPath = $"M {string.Join(" ", l.Polygon.Lines.Select(l => $"{l[0].X}.0 {l[0].Y}.0"))} z";
            });

            return loops.Select(l =>
            {
                var polygonCenterPoint = l.Polygon.Center;
                var radius = new double[] { l.Polygon.Bounds.Width, l.Polygon.Bounds.Height }.Max() / 2;

                var sunRays = new int[] { 0, 90, 180, 270 }.Select((angle, j) =>
                {
                    var x = polygonCenterPoint.X + radius * Math.Cos(angle * Math.PI / 180);
                    var y = polygonCenterPoint.Y + radius * Math.Sin(angle * Math.PI / 180);
                    return new
                    {
                        RayId = angle,
                        Line = new DoublePoint[] { polygonCenterPoint.ToInt(_gain), new DoublePoint(x, y).ToInt(_gain) }
                    };
                }).ToList();

                return sunRays.Select(r =>
                {
                    var rayIntersection = l.Polygon.Lines
                        .Select(l => new { Line = l, Intersection = GeometryUtil.LineIntersect(r.Line[0], r.Line[1], l[0], l[1]) })
                        .FirstOrDefault(ri => ri.Intersection != null);

                    var line = rayIntersection.Line;
                    var lineId = Array.IndexOf(l.Polygon.Lines, line);
                    var lineLength = GeometryUtil.GetSegmentLineLength(line[0], line[1]);
                    var subLineLength = GeometryUtil.GetSegmentLineLength(line[0], rayIntersection.Intersection);

                    var relativeIntersection = subLineLength / lineLength;

                    return new CuttingPoint
                    {
                        LineId = lineId,
                        RelativeIntersection = relativeIntersection,
                        PathId = l.LoopPath.Path.Id,
                        RayId = r.RayId
                    };
                }).ToArray();
            }).ToList();
        }

        public static SvgDocument ParseSvgFile(string filePath)
        {
            var content = File.ReadAllText(filePath);
            return ParseSvgString(content);
        }

        private static SvgDocument ParseSvgString(string svg)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(svg);

            return new SvgDocument(xmlDocument, xmlDocument.DocumentElement);
        }

        //private void VisualiseRays(List<LoopPolygon> loops, RayDistance[] sunRays, int raysSectorFirstRay, string name)
        //{
        //    var svgTest = SvgDocument.Create();
        //    svgTest.Width = 700;
        //    svgTest.Height = 700;
        //    svgTest.ViewBox = new SvgViewBox
        //    {
        //        Height = 700,// gain,
        //        Width = 700// gain,
        //    };

        //    loops.ForEach(e =>
        //    {
        //        var path = svgTest.AddPath();
        //        path.D = e.LoopPath.ScaledPath;
        //        path.Id = e.LoopPath.Path.Id;
        //        path.StrokeWidth = 0.3;
        //        path.Stroke = "#000000";
        //        path.Fill = "none";
        //    });

        //    sunRays.Skip(raysSectorFirstRay).Take(90)
        //    //sunRays
        //    .ToList().ForEach(r =>
        //    {
        //        var path = svgTest.AddPath();
        //        path.Id = r.RayId.ToString();
        //        path.D = $"M {r.RayLine[0].X} {r.RayLine[0].Y} {r.RayLine[1].X} {r.RayLine[1].Y}";
        //        path.StrokeWidth = 0.3;
        //        path.Stroke = "#000000";
        //        path.Fill = "none";

        //        var group = svgTest.AddGroup();


        //        AddLabelPathToGroup(new LabelSvgGroup
        //        {
        //            Label = r.RayId.ToString(),
        //            GroupLocation = r.RayLine[1]
        //        }, group);
        //    });

        //    _file.SaveSvg($"test_transform_{name}", svgTest._document.OuterXml);
        //}
    }

    public class CuttingPoint
    {
        public int LineId { get; set; }
        public string PathId { get; set; }
        public double RelativeIntersection { get; set; }
        public int RayId { get; set; }
    }
}

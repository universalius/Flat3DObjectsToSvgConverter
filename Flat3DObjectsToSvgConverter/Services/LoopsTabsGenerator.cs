using ClipperLib;
using Flat3DObjectsToSvgConverter.Common.Extensions;
using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models.ObjectsLabelsPreciseLocatorAndSvgConverter;
using GeometRi;
using Microsoft.Extensions.Logging;
using ObjParserExecutor.Helpers;
using SvgLib;
using SvgNest;
using SvgNest.Utils;
using System.Diagnostics;
using System.Xml;

namespace Flat3DObjectsToSvgConverter.Services
{
    public class LoopsTabsGenerator
    {
        private readonly SvgParser _svgParser;
        private readonly ILogger<LoopsTabsGenerator> _logger;
        private readonly IOFileService _file;

        private readonly int _gain = 100000;
        private readonly double _gapLength = 0.3;

        public LoopsTabsGenerator(ILogger<LoopsTabsGenerator> logger, IOFileService file)
        {
            _file = file;
            _svgParser = new SvgParser();
            _logger = logger;
        }

        public async Task<string> CutLoopsToMakeTabs(string svg)
        {
            var plane = new Plane3d(new Point3d(), new Point3d(), new Point3d());

            var watch = Stopwatch.StartNew();
            Console.WriteLine("Start cut loops to make tabs!");
            Console.WriteLine();

            SvgDocument svgDocument = SvgFileHelpers.ParseSvgString(svg);
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

            MakeTabs(cuttingPoints, pathElements);

            watch.Stop();
            Console.WriteLine($"Finished cut loops to make tabs! Took - {watch.ElapsedMilliseconds / 1000.0} sec");
            Console.WriteLine();

            _file.SaveSvg("with_support_tabs", newSvgDocument.Element.OuterXml);

            return null;
        }

        private void MakeTabs(List<CuttingPoint[]> cuttingPoints, XmlElement[] pathElements)
        {
            cuttingPoints.ToList().ForEach(cps =>
            {
                var pathId = cps.First().PathId;
                var path = new SvgPath(pathElements.First(e => e.GetAttribute("id") == pathId));
                var group = new SvgGroup(path.Element.ParentNode as XmlElement);

                var pathPoints = path.D.Replace("M ", "").Replace("z", "").Split("L ").ToList();
                pathPoints.Add(pathPoints.First());

                var cuts = cps.OrderBy(cp => cp.LineId).Select(cp =>
                {
                    DoublePoint[] gap;

                    var firstPointId = cp.LineId;
                    var secondPointId = cp.LineId + 1;

                    var point1 = pathPoints[firstPointId];
                    var point2 = pathPoints[secondPointId];

                    var line = new DoublePoint[2]
                    {
                        point1.ToDoublePoint(),
                        point2.ToDoublePoint(),
                    };

                    var lineLength = GeometryUtil.GetSegmentLineLength(line[0], line[1]);
                    var subLineLength = cp.RelativeIntersection * lineLength;

                    var angleInRadians = GeometryUtil.GetSegmentVectorXAngle(line[0], line[1]);
                    var gapXShift = _gapLength * Math.Cos(angleInRadians);
                    var gapYShift = _gapLength * Math.Sin(angleInRadians);

                    if (cp.RelativeIntersection < 0.1)
                    {
                        gap = new DoublePoint[]
                        {
                            null,
                            new DoublePoint(
                                line[0].X + gapXShift,
                                line[0].Y + gapYShift),
                        };
                    }

                    if (cp.RelativeIntersection > 0.9)
                    {
                        gap = new DoublePoint[]
                        {
                            new DoublePoint(
                                line[1].X - gapXShift,
                                line[1].Y - gapYShift),
                            null,
                        };
                    }
                    else
                    {
                        var cutPoint = new DoublePoint(
                                line[0].X + subLineLength * Math.Cos(angleInRadians),
                                line[0].Y + subLineLength * Math.Sin(angleInRadians));

                        var halfHoleXShift = gapXShift / 2;
                        var halfHoleYShift = gapYShift / 2;

                        gap = new DoublePoint[]
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
                        Gap = gap,
                    };
                }).ToList();

                var i = 0;
                cuts.ForEach(c =>
                {
                    List<string> subPathPoints;
                    if (i != 0)
                    {
                        var prevCut = cuts[i - 1];
                        var skip = prevCut.CutLine.SecondPointId;
                        subPathPoints = pathPoints.Skip(skip).Take(c.CutLine.FirstPointId - skip + 1).ToList();
                        var startPoint = prevCut.Gap[1];
                        if (startPoint != null)
                        {
                            subPathPoints.Insert(0, startPoint.ToSvgString());
                        }
                    }
                    else
                    {
                        var lastCut = cuts.Last();
                        var gapPoint = lastCut.Gap[1];
                        var startPoint = gapPoint != null ? gapPoint.ToSvgString() : pathPoints[lastCut.CutLine.SecondPointId];
                        var tailPoints = pathPoints.Skip(lastCut.CutLine.SecondPointId).ToList();
                        tailPoints.Insert(0, startPoint);
                        tailPoints.Pop();
                        subPathPoints = pathPoints.Take(c.CutLine.FirstPointId + 1).ToList();
                        subPathPoints.InsertRange(0, tailPoints);
                    }

                    var lastPoint = c.Gap[0];
                    if (lastPoint != null)
                    {
                        subPathPoints.Add(lastPoint.ToSvgString());
                    }

                    var subPath = group.AddPath();
                    subPath.Id = c.PathId;
                    subPath.D = $"M {string.Join(" ", subPathPoints)}";
                    subPath.CopyStyles(path);
                    i++;
                });

                group.Element.RemoveChild(path.Element);
            });
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
            var axisGain = 1.0;
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

                l.LoopPath.ScaledPath = $"M {string.Join(" ", l.Polygon.Lines.Select(l => $"{l[0].X}.0 {l[0].Y}.0"))} z";
            });

            List<Ray> allSunRays = new List<Ray>();

            var cuttingPoints = loops.Select(l =>
            {
                var sunRays = GetSunRaysBasedOnArea(l.Polygon);
                allSunRays.AddRange(sunRays);

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

            VisualiseRays(loops, allSunRays);

            return cuttingPoints;
        }

        private List<Ray> GetSunRaysBasedOnArea(PathPolygon polygon)
        {
            var polygonCenterPoint = polygon.Center;
            var radius = new double[] { polygon.Bounds.Width, polygon.Bounds.Height }.Max() / 1.9;
            var area = polygon.Bounds.Width * polygon.Bounds.Height;

            var tabsQuantityRaysMap = new Dictionary<int, int[]>
            {
                { 2, new int[]{ 0, 180 } },
                { 3, new int[]{ 0, 135, 225 }},
                { 4, new int[] { 30, 150, 210, 330 } },
                { 6, new int[] { 0, 30, 150, 180, 210, 330 } }
            };

            var tabsQuantityAreaMap = new Dictionary<int, double[]>
            {
                { 2, new double[]{ 0, 225 }},
                { 3, new double[]{ 255, 1500 }},
                { 4, new double[]{ 1500, 4255}}
            };


            var tabsCount = tabsQuantityAreaMap.Select(i => new { TabsQuantity = i.Key, AreaRange = i.Value })
                .FirstOrDefault(i => area > i.AreaRange[0] && area < i.AreaRange[1])?.TabsQuantity ?? 6;

            var raysAngles = tabsQuantityRaysMap[tabsCount];
            if (polygon.Bounds.Width > polygon.Bounds.Height)
            {
                raysAngles = raysAngles.Select(a => a + 90).ToArray();
            }

            var sunRays = raysAngles.Select((angle, j) =>
            {
                var x = polygonCenterPoint.X + radius * Math.Cos(angle * Math.PI / 180);
                var y = polygonCenterPoint.Y + radius * Math.Sin(angle * Math.PI / 180);
                return new Ray(
                    RayId: angle,
                    Line: new DoublePoint[] { polygonCenterPoint.ToInt(_gain), new DoublePoint(x, y).ToInt(_gain) }
                );
            }).ToList();

            return sunRays;
        }

        private void VisualiseRays(IEnumerable<LoopPolygon> loops, IEnumerable<Ray> sunRays)
        {
            var svgTest = SvgDocument.Create();
            svgTest.Width = 700;
            svgTest.Height = 700;
            svgTest.ViewBox = new SvgViewBox
            {
                Height = 700,
                Width = 700
            };

            var group = svgTest.AddGroup();
            group.StrokeWidth = 0.3 * _gain;
            group.Transform = $"translate(0 0) scale({1.0 / _gain})";

            loops.ToList().ForEach(e =>
            {
                var path = group.AddPath();
                path.D = e.LoopPath.ScaledPath;
                path.Id = e.LoopPath.Path.Id;
                path.Stroke = "#000000";
                path.Fill = "none";
            });

            sunRays.ToList().ForEach(r =>
            {
                var path = group.AddPath();
                path.Id = r.RayId.ToString();
                path.D = $"M {r.Line[0].X} {r.Line[0].Y} {r.Line[1].X} {r.Line[1].Y}";
                path.Stroke = "green";
                path.Fill = "none";

                //AddLabelPathToGroup(new LabelSvgGroup
                //{
                //    Label = r.RayId.ToString(),
                //    GroupLocation = r.RayLine[1]
                //}, group);
            });

            _file.SaveSvg($"rays", svgTest._document.OuterXml);
        }
    }


    public record Ray(int RayId, DoublePoint[] Line);

    public class CuttingPoint
    {
        public int LineId { get; set; }
        public string PathId { get; set; }
        public double RelativeIntersection { get; set; }
        public int RayId { get; set; }
    }
}

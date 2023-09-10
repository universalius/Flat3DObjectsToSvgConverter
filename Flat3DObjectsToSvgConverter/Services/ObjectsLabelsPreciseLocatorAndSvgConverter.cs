using ClipperLib;
using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models.EdgeLoopParser;
using GeometRi;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using ObjParser;
using ObjParserExecutor.Models;
using SvgLib;
using SvgNest;
using SvgNest.Models.GeometryUtil;
using SvgNest.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using static SvgLib.SvgDefaults.Attributes;

namespace Flat3DObjectsToSvgConverter.Services
{
    public class ObjectsLabelsPreciseLocatorAndSvgConverter
    {
        private CultureInfo culture = new CultureInfo("en-US", false);
        private readonly IEnumerable<SvgLetter> _svgLetters;
        private readonly SvgParser _svgParser;

        //private readonly double _letterWidth;
        //private readonly double _letterHeight;
        private readonly ILogger<ObjectsLabelsPreciseLocatorAndSvgConverter> _logger;
        private readonly IOFileService _file;


        private int gain = 100000;


        public ObjectsLabelsPreciseLocatorAndSvgConverter(ILogger<ObjectsLabelsPreciseLocatorAndSvgConverter> logger, IOFileService file)
        {
            //_letterHeight = GetOrHeight();
            //_letterWidth = GetUnderscoreWidth();
            _file = file;
            _svgParser = new SvgParser();
            _svgLetters = GetSvgLetters();
            _logger = logger;
        }

        public async Task<string> Convert(string svg)
        {
            var plane = new Plane3d(new Point3d(), new Point3d(), new Point3d());

            var watch = Stopwatch.StartNew();
            Console.WriteLine("Start placing labels for svg curves!");
            Console.WriteLine();

            //_logger.LogInformation("Test qwer asdf cvbbb");

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

                var label = GetLabel(path.Id);
                var newPath = path.Clone();

                if (group.Transform != "translate(0 0)")
                {
                    newPath.Transform = group.Transform;
                    _svgParser.ApplyTransform(newPath.Element);
                    //var labelGroup = group.AddGroup();
                    //labelGroup.Transform = GetLabelGroupTransform(path, group.GetTransformRotate());

                    //await AddLabelToGroup(label, labelGroup);
                }

                var (lettersGroup, labelWidth) = GetLabelLetters(label, svgDocument);
                return new LoopPolygon
                {
                    LoopPath = new LoopPath
                    {
                        Path = newPath,
                        ParentGroupId = i
                    },
                    LabelLetters = new LabelSvgGroup
                    {
                        Label = label,
                        Group = lettersGroup,
                        Width = labelWidth,
                        ParentGroupId = i
                    },
                };
            }).ToList();

            CalculateLabelCoords(loops);

            var newSvgDocument = svgDocument.Clone();
            loops.ForEach(l =>
            {
                var group = newSvgDocument.AddGroup();
                var loopsGroupElement = groupElements[l.LoopPath.ParentGroupId];

                var coords = l.LabelLetters.GroupLocation;
                l.LabelLetters.Group.Transform = $"translate({(coords.X - l.LabelLetters.Width).ToString(culture)} {(coords.Y - _svgLetters.First().Height).ToString(culture)})";

                group.Element.AppendChild(loopsGroupElement);
                group.Element.AppendChild(l.LabelLetters.Group.Element);
            });


            //var svgTest = SvgDocument.Create();

            //list.ForEach(e =>
            //{
            //    var path = svgTest.AddPath();
            //    path.D = e.GetAttribute("d");
            //    path.Id = e.GetAttribute("id");
            //});

            //_file.SaveSvg("test_transform", svgTest._document.OuterXml);

            watch.Stop();
            Console.WriteLine($"Finished placing labels for svg curves! Took - {watch.ElapsedMilliseconds / 1000.0} sec");
            Console.WriteLine();

            var labelsSvg = newSvgDocument.Element.OuterXml;
            _file.SaveSvg("labels", labelsSvg);

            return null;
        }

        private IEnumerable<SvgLetter> GetSvgLetters()
        {
            var mainFolder = AppDomain.CurrentDomain.BaseDirectory;
            SvgDocument svgDocument = ParseSvgFile(Path.Combine(mainFolder, "Asserts\\Letters.svg"));
            var pathElements = svgDocument.Element.GetElementsByTagName("path").Cast<XmlElement>().ToArray();
            var pathes = pathElements.Select(e => new SvgPath(e));

            return pathes.Select(p =>
            {
                var subPathes = _svgParser.SplitPath(p.Element);
                var points = _svgParser.Polygonify((subPathes?.Any() ?? false) ? subPathes.First() : p.Element);

                var a = string.Join(" ", points.Select(p => $"{p.X.ToString(culture)} {p.Y.ToString(culture)}"));

                var bounds = GeometryUtil.GetPolygonBounds(points);
                return new SvgLetter
                {
                    Path = p,
                    Letter = p.Id,
                    Height = bounds.Height,
                    Width = bounds.Width,
                };
            }).ToArray();
        }

        private string GetLabel(string pathId)
        {
            var idSections = pathId.Split("-")[0].Split("_");
            var firstPart = idSections[0];

            int secondNumber;
            var secondPart = string.Empty;
            if (idSections.Count() > 1)
            {
                secondPart = int.TryParse(idSections[1], out secondNumber) ?
                    secondNumber > 10 ? secondNumber.ToString() : string.Empty : string.Empty;
            }


            var label = $"{firstPart}{(string.IsNullOrEmpty(secondPart) ? string.Empty : $"_{secondPart}")}";
            return label;
        }

        private void AddLabelPathToGroup(LabelSvgGroup labelPath, SvgGroup group)
        {
            var letterWidth = _svgLetters.First().Width;
            var letterHeight = _svgLetters.First().Height;
            int i = 0;
            var label = labelPath.Label;
            var shiftByX = 0.0;
            label.ToList().ForEach(c =>
            {
                var s = c.ToString();
                if (s != " ")
                {
                    shiftByX = i * letterWidth;

                    var path = group.AddPath();
                    path.D = _svgLetters.FirstOrDefault(p => p.Path.Id == s).Path.D;
                    path.Fill = "#000000";
                    path.Transform = $"translate({shiftByX.ToString(culture)})";
                }

                i++;
            });
            var labelWidth = shiftByX + letterWidth;
            group.Transform = $"translate({(labelPath.GroupLocation.X - labelWidth).ToString(culture)} {(labelPath.GroupLocation.Y - letterHeight).ToString(culture)})";
        }

        private (SvgGroup, double) GetLabelLetters(string label, SvgDocument svgDocument)
        {
            var group = new SvgGroup(svgDocument);
            var shiftByX = 0.0;
            var i = 0;
            label.ToList().ForEach(c =>
            {
                var s = c.ToString();
                if (s != " ")
                {
                    var letter = _svgLetters.FirstOrDefault(p => p.Letter == s);
                    var path = group.AddPath();
                    path.D = letter.Path.D;
                    path.Fill = "#000000";

                    if(i != 0)
                    {
                        path.Transform = $"translate({shiftByX.ToString(culture)})";
                    }

                    shiftByX += letter.Width;
                    i++;
                }
            });

            //var lastLetter = label.Last().ToString();
            var labelWidth = shiftByX;// + _svgLetters.First(p => p.Letter == lastLetter).Width;

            return (group, labelWidth);
        }

        private DoublePoint GetPolygonCenter(LoopPolygon loop)
        {
            var bounds = loop.Polygon.Bounds;
            var initialCenter = new DoublePoint(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
            if (GeometryUtil.PointInPolygon(initialCenter, loop.Polygon.Points) ?? false)
            {
                return initialCenter;
            }

            var spaceBetweenPathes = loop.LabelLetters.Width * 1.2;
            var axis = new DoublePoint[2] {
                new DoublePoint(bounds.X - spaceBetweenPathes / 2, initialCenter.Y).ToInt(gain),
                new DoublePoint(bounds.X + bounds.Width + spaceBetweenPathes / 2, initialCenter.Y).ToInt(gain)
            };
            var intersections = loop.Polygon.Lines
                .Select(l => GeometryUtil.LineIntersect(axis[0], axis[1], l[0], l[1]))
                .Where(intersection => intersection != null).ToArray();

            if (intersections.Count() == 1)
            {
                throw new Exception("Found only one point for center axis intercection");
            }

            var closestPoints = intersections.OrderBy(i => i.X).Take(2).Select(i => i.Scale(1.0 / gain)).ToArray();
            var newCenter = new DoublePoint(closestPoints[0].X + (closestPoints[1].X - closestPoints[0].X) / 2, initialCenter.Y);
            return newCenter;
        }

        private IEnumerable<LoopPolygon> CalculateLabelCoords(List<LoopPolygon> loops)
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
                            return new DoublePoint[2] { new DoublePoint(p.X, p.Y).ToInt(gain), new DoublePoint(points[0].X, points[0].Y).ToInt(gain) };

                        return new DoublePoint[2] { new DoublePoint(p.X, p.Y).ToInt(gain), new DoublePoint(points[i + 1].X, points[i + 1].Y).ToInt(gain) };

                    }).ToArray(),
                };
                l.Polygon.Center = GetPolygonCenter(l);

                l.LoopPath.ScaledPath = $"M {string.Join(" ", l.Polygon.Lines.Select(l => $"{l[0].X}.0 {l[0].Y}.0"))} z";
            });

            loops.ForEach(l =>
            {
                var spaceBetweenLoops = l.LabelLetters.Width * 1.2;
                var extendedBounds = new PolygonBounds
                {
                    X = l.Polygon.Bounds.X - spaceBetweenLoops,
                    Y = l.Polygon.Bounds.Y - spaceBetweenLoops,
                    Width = l.Polygon.Bounds.Width + spaceBetweenLoops,
                    Height = l.Polygon.Bounds.Height + spaceBetweenLoops
                };
                l.Neighbors = loops.Where(l1 => l.LoopPath.Path.Id != l1.LoopPath.Path.Id && GeometryUtil.BoundsIntersect(extendedBounds, l1.Polygon.Bounds))
                    .ToArray();

                var polygonCenterPoint = l.Polygon.Center;

                var raysCount = 360;
                var raysAngle = 360.0 / raysCount;
                var radius = new double[] { l.Polygon.Bounds.Width, l.Polygon.Bounds.Height }.Max() / 2 + spaceBetweenLoops;

                var sunRays = new int[raysCount].Select((val, j) =>
                {
                    var angle = j * raysAngle;
                    var x = polygonCenterPoint.X + radius * Math.Cos(angle * Math.PI / 180);
                    var y = polygonCenterPoint.Y + radius * Math.Sin(angle * Math.PI / 180);
                    return new RayDistance
                    {
                        RayId = j,
                        Line = new DoublePoint[] { polygonCenterPoint.ToInt(gain), new DoublePoint(x, y).ToInt(gain) }
                    };
                }).ToArray();

                var raysSectorFirstRay = raysCount - raysCount / 2;
                var distanceBetweenPolygons = new List<RayDistance>();
                var rayIndex = 0;

                //VisualiseRays(loops, sunRays, raysSectorFirstRay, l.LoopPath.Path.Id);

                var sclaledWidthBetweenPathes = spaceBetweenLoops * gain;
                sunRays.Skip(raysSectorFirstRay).Take(90).ToList().ForEach(r =>
                {
                    var mainWithRayIntersection = l.Polygon.Lines
                        .Select(l => GeometryUtil.LineIntersect(r.Line[0], r.Line[1], l[0], l[1]))
                        .Where(intersection => intersection != null)
                        .MinBy(p => p.X);

                    var neighborsWithRayIntersections = l.Neighbors.Select(n =>
                    {
                        var intersections = n.Polygon.Lines.Select(l => GeometryUtil.LineIntersect(r.Line[0], r.Line[1], l[0], l[1]))
                            .Where(intersection => intersection != null).ToArray();
                        return new PathIntersection
                        {
                            LoopPolygon = n,
                            Intersection = intersections.Any() ? intersections.MinBy(p => p.X) : null
                        };
                    }).Where(pi => pi.Intersection != null).ToArray();

                    LoopPolygon neighborPathPolygon = null;
                    double distance = sclaledWidthBetweenPathes;
                    if (neighborsWithRayIntersections.Any())
                    {
                        var neighborIntersection = neighborsWithRayIntersections
                            .Select(nri =>
                            new
                            {
                                Distance = GeometryUtil.GetSegmentLineLength(mainWithRayIntersection, nri.Intersection),
                                Neighbor = nri.LoopPolygon
                            })
                            .MinBy(nd => nd.Distance);
                        distance = neighborIntersection.Distance;
                        neighborPathPolygon = neighborIntersection.Neighbor;
                    }

                    distanceBetweenPolygons.Add(new RayDistance
                    {
                        Line = r.Line,
                        RayId = r.RayId,
                        Main = new PathIntersection
                        {
                            LoopPolygon = l,
                            Intersection = mainWithRayIntersection
                        },
                        Neighbor = neighborPathPolygon,
                        Distance = distance
                    });
                    rayIndex++;
                });

                var distancesEnouphForLabel = distanceBetweenPolygons.Where(p => p.Distance >= sclaledWidthBetweenPathes).OrderBy(p => p.RayId).ToList();

                if (distancesEnouphForLabel.Any())
                {
                    var targetRayId = raysSectorFirstRay + 90 / 2;

                    var targetRays = distancesEnouphForLabel.Where(rd => rd.RayId >= targetRayId)
                        .OrderBy(rd => rd.RayId).ToArray();

                    DoublePoint rayIntersection;

                    if (!targetRays.Any())
                    {
                        targetRays = distancesEnouphForLabel.Where(rd => rd.RayId <= targetRayId)
                            .OrderBy(rd => rd.RayId).ToArray();
                        var targetRay = targetRays.Last();
                        rayIntersection = targetRay.Main.Intersection;
                    }
                    else
                    {
                        var targetRay = targetRays.First();
                        rayIntersection = targetRay.Main.Intersection;
                    }

                    l.LabelLetters.GroupLocation = new DoublePoint(rayIntersection.X, rayIntersection.Y).Scale(1.0 / gain);
                }

                //var groupedRaysBySequence = new List<Dictionary<int, RayDistance>>();
                //distancesEnouphForLabel.ForEach(d =>
                //{
                //    var lastGroup = groupedRaysBySequence.LastOrDefault();

                //    if (lastGroup == null)
                //    {
                //        groupedRaysBySequence.Add(new Dictionary<int, RayDistance> { { d.RayId, d } });
                //    }
                //    else
                //    {
                //        if (d.RayId - lastGroup.Last().Key == 1)
                //        {
                //            lastGroup.Add(d.RayId, d);
                //        }
                //        else
                //        {
                //            groupedRaysBySequence.Add(new Dictionary<int, RayDistance> { { d.RayId, d } });
                //        }
                //    }
                //});

                //if (groupedRaysBySequence.Any())
                //{
                //    var groupedRaysBySequenceHeights = groupedRaysBySequence.Select((g, i) =>
                //    {
                //        var rayDistances = g.Values.ToArray();
                //        var betweenRayIntersectionsHeight = rayDistances.Select((rd, j) =>
                //        {
                //            if (j == g.Count - 1)
                //                return (double?)null;

                //            var height = GeometryUtil.GetLineYVectorLength(rd.Main.Intersection, rayDistances[j + 1].Main.Intersection);
                //            return height;
                //        }).Where(h => h != null).ToArray();

                //        return new
                //        {
                //            Id = i,
                //            Group = g,
                //            TotaHeight = betweenRayIntersectionsHeight.Sum()
                //        };
                //    }).ToArray();

                //    var groupedRaysBySequenceWithNeededHeight = groupedRaysBySequenceHeights.Where(g => g.TotaHeight >= HeightBetweenPathes).ToArray();

                //    var targetRay = raysSectorFirstRay + 90 / 2;

                //    var targetRayGroups = groupedRaysBySequenceWithNeededHeight.Where(gr => gr.Group.Values.Any(rd => rd.RayId >= targetRay))
                //        .OrderBy(gr => gr.Id).ToArray();

                //    if (!targetRayGroups.Any())
                //    {
                //        targetRayGroups = groupedRaysBySequenceWithNeededHeight.Where(gr => gr.Group.Values.Any(rd => rd.RayId <= targetRay))
                //            .OrderByDescending(gr => gr.Id).ToArray();
                //        var targetRayGroup = targetRayGroups.First();
                //        var rayIntersection = targetRayGroup.Group.Values.Last().Main.Intersection;

                //        return new DoublePoint(rayIntersection.X - WidthBetweenPathes, rayIntersection.Y - HeightBetweenPathes);
                //    }

                //    if (targetRayGroups.Any())
                //    {
                //        var targetRayGroup = targetRayGroups.First();
                //        var rayIntersection = targetRayGroup.Group.Values.First().Main.Intersection;
                //        return new DoublePoint(rayIntersection.X + WidthBetweenPathes, rayIntersection.Y + HeightBetweenPathes);
                //    }
                //}
            });

            return loops;
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

        private void VisualiseRays(List<LoopPolygon> loops, RayDistance[] sunRays, int raysSectorFirstRay, string name)
        {
            var svgTest = SvgDocument.Create();
            svgTest.Width = 700;
            svgTest.Height = 700;
            svgTest.ViewBox = new SvgViewBox
            {
                Height = 700,// gain,
                Width = 700// gain,
            };

            loops.ForEach(e =>
            {
                var path = svgTest.AddPath();
                path.D = e.LoopPath.ScaledPath;
                path.Id = e.LoopPath.Path.Id;
                path.StrokeWidth = 0.3;
                path.Stroke = "#000000";
                path.Fill = "none";
            });

            sunRays.Skip(raysSectorFirstRay).Take(90)
            //sunRays
            .ToList().ForEach(r =>
            {
                var path = svgTest.AddPath();
                path.Id = r.RayId.ToString();
                path.D = $"M {r.Line[0].X} {r.Line[0].Y} {r.Line[1].X} {r.Line[1].Y}";
                path.StrokeWidth = 0.3;
                path.Stroke = "#000000";
                path.Fill = "none";

                var group = svgTest.AddGroup();


                AddLabelPathToGroup(new LabelSvgGroup
                {
                    Label = r.RayId.ToString(),
                    GroupLocation = r.Line[1]
                }, group);
            });

            _file.SaveSvg($"test_transform_{name}", svgTest._document.OuterXml);
        }
    }

    public class PathPolygon : PolygonWithBounds
    {
        public DoublePoint[][] Lines { get; set; }
        public DoublePoint Center { get; set; }
    }

    public class LoopPath
    {
        public SvgPath Path { get; set; }
        public int ParentGroupId { get; set; }
        public string ScaledPath { get; set; }
    }

    public class LoopPolygon
    {
        public LoopPath LoopPath { get; set; }
        public LabelSvgGroup LabelLetters { get; set; }
        public PathPolygon Polygon { get; set; }
        public IEnumerable<LoopPolygon> Neighbors { get; set; }
    }

    public class RayDistance
    {
        public DoublePoint[] Line { get; set; }
        public int RayId { get; set; }
        public PathIntersection Main { get; set; }
        public LoopPolygon Neighbor { get; set; }
        public double Distance { get; set; }
    }

    public class PathIntersection
    {
        public LoopPolygon LoopPolygon { get; set; }
        public DoublePoint Intersection { get; set; }
    }

    public class LabelSvgGroup
    {
        public string Label { get; set; }
        public SvgGroup Group { get; set; }
        public DoublePoint GroupLocation { get; set; }
        public int ParentGroupId { get; set; }
        public double Width { get; set; }
    }

    public class SvgLetter
    {
        public string Letter { get; set; }
        public SvgPath Path { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }
}

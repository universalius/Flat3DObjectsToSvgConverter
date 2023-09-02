using ClipperLib;
using Flat3DObjectsToSvgConverter.Helpers;
using GeometRi;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using ObjParser;
using ObjParserExecutor.Models;
using SvgLib;
using SvgNest;
using SvgNest.Models.GeometryUtil;
using SvgNest.Utils;
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
        private readonly IEnumerable<SvgPath> _svgLetters;
        private readonly SvgParser _svgParser;

        private readonly double _letterWidth;
        private readonly double _letterHeight;
        private readonly ILogger<ObjectsLabelsPreciseLocatorAndSvgConverter> _logger;
        private readonly IOFileService _file;

        private const double WidthBetweenPathes = 10;
        private const double HeightBetweenPathes = 10;

        private int gain = 100000;


        public ObjectsLabelsPreciseLocatorAndSvgConverter(ILogger<ObjectsLabelsPreciseLocatorAndSvgConverter> logger, IOFileService file)
        {
            var mainFolder = AppDomain.CurrentDomain.BaseDirectory;

            SvgDocument svgDocument = ParseSvgFile(Path.Combine(mainFolder, "Asserts\\Letters.svg"));
            var pathElements = svgDocument.Element.GetElementsByTagName("path").Cast<XmlElement>().ToArray();
            _svgLetters = pathElements.Select(e => new SvgPath(e));
            _letterHeight = GetOrHeight();
            _letterWidth = GetUnderscoreWidth();
            _file = file;
            _svgParser = new SvgParser();

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

            var mainPathPolygons = groupElements.Select((element, i) =>
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
                return new PathPolygon { Path = newPath, Label = label, GroupId = i };
            }).ToList();

            var labelPathes = GetLabelCoords(mainPathPolygons);
            var newSvgDocument = svgDocument.Clone();
            labelPathes.ToList().ForEach(lp =>
            {
                var group = newSvgDocument.AddGroup();
                var loopsGroupElement = groupElements[lp.GroupId];
                var labelGroup = group.AddGroup();
                AddLabelPathToGroup(lp, labelGroup);
                group.Element.AppendChild(loopsGroupElement);
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

        private void AddLabelPathToGroup(LabelPath labelPath, SvgGroup group)
        {
            int i = 0;
            var label = labelPath.Label;
            var shiftByX = 0.0;
            label.ToList().ForEach(c =>
            {
                var s = c.ToString();
                if (s != " ")
                {
                    shiftByX = i * _letterWidth * 0.8;

                    var path = group.AddPath();
                    path.D = _svgLetters.FirstOrDefault(p => p.Id == s).D;
                    path.Fill = "#000000";
                    path.Transform = $"translate({shiftByX.ToString(culture)})";
                }

                i++;
            });
            var labelWidth = shiftByX + _letterWidth;
            group.Transform = $"translate({(labelPath.Location.X - labelWidth).ToString(culture)} {(labelPath.Location.Y - _letterHeight).ToString(culture)})";
        }

        private Extent GetLabelCoords(SvgPath path)
        {
            var pointsString = new Regex("[mz]").Replace(path.D.ToLowerInvariant(), string.Empty).Trim().Split("l");
            var points = pointsString.Select((s, i) =>
            {
                var points = s.Trim().Split(" ");
                return new DoublePoint(double.Parse(points[0], culture), double.Parse(points[1], culture));
            }).ToList();

            return new Extent
            {
                XMin = points.Min(p => p.X),
                XMax = points.Max(p => p.X),
                YMin = points.Min(p => p.Y),
                YMax = points.Max(p => p.Y)
            };
        }

        private string GetLabelGroupTransform(SvgPath path, string parentGroupRotate)
        {
            var leftTopPoint = GetLabelCoords(path);
            var rotate = int.Parse(parentGroupRotate);
            var shift = _letterHeight * 1.2;
            if (rotate == 0)
            {
                leftTopPoint.YMin -= shift;
            }

            if (rotate == 90)
            {
                leftTopPoint.XMin -= shift;
                leftTopPoint.YMin += leftTopPoint.YSize;
            }

            if (rotate == 180)
            {
                leftTopPoint.XMin += leftTopPoint.XSize;
                leftTopPoint.YMin += leftTopPoint.YSize + shift;
            }

            if (rotate == 270)
            {
                leftTopPoint.XMin += leftTopPoint.XSize + shift;
            }

            return $"translate({leftTopPoint.XMin.ToString(culture)} {leftTopPoint.YMin.ToString(culture)}) rotate({-rotate})";
        }


        private DoublePoint GetPolygonCenter(PolygonBounds bounds, PathPolygon pathPolygon)
        {
            var bound = pathPolygon.Bounds;
            var initialCenter = new DoublePoint(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
            if (GeometryUtil.PointInPolygon(initialCenter, pathPolygon.Points) ?? false)
            {
                return initialCenter;
            }

            var axis = new DoublePoint[2] {
                new DoublePoint(bounds.X - WidthBetweenPathes / 2, initialCenter.Y).ToInt(gain),
                new DoublePoint(bounds.X + bound.Width + WidthBetweenPathes / 2, initialCenter.Y).ToInt(gain)
            };
            var intersections = pathPolygon.Lines
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

        private IEnumerable<LabelPath> GetLabelCoords(List<PathPolygon> pathPolygons)
        {
            pathPolygons.ForEach(pp =>
            {
                var points = _svgParser.Polygonify(pp.Path.Element);
                var bounds = GeometryUtil.GetPolygonBounds(points);
                pp.Points = points;
                pp.Bounds = bounds;
                pp.Lines = points.Select((p, i) =>
                {
                    if (i == points.Length - 1)
                        return new DoublePoint[2] { new DoublePoint(p.X, p.Y).ToInt(gain), new DoublePoint(points[0].X, points[0].Y).ToInt(gain) };

                    return new DoublePoint[2] { new DoublePoint(p.X, p.Y).ToInt(gain), new DoublePoint(points[i + 1].X, points[i + 1].Y).ToInt(gain) };

                }).ToArray();

                pp.PolygonCenter = GetPolygonCenter(bounds, pp);
                pp.ScaledPath = "M " +
                    string.Join(" ", pp.Lines.Select(l => $"{l[0].X}.0 {l[0].Y}.0")) +
                    " z";
            });

            var polygonNeighbors = pathPolygons.Select(first =>
            {
                var bounds = new PolygonBounds
                {
                    X = first.Bounds.X - WidthBetweenPathes,
                    Y = first.Bounds.Y - WidthBetweenPathes,
                    Width = first.Bounds.Width + WidthBetweenPathes,
                    Height = first.Bounds.Height + WidthBetweenPathes
                };
                return new
                {
                    PathPolygon = first,
                    Neighbors = pathPolygons.Where(pp => first.Path.Id != pp.Path.Id && GeometryUtil.BoundsIntersect(bounds, pp.Bounds)).ToArray(),
                };
            }).ToArray();

            var labelsCoords = polygonNeighbors.Select(pn =>
            {
                var polygonCenterPoint = pn.PathPolygon.PolygonCenter;

                var raysCount = 360;
                var raysAngle = 360.0 / raysCount;
                var radius = new double[] { pn.PathPolygon.Bounds.Width, pn.PathPolygon.Bounds.Height }.Max() / 2 + WidthBetweenPathes;

                var sunRays = new int[raysCount].Select((val, j) =>
                {
                    var angle = j * raysAngle;
                    var x = polygonCenterPoint.X + radius * Math.Cos(angle * Math.PI / 180);
                    var y = polygonCenterPoint.Y + radius * Math.Sin(angle * Math.PI / 180);
                    return new
                    {
                        Angle = j,
                        Line = new DoublePoint[] { polygonCenterPoint.ToInt(gain), new DoublePoint(x, y).ToInt(gain) }
                    };
                }).ToArray();

                var raysSectorFirstRay = raysCount - raysCount / 2;
                var distanceBetweenPolygons = new List<RayDistance>();
                var rayIndex = 0;

                var svgTest = SvgDocument.Create();
                svgTest.Width = 700;
                svgTest.Height = 700;
                svgTest.ViewBox = new SvgViewBox
                {
                    Height = 700,// gain,
                    Width = 700// gain,
                };

                pathPolygons.ForEach(e =>
                {
                    var path = svgTest.AddPath();
                    path.D = e.ScaledPath;
                    path.Id = e.Path.Id;
                    path.StrokeWidth = 0.3;
                    path.Stroke = "#000000";
                    path.Fill = "none";
                });

                sunRays.Skip(raysSectorFirstRay).Take(90)
                //sunRays
                .ToList().ForEach(r =>
                {
                    var path = svgTest.AddPath();
                    path.Id = r.Angle.ToString();
                    path.D = $"M {r.Line[0].X} {r.Line[0].Y} {r.Line[1].X} {r.Line[1].Y}";
                    path.StrokeWidth = 0.3;
                    path.Stroke = "#000000";
                    path.Fill = "none";

                    var group = svgTest.AddGroup();


                    AddLabelPathToGroup(new LabelPath
                    {
                        Label = r.Angle.ToString(),
                        Location = r.Line[1]
                    }, group);
                });


                _file.SaveSvg("test_transform", svgTest._document.OuterXml);

                var sclaledWidthBetweenPathes = WidthBetweenPathes * gain;
                sunRays.Skip(raysSectorFirstRay).Take(90).ToList().ForEach(r =>
                {
                    var mainWithRayIntersection = pn.PathPolygon.Lines
                        .Select(l => GeometryUtil.LineIntersect(r.Line[0], r.Line[1], l[0], l[1]))
                        .Where(intersection => intersection != null)
                        .MinBy(p => p.X);

                    var neighborsWithRayIntersections = pn.Neighbors.Select(n =>
                    {
                        var intersections = n.Lines.Select(l => GeometryUtil.LineIntersect(r.Line[0], r.Line[1], l[0], l[1]))
                            .Where(intersection => intersection != null).ToArray();
                        return new PathIntersection
                        {
                            PathPolygon = n,
                            Intersection = intersections.Any() ? intersections.MinBy(p => p.X) : null
                        };
                    }).Where(pi => pi.Intersection != null).ToArray();

                    PathPolygon neighborPathPolygon = null;
                    double distance = sclaledWidthBetweenPathes;
                    if (neighborsWithRayIntersections.Any())
                    {
                        if (mainWithRayIntersection == null)
                        {
                            var c = 0;
                        }

                        var neighborIntersection = neighborsWithRayIntersections
                            .Select(nri =>
                            new
                            {
                                Distance = GeometryUtil.GetSegmentLineLength(mainWithRayIntersection, nri.Intersection),
                                Neighbor = nri.PathPolygon
                            })
                            .MinBy(nd => nd.Distance);
                        distance = neighborIntersection.Distance;
                        neighborPathPolygon = neighborIntersection.Neighbor;
                    }

                    distanceBetweenPolygons.Add(new RayDistance
                    {
                        Ray = r.Line,
                        RayId = r.Angle,
                        Main = new PathIntersection
                        {
                            PathPolygon = pn.PathPolygon,
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

                    return new LabelPath
                    {
                        Label = pn.PathPolygon.Label,
                        Path = pn.PathPolygon.Path,
                        Location = new DoublePoint(rayIntersection.X, rayIntersection.Y).Scale(1.0 / gain),
                        GroupId = pn.PathPolygon.GroupId
                    };
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

                return null;
            }).ToArray();

            return labelsCoords;
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

        private double GetUnderscoreWidth()
        {
            var undescorePath = _svgLetters.First(p => p.Id == "_");
            var d = undescorePath.D.ToLower();
            var marker = "h ";
            var index = d.IndexOf(marker);
            var first = index + marker.Count();
            var last = d.IndexOf(" ", first);
            return double.Parse(d.Substring(first, last - first), culture);

        }

        private double GetOrHeight()
        {
            var orPath = _svgLetters.First(p => p.Id == "|");
            var d = orPath.D.ToLower();
            var marker = "v ";
            var index = d.LastIndexOf(marker);
            var first = index + marker.Count();
            var last = d.IndexOf(" z", first);
            return double.Parse(d.Substring(first, last - first), culture);
        }
    }

    public class PathPolygon : PolygonWithBounds
    {
        public SvgPath Path { get; set; }
        public string Label { get; set; }
        public DoublePoint[][] Lines { get; set; }
        public DoublePoint PolygonCenter { get; set; }
        public int GroupId { get; set; }
        public string ScaledPath { get; set; }
    }

    public class RayDistance
    {
        public DoublePoint[] Ray { get; set; }
        public int RayId { get; set; }
        public PathIntersection Main { get; set; }
        public PathPolygon Neighbor { get; set; }
        public double Distance { get; set; }
    }

    public class PathIntersection
    {
        public PathPolygon PathPolygon { get; set; }
        public DoublePoint Intersection { get; set; }
    }

    public class LabelPath
    {
        public string Label { get; set; }
        public SvgPath Path { get; set; }
        public DoublePoint Location { get; set; }
        public int GroupId { get; set; }
    }
}

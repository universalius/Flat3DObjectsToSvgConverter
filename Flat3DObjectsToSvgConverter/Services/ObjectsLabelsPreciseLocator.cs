using ClipperLib;
using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models.ObjectsLabelsPreciseLocatorAndSvgConverter;
using GeometRi;
using Microsoft.Extensions.Logging;
using SvgLib;
using SvgNest;
using SvgNest.Models.GeometryUtil;
using SvgNest.Utils;
using System.Diagnostics;
using System.Globalization;
using System.Xml;

namespace Flat3DObjectsToSvgConverter.Services
{
    public class ObjectsLabelsPreciseLocator
    {
        private CultureInfo culture = new CultureInfo("en-US", false);
        private readonly IEnumerable<SvgLetter> _svgLetters;
        private readonly SvgParser _svgParser;
        private readonly ILogger<ObjectsLabelsPreciseLocator> _logger;
        private readonly IOFileService _file;

        private readonly int _gain = 100000;
        private readonly double _labelShiftGain = 1;

        public ObjectsLabelsPreciseLocator(ILogger<ObjectsLabelsPreciseLocator> logger, IOFileService file)
        {
            _file = file;
            _svgParser = new SvgParser();
            _svgLetters = GetSvgLetters();
            _logger = logger;
        }

        public async Task<SvgDocument> PlaceLabels(string svg)
        {
            var plane = new Plane3d(new Point3d(), new Point3d(), new Point3d());

            var watch = Stopwatch.StartNew();
            Console.WriteLine("Start placing labels for svg curves!");
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

                var label = GetLabel(path.Id);
                var newPath = path.Clone();

                if (group.Transform != "translate(0 0)")
                {
                    newPath.Transform = group.Transform;
                    _svgParser.ApplyTransform(newPath.Element);
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
                group.Element.AppendChild(loopsGroupElement);

                var coords = l.LabelLetters.GroupLocation;
                if (coords != null)
                {
                    var xShift = coords.X - l.LabelLetters.Width * _labelShiftGain;
                    var yShift = coords.Y - _svgLetters.First().Height * _labelShiftGain;
                    l.LabelLetters.Group.Transform = $"translate({xShift.ToString(culture)} {yShift.ToString(culture)})";
                    l.LabelLetters.Group.AddClass($"labels {l.LoopPath.Path.Id}");

                    group.Element.AppendChild(l.LabelLetters.Group.Element);
                }
            });

            watch.Stop();
            Console.WriteLine($"Finished placing labels for svg curves! Took - {watch.ElapsedMilliseconds / 1000.0} sec");
            Console.WriteLine();

            var labelsSvg = newSvgDocument.Element.OuterXml;
            _file.SaveSvg("labels", labelsSvg);

            return newSvgDocument;
        }

        private IEnumerable<SvgLetter> GetSvgLetters()
        {
            var mainFolder = AppDomain.CurrentDomain.BaseDirectory;
            SvgDocument svgDocument = SvgFileHelpers.ParseSvgFile(Path.Combine(mainFolder, "Asserts\\Letters.svg"));
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
                    path.Stroke = "blue";
                    path.StrokeWidth = 0.264583;

                    if (i != 0)
                    {
                        path.Transform = $"translate({shiftByX.ToString(culture)})";
                    }

                    shiftByX += letter.Width;
                    i++;
                }
            });

            var labelWidth = shiftByX;
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
                new DoublePoint(bounds.X - spaceBetweenPathes / 2, initialCenter.Y).ToInt(_gain),
                new DoublePoint(bounds.X + bounds.Width + spaceBetweenPathes / 2, initialCenter.Y).ToInt(_gain)
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
                            return new DoublePoint[2] { new DoublePoint(p.X, p.Y).ToInt(_gain), new DoublePoint(points[0].X, points[0].Y).ToInt(_gain) };

                        return new DoublePoint[2] { new DoublePoint(p.X, p.Y).ToInt(_gain), new DoublePoint(points[i + 1].X, points[i + 1].Y).ToInt(_gain) };

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
                        RayLine = new DoublePoint[] { polygonCenterPoint.ToInt(_gain), new DoublePoint(x, y).ToInt(_gain) }
                    };
                }).ToArray();

                var raysSectorFirstRay = raysCount - raysCount / 2;
                var distanceBetweenPolygons = new List<RayDistance>();
                var rayIndex = 0;

                //VisualiseRays(loops, sunRays, raysSectorFirstRay, l.LoopPath.Path.Id);

                var sclaledWidthBetweenPathes = spaceBetweenLoops * _gain;
                sunRays.Skip(raysSectorFirstRay).Take(90).ToList().ForEach(r =>
                {
                    var mainWithRayIntersection = l.Polygon.Lines
                        .Select(l => GeometryUtil.LineIntersect(r.RayLine[0], r.RayLine[1], l[0], l[1]))
                        .Where(intersection => intersection != null)
                        .MinBy(p => p.X);

                    var neighborsWithRayIntersections = l.Neighbors.Select(n =>
                    {
                        var intersections = n.Polygon.Lines.Select(l => GeometryUtil.LineIntersect(r.RayLine[0], r.RayLine[1], l[0], l[1]))
                            .Where(intersection => intersection != null).ToArray();
                        return new PathIntersection
                        {
                            LoopPolygon = n,
                            IntersectionPoint = intersections.Any() ? intersections.MinBy(p => p.X) : null
                        };
                    }).Where(pi => pi.IntersectionPoint != null).ToArray();

                    LoopPolygon neighborPathPolygon = null;
                    double distance = sclaledWidthBetweenPathes;
                    if (neighborsWithRayIntersections.Any())
                    {
                        var neighborIntersection = neighborsWithRayIntersections
                            .Select(nri =>
                            new
                            {
                                Distance = GeometryUtil.GetSegmentLineLength(mainWithRayIntersection, nri.IntersectionPoint),
                                Neighbor = nri.LoopPolygon
                            })
                            .MinBy(nd => nd.Distance);
                        distance = neighborIntersection.Distance;
                        neighborPathPolygon = neighborIntersection.Neighbor;
                    }

                    distanceBetweenPolygons.Add(new RayDistance
                    {
                        RayLine = r.RayLine,
                        RayId = r.RayId,
                        Main = new PathIntersection
                        {
                            LoopPolygon = l,
                            IntersectionPoint = mainWithRayIntersection
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
                        rayIntersection = targetRay.Main.IntersectionPoint;
                    }
                    else
                    {
                        var targetRay = targetRays.First();
                        rayIntersection = targetRay.Main.IntersectionPoint;
                    }

                    if (rayIntersection != null)
                    {
                        l.LabelLetters.GroupLocation = new DoublePoint(rayIntersection.X, rayIntersection.Y).Scale(1.0 / _gain);
                    }
                    else
                    {
                        Console.WriteLine($"    Could not find ray intersection for lable {l.LabelLetters.Label}, label placement will be skipped");
                    }
                }
            });

            return loops;
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
                path.D = $"M {r.RayLine[0].X} {r.RayLine[0].Y} {r.RayLine[1].X} {r.RayLine[1].Y}";
                path.StrokeWidth = 0.3;
                path.Stroke = "#000000";
                path.Fill = "none";

                var group = svgTest.AddGroup();


                AddLabelPathToGroup(new LabelSvgGroup
                {
                    Label = r.RayId.ToString(),
                    GroupLocation = r.RayLine[1]
                }, group);
            });

            _file.SaveSvg($"test_transform_{name}", svgTest._document.OuterXml);
        }
    }
}

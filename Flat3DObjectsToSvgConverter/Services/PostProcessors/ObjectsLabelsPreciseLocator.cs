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
using System.Linq;
using System.Xml;

namespace Flat3DObjectsToSvgConverter.Services.PostProcessors
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
            var watch = Stopwatch.StartNew();
            Console.WriteLine("Start placing labels for svg curves!");
            Console.WriteLine();

            SvgDocument svgDocument = SvgFileHelpers.ParseSvgString(svg);
            var groupElements = svgDocument.Element.GetElementsByTagName("g").Cast<XmlElement>().ToArray();

            var loops = groupElements.Select((element, i) =>
            {
                var group = new SvgGroup(element);
                var pathes = group.Element.GetElementsByTagName("path").Cast<XmlElement>()
                .Select(pe => new SvgPath(pe).Clone());

                var mainPath = pathes.FirstOrDefault(p => p.GetClasses().Contains("main"));
                if (mainPath == null)
                {
                    throw new Exception("At least one path in a group should have main class");
                }

                var closedSlotPaths = pathes.Where(p => p.Id != mainPath.Id && !p.GetClasses().Contains("hole")).ToList();
                var label = GetLabel(mainPath.Id);

                if (group.Transform != "translate(0 0)")
                {
                    new List<SvgPath>(closedSlotPaths) { mainPath }.ForEach(p =>
                    {
                        p.Transform = group.Transform;
                        _svgParser.ApplyTransform(p.Element);
                    });
                }

                var (lettersGroup, labelWidth) = GetLabelLetters(label, svgDocument);
                var widthWithShift = labelWidth + 1;
                var heightWithShift = _svgLetters.First().Height + 1;
                return new LoopPolygon
                {
                    LoopPath = new LoopPath
                    {
                        Path = mainPath,
                        ParentGroupId = i,
                        SiblingPaths = closedSlotPaths,
                    },
                    LabelLetters = new LabelSvgGroup
                    {
                        Label = label,
                        Group = lettersGroup,
                        Height = heightWithShift,
                        ScaledHeight = heightWithShift * _gain,
                        Width = widthWithShift,
                        ScaledWidth = widthWithShift * _gain,
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
                    var yShift = coords.Y - l.LabelLetters.Height * _labelShiftGain;
                    l.LabelLetters.Group.Transform = $"translate({xShift.ToString(culture)} {yShift.ToString(culture)})";
                    l.LabelLetters.Group.AddClass("labels");
                    l.LabelLetters.Group.AddData("mainId", l.LoopPath.Path.Id);

                    group.Element.AppendChild(l.LabelLetters.Group.Element);
                }

                //var circleGroup = group.AddGroup();
                //l.Polygon.Points1.ToList().ForEach(p =>
                //{
                //    var circle = circleGroup.AddCircle();
                //    circle.CX = p.X / 100000000.0;
                //    circle.CY = p.Y / 100000000.0;
                //    circle.R = 0.5;
                //    circle.Stroke = "black";
                //    circle.StrokeWidth = 0.1;
                //});
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
                var points = _svgParser.Polygonify(subPathes?.Any() ?? false ? subPathes.First() : p.Element);

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
                        path.Transform = $"translate({shiftByX})";
                    }

                    shiftByX += letter.Width;
                    i++;
                }
            });

            var labelWidth = shiftByX;
            return (group, labelWidth);
        }

        private IEnumerable<LoopPolygon> CalculateLabelCoords(List<LoopPolygon> loops)
        {
            loops.ForEach(l =>
            {
                var pol = _svgParser.Polygonify(l.LoopPath.Path.Element);
                var points = pol.Select(p => new DoublePoint(p.X, p.Y).ToInt(_gain)).ToArray();
                var bounds = GeometryUtil.GetPolygonBounds(points);

                l.Polygon = new PathPolygon
                {
                    Points = points,
                    //Points1 = pol.Select(p => new DoublePoint(p.X, p.Y).ToInt(100000000)).ToArray(),
                    Bounds = bounds,
                    Lines = points.Select((p, i) =>
                    {
                        if (i == points.Length - 1)
                            return new DoublePoint[2] { p, points[0] };

                        return new DoublePoint[2] { p, points[i + 1] };
                    }).ToArray(),
                };
                l.Polygon.Center = GeometryUtil.GetPolygonCentroid(l.Polygon.Points);

                l.LoopPath.ScaledPath = $"M {string.Join(" ", l.Polygon.Lines.Select(l => $"{l[0].X}.0 {l[0].Y}.0"))} z";
            });

            loops.ForEach(l =>
            {
                var spaceBetweenLoops = l.LabelLetters.ScaledWidth * 1.2;
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
                        RaySegment = new DoublePoint[] { polygonCenterPoint, new DoublePoint(x, y) },
                    };
                }).ToArray();

                var raysSectorFirstRay = raysCount - raysCount / 2;
                var distanceBetweenPolygons = new List<RayDistance>();

                sunRays.Skip(raysSectorFirstRay).Take(90).ToList().ForEach(r =>
                {
                    var mainWithRayIntersection = l.Polygon.Lines
                        .Select(l => GeometryUtil.LineIntersect(r.RaySegment[0], r.RaySegment[1], l[0], l[1]))
                        .Where(intersection => intersection != null)
                        .MinBy(p => p.X);

                    if (mainWithRayIntersection == null)
                    {
                        return;
                    }

                    var neighborsWithRayIntersections = l.Neighbors.Select(n =>
                    {
                        var zeroPoint = mainWithRayIntersection;
                        var x = zeroPoint.X - l.LabelLetters.ScaledWidth * _labelShiftGain;
                        var y = zeroPoint.Y - l.LabelLetters.ScaledHeight * _labelShiftGain;
                        var labelContainerPolygon = new PointsWithOffset(new DoublePoint[] {
                                    zeroPoint,
                                    new DoublePoint(x , zeroPoint.Y),
                                    new DoublePoint(x, y),
                                    new DoublePoint(zeroPoint.X, y)
                                });
                        var labelNotFitInGap = GeometryUtil.Intersect(labelContainerPolygon, new PointsWithOffset(n.Polygon.Points));

                        return new PathIntersection
                        {
                            LoopPolygon = n,
                            HasIntersection = labelNotFitInGap
                        };
                    })
                    .Where(pi => pi.HasIntersection).ToArray();

                    distanceBetweenPolygons.Add(new RayDistance
                    {
                        RaySegment = r.RaySegment,
                        RayId = r.RayId,
                        Main = new PathIntersection
                        {
                            LoopPolygon = l,
                            IntersectionPoint = mainWithRayIntersection
                        },
                        HasIntersection = neighborsWithRayIntersections.Any()
                    });
                });

                var distancesEnouphForLabel = distanceBetweenPolygons.Where(p => !p.HasIntersection).OrderBy(p => p.RayId).ToList();

                if (distancesEnouphForLabel.Any())
                {
                    var closedSlotSegments = l.LoopPath.SiblingPaths.Select(p => _svgParser.Polygonify(p.Element))
                        .Select(points => new Segment3d(points[0].ToPoint3d(_gain), points[1].ToPoint3d(_gain)));

                    var targetRayId = raysSectorFirstRay + 90 / 2;
                    var targetRays = distancesEnouphForLabel.Where(rd => rd.RayId >= targetRayId).ToArray();

                    if (closedSlotSegments.Any())
                    {
                        targetRays = targetRays.Where(rd =>
                        {
                            var segment1 = new Segment3d(rd.RaySegment[0].ToPoint3d(), rd.RaySegment[1].ToPoint3d());
                            var isRayHitsClosedSlotSegment = closedSlotSegments.Any(s => s.IntersectionWith(segment1) != null);
                            return !isRayHitsClosedSlotSegment;
                        }).ToArray();
                    };

                    targetRays = targetRays.OrderBy(rd => rd.RayId).ToArray();

                    DoublePoint rayIntersection;
                    //VisualiseRays(loops, targetRays, l.LoopPath.Path.Id);

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

        private void VisualiseRays(List<LoopPolygon> loops, RayDistance[] sunRays, string name)
        {
            var svgTest = SvgDocument.Create();
            svgTest.Width = 700;
            svgTest.Height = 700;
            svgTest.ViewBox = new SvgViewBox
            {
                Height = 700,
                Width = 700
            };

            var scaledGroup = svgTest.AddGroup();
            scaledGroup.StrokeWidth = 0.3 * _gain;
            scaledGroup.Transform = $"translate(0 0) scale({1.0 / _gain})";

            loops.ForEach(e =>
            {
                var path = scaledGroup.AddPath();
                path.D = e.LoopPath.ScaledPath;
                path.Id = e.LoopPath.Path.Id;
                path.Stroke = "#000000";
                path.Fill = "none";
            });

            sunRays
            .ToList().ForEach(r =>
            {
                var path = scaledGroup.AddPath();
                path.Id = r.RayId.ToString();
                path.D = $"M {r.RaySegment[0].X} {r.RaySegment[0].Y} {r.RaySegment[1].X} {r.RaySegment[1].Y}";
                path.Stroke = "#000000";
                path.Fill = "none";

                var loop = loops.First(l => l.LoopPath.Path.Id == name);

                if (r.Main.IntersectionPoint != null)
                {
                    var zeroPoint = r.Main.IntersectionPoint;
                    var x = zeroPoint.X - loop.LabelLetters.ScaledWidth * _labelShiftGain;
                    var y = zeroPoint.Y - loop.LabelLetters.ScaledHeight * _labelShiftGain;
                    var labelContainerPolygon = new DoublePoint[] {
                                    zeroPoint,
                                    new DoublePoint(x , zeroPoint.Y),
                                    new DoublePoint(x, y),
                                    new DoublePoint(zeroPoint.X, y)
                                };

                    var rectPath = scaledGroup.AddPath();
                    rectPath.Id = r.RayId + "_rect";
                    rectPath.D = $"M {string.Join(" ", labelContainerPolygon.Select(p => $"{p.X} {p.Y}"))} z"; ;
                    rectPath.Stroke = "blue";
                    rectPath.Fill = "none";
                }

                var group = scaledGroup.AddGroup();
                AddLabelPathToGroup(new LabelSvgGroup
                {
                    Label = r.RayId.ToString(),
                    GroupLocation = r.RaySegment[1]
                }, group);
            });

            _file.SaveSvg($"test_transform_{name}", svgTest._document.OuterXml);
        }
    }
}

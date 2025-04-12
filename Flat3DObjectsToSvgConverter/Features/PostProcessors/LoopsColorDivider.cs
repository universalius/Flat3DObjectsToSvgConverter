using ClipperLib;
using Flat3DObjectsToSvgConverter.Features;
using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models.ObjectsLabelsPreciseLocatorAndSvgConverter;
using GeometRi;
using Microsoft.Extensions.Logging;
using SvgLib;
using SvgNest;
using SvgNest.Utils;
using System.Diagnostics;
using System.Linq;
using System.Xml;

namespace Flat3DObjectsToSvgConverter.Features.PostProcessors
{
    public class LoopsColorDivider
    {
        private readonly SvgParser _svgParser;
        private readonly ILogger<LoopsColorDivider> _logger;
        private readonly IOFileService _file;

        private readonly int _gain = 100000;

        public LoopsColorDivider(ILogger<LoopsColorDivider> logger, IOFileService file)
        {
            _file = file;
            _svgParser = new SvgParser();
            _logger = logger;
        }

        public string SetLoopsColorBasedOnLength(string svg)
        {
            var plane = new Plane3d(new Point3d(), new Point3d(), new Point3d());

            var watch = Stopwatch.StartNew();
            Console.WriteLine("Start set color to loops!");
            Console.WriteLine();

            SvgDocument svgDocument = SvgFileHelpers.ParseSvgString(svg).Clone(true);
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

                var siblingPaths = pathes.Where(p => p.Id != mainPath.Id).ToList();

                if (group.Transform != "translate(0 0)")
                {
                    new List<SvgPath>(siblingPaths) { mainPath }.ForEach(p =>
                    {
                        p.Transform = group.Transform;
                        _svgParser.ApplyTransform(p.Element);
                    });
                }

                return new LoopPolygon
                {
                    LoopPath = new LoopPath
                    {
                        Path = mainPath,
                        ParentGroupId = i,
                        SiblingPaths = siblingPaths,
                    },
                };
            }).ToList();

            var orderedLoops = GetLoopsOrderedByCenterY(loops);

            //var newSvgDocument = svgDocument.Clone(true);
            //var groupElements = newSvgDocument.Element.GetElementsByTagName("g").Cast<XmlElement>().ToArray();

            SetColors(orderedLoops, groupElements);

            watch.Stop();
            Console.WriteLine($"Finished set color to loops! Took - {watch.ElapsedMilliseconds / 1000.0} sec");
            Console.WriteLine();

            _file.SaveSvg("with_multiple_colors", svgDocument.Element.OuterXml);

            return svgDocument.Element.OuterXml;
        }

        private void SetColors((int, double)[] orderedLoops, XmlElement[] groupElements)
        {
            var lengthLimit = 1700;
            var totalLength = 0.0;
            var groupIds = new List<int>();
            List<List<int>> loopsGroupedByColor = new List<List<int>>();
            for (int i = 0; i < orderedLoops.Length; i++)
            {
                var (groupId, pathLength) = orderedLoops[i];
                totalLength += pathLength;

                if (totalLength <= lengthLimit)
                {
                    groupIds.Add(groupId);

                    if (i == orderedLoops.Length - 1)
                    {
                        loopsGroupedByColor.Add(groupIds);
                    }
                }
                else
                {
                    loopsGroupedByColor.Add(groupIds);
                    totalLength = pathLength;
                    groupIds = new List<int> { groupId };
                }
            }

            for (int i = 0; i < loopsGroupedByColor.Count; i++)
            {
                groupIds = loopsGroupedByColor[i];
                var color = _colors[i];
                groupIds.ForEach(groupId =>
                {
                    var group = groupElements[groupId];
                    var paths = group.ChildNodes.Cast<XmlElement>().Select(e => new SvgPath(e)).ToList();
                    paths.ForEach(p =>
                    {
                        p.SetStyle("stroke", color);
                    });
                });

            }
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

        private (int, double)[] GetLoopsOrderedByCenterY(List<LoopPolygon> loops)
        {
            return loops.Select(l =>
            {
                var mainPoints = _svgParser.Polygonify(l.LoopPath.Path.Element);
                var bounds = GeometryUtil.GetPolygonBounds(mainPoints);
                var mainEdges = mainPoints.ToEdges();

                l.Polygon = new PathPolygon
                {
                    Points = mainPoints,
                    Bounds = bounds,
                    Lines = mainPoints.ToEdges(_gain),
                };

                var polygonCenter = GetPolygonCenter(l);

                var siblingsPoints = l.LoopPath.SiblingPaths
                    .Select(path => _svgParser.Polygonify(path.Element))
                    .Select(points => points.ToEdges())
                    .ToArray();

                var pathsLength = new List<DoublePoint[][]>(siblingsPoints) { mainEdges }
                    .Sum(edges => edges.Sum(e => GeometryUtil.GetSegmentLength(e)));

                return new
                {
                    GroupId = l.LoopPath.ParentGroupId,
                    YCenter = polygonCenter.Y,
                    PathsLength = pathsLength
                };
            })
            .OrderBy(i => i.YCenter)
            .Select(i => (i.GroupId, i.PathsLength))
            .ToArray();
        }

        private List<Ray> GetSunRaysBasedOnArea(PathPolygon polygon)
        {
            var polygonCenterPoint = polygon.Center;
            var bounds = polygon.Bounds;
            var radius = Math.Sqrt(Math.Pow(bounds.Width, 2) + Math.Pow(bounds.Height, 2));
            //new double[] { polygon.Bounds.Width, polygon.Bounds.Height }.Max() / 1.9;
            var area = bounds.Width * bounds.Height;

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
            if (bounds.Width > bounds.Height)
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


        private string[] _colors = new string[]
        {
            "#FF0000",
            "#00E000",
            "#D0D000",
            "#FF8000",
            "#00E0E0",
            "#FF00FF",
            "#B4B4B4",
            "#0000A0",
            "#A00000",
            "#00A000",
            "#A0A000",
            "#C08000",
            "#00A0FF",
            "#A000A0",
            "#808080",
            "#7D87B9",
            "#BB7784",
            "#4A6FE3",
            "#D33F6A",
            "#8CD78C",
            "#F0B98D",
            "#F6C4E1",
            "#FA9ED4",
            "#500A78",
            "#B45A00",
            "#004754",
            "#86FA88",
            "#FFDB66",
            "#F36926",
            "#0C96D9"
        };
    }
}

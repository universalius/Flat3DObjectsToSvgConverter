using SvgLib;
using System.Xml;
using SvgNest.Utils;
using ClipperLib;
using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using DPath = System.Collections.Generic.List<ClipperLib.DoublePoint>;
using Plain3DObjectsToSvgConverter.Common.Extensions;
using Newtonsoft.Json;
using SvgNest.Helpers;
using SvgNest.Constants;
using System.Globalization;
using SvgNest.Models;
using SvgNest.Models.GeometryUtil;

namespace SvgNest
{
    public class SvgNest
    {
        private SvgDocument _svg = null;

        // keep a reference to any style nodes, to maintain color/fill info
        private string style = null;

        private List<XmlElement> _parts = null;

        private List<Node> _tree = null;

        private XmlElement _bin = null;
        private PolygonBounds _binBounds = null;
        private Dictionary<string, List<DPath>> _nfpCache = null;

        private CultureInfo culture = new CultureInfo("en-US", false);

        private SvgNestConfig _config;

        private SvgParser _svgParser;

        public SvgNest(SvgNestConfig config = null)
        {
            _svgParser = new SvgParser();
            _config = config ?? new SvgNestConfig();
            _nfpCache = new Dictionary<string, List<DPath>>();
        }

        public List<XmlElement> CompactedSvgs { get; set; }

        public SvgDocument ParseSvg(string svgstring)
        {
            // reset if in progress
            //stop();

            _bin = null;
            //binPolygon = null;
            _tree = null;

            // parse svg
            _svg = _svgParser.Load(svgstring);

            //style = svgParser.getStyle();

            _svg = _svgParser.CleanInput();

            _tree = GetParts(_svg.Element.ChildNodes.Cast<XmlElement>().ToArray());

            return _svg;
        }

        public async Task Start()
        {
            // Copy relevant scaling info
            _bin = _svg._document.CreateElement("rect");
            _bin.SetAttribute("x", "0");
            _bin.SetAttribute("y", "0");
            _bin.SetAttribute("width", ((int)_svg.Width).ToString());
            _bin.SetAttribute("height", ((int)_svg.Height).ToString());
            _bin.SetAttribute("class", "fullRect");

            if (_svg == null)
            {
                throw new Exception("Svg or bin is null");
            }

            var children = _svg.Element.ChildNodes.Cast<XmlElement>().ToArray();

            var childrenCopy = new XmlElement[children.Length];
            Array.Copy(children, childrenCopy, children.Length);
            _parts = childrenCopy.ToList();

            //var binindex = bin == null ? parts.IndexOf(bin) : 0;

            //if (binindex >= 0)
            //{
            //    // don"t process bin as a part of the tree
            //    parts.RemoveRange(binindex, 1);
            //}

            // build tree without bin
            //tree = getParts(parts.ToArray());

            OffsetTree(_tree, 0.5 * _config.Spacing);

            PolygonWithBounds binPolygon = new PolygonWithBounds { Points = _svgParser.Polygonify(_bin) };
            binPolygon.Points = CleanPolygon(binPolygon.Points);

            if (binPolygon == null || binPolygon.Points.Length < 3)
            {
                throw new Exception("binPolygon is null or has less then 3 points");
            }

            _binBounds = GeometryUtil.GetPolygonBounds(binPolygon.Points);

            if (_config.Spacing > 0)
            {
                var offsetBin = PolygonOffset(binPolygon.Points, -0.5 * _config.Spacing);
                if (offsetBin.Count == 1)
                {
                    // if the offset contains 0 or more than 1 path, something went wrong.
                    binPolygon.Points = offsetBin.Pop();
                }
            }

            binPolygon.Id = -1;

            // put bin on origin
            var binPoints = binPolygon.Points;

            var binX = binPoints.Select(p => p.X).ToArray();
            var binY = binPoints.Select(p => p.Y).ToArray();
            var xbinmax = binX.Max();
            var xbinmin = binX.Min();
            var ybinmax = binY.Max();
            var ybinmin = binY.Min();

            //var xbinmax = binPoints[0].X;
            //var xbinmin = binPoints[0].X;
            //var ybinmax = binPoints[0].Y;
            //var ybinmin = binPoints[0].Y;

            //for (var i = 1; i < binPoints.Length; i++)
            //{
            //    if (binPoints[i].X > xbinmax)
            //    {
            //        xbinmax = binPoints[i].X;
            //    }
            //    else if (binPoints[i].X < xbinmin)
            //    {
            //        xbinmin = binPoints[i].X;
            //    }
            //    if (binPoints[i].Y > ybinmax)
            //    {
            //        ybinmax = binPoints[i].Y;
            //    }
            //    else if (binPoints[i].Y < ybinmin)
            //    {
            //        ybinmin = binPoints[i].Y;
            //    }
            //}

            for (var i = 0; i < binPoints.Length; i++)
            {
                binPoints[i].X -= xbinmin;
                binPoints[i].Y -= ybinmin;
            }

            if (binPolygon.Bounds == null)
            {
                binPolygon.Bounds = new PolygonBounds();
            }

            binPolygon.Bounds.Width = xbinmax - xbinmin;
            binPolygon.Bounds.Height = ybinmax - ybinmin;

            // all paths need to have the same winding direction
            if (GeometryUtil.PolygonArea(binPoints) > 0)
            {
                binPolygon.Points.Reverse();
            }

            // remove duplicate endpoints, ensure counterclockwise winding direction
            _tree.ForEach(node =>
            {
                var start = node.Points.First();
                var end = node.Points.Last();
                if (start == end || (GeometryUtil.AlmostEqual(start.X, end.X) && GeometryUtil.AlmostEqual(start.Y, end.Y)))
                {
                    node.Points.Pop();
                }

                if (GeometryUtil.PolygonArea(node.Points.ToArray()) > 0)
                {
                    node.Points.Reverse();
                }
            });

            await LaunchWorkers(_tree, binPolygon);

            //var self = this;
            //working = false;

            //workerTimer = setInterval(function(){
            //    if (!self.working)
            //    {
            //        self.launchWorkers.call(self, tree, binPolygon, config, progressCallback, displayCallback);
            //        self.working = true;
            //    }

            //    progressCallback(progress);
            //}, 100);
        }

        // offset tree recursively
        private void OffsetTree(List<Node> tree, double offset)
        {
            tree.ForEach(node =>
        {
            var offsetpaths = PolygonOffset(node.Points.ToArray(), offset);
            if (offsetpaths.Count == 1)
            {
                // replace array items in place
                node.Points = offsetpaths[0].ToList();
            }

            if (node.Children != null && node.Children.Any())
            {
                OffsetTree(node.Children, -offset);
            }
        });
        }

        private async Task<List<XmlElement>> LaunchWorkers(List<Node> tree, PolygonWithBounds binPolygon/*, progressCallback, displayCallback*/)
        {
            PlacementsFitness best = null;

            // initiate new GA
            var adam = new List<Node>(tree);

            // seed with decreasing area
            adam = adam.OrderByDescending((n) => (int)Math.Abs(GeometryUtil.PolygonArea(n.Points.ToArray()))).ToList();

            var GA = new GeneticAlgorithm(adam, binPolygon.Points, _config);

            Individual individual = null;

            // evaluate all members of the population
            for (var i = 0; i < GA.Population.Count; i++)
            {
                if (GA.Population[i].Fitness == 0)
                {
                    individual = GA.Population[i];
                    break;
                }
            }

            if (individual == null)
            {
                // all individuals have been evaluated, start next generation
                GA.Generation();
                individual = GA.Population[1];
            }

            var placelist = individual.Placement;
            var rotations = individual.Rotation;

            var ids = new List<int>();
            for (var i = 0; i < placelist.Count; i++)
            {
                ids.Add(placelist[i].Id);
                placelist[i].Rotation = rotations[i];
            }

            var nfpPairs = new List<NodesPair>();
            SvgNestPair key;
            string keyJson;
            var newCache = new Dictionary<string, List<DPath>>();

            for (var i = 0; i < placelist.Count; i++)
            {
                var part = placelist[i];
                key = new SvgNestPair { A = binPolygon.Id, B = part.Id, Inside = true, ARotation = 0, BRotation = rotations[i] };
                keyJson = JsonConvert.SerializeObject(key);

                if (!_nfpCache.ContainsKey(keyJson))
                {
                    nfpPairs.Add(new NodesPair
                    {
                        A = new Node { Id = binPolygon.Id, Points = binPolygon.Points.ToList() },
                        B = part,
                        Key = key
                    });
                }
                else
                {
                    newCache[keyJson] = _nfpCache[keyJson];
                }

                for (var j = 0; j < i; j++)
                {
                    var placed = placelist[j];
                    key = new SvgNestPair
                    {
                        A = placed.Id,
                        B = part.Id,
                        Inside = false,
                        ARotation = rotations[j],
                        BRotation = rotations[i]
                    };

                    keyJson = JsonConvert.SerializeObject(key);
                    if (!_nfpCache.ContainsKey(keyJson))
                    {
                        nfpPairs.Add(new NodesPair { A = placed, B = part, Key = key });
                    }
                    else
                    {
                        newCache[keyJson] = _nfpCache[keyJson];

                    }
                }
            }

            // only keep cache for one cycle
            _nfpCache = newCache;

            var worker = new PlacementWorker(binPolygon, new List<Node>(placelist), ids, rotations, _config, _nfpCache);

            ////var b = "";
            //var b = "{\r\n    \"A\": [\r\n        {\r\n            \"x\": 300,\r\n            \"y\": 400\r\n        },\r\n        {\r\n            \"x\": 0,\r\n            \"y\": 400\r\n        },\r\n        {\r\n            \"x\": 0,\r\n            \"y\": 0\r\n        },\r\n        {\r\n            \"x\": 300,\r\n            \"y\": 0\r\n        }\r\n    ],\r\n    \"B\": [\r\n        {\r\n            \"x\": 459.3695005,\r\n            \"y\": -34.2\r\n        },\r\n        {\r\n            \"x\": 459.1624005,\r\n            \"y\": -24.387\r\n        },\r\n        {\r\n            \"x\": 460.7934005,\r\n            \"y\": -24.711\r\n        },\r\n        {\r\n            \"x\": 460.3695005,\r\n            \"y\": -25.876\r\n        },\r\n        {\r\n            \"x\": 460.3695005,\r\n            \"y\": -34.2\r\n        },\r\n        {\r\n            \"x\": 462.8695005,\r\n            \"y\": -34.2\r\n        },\r\n        {\r\n            \"x\": 463.5094005,\r\n            \"y\": -31.1\r\n        },\r\n        {\r\n            \"x\": 462.8695005,\r\n            \"y\": -31.1\r\n        },\r\n        {\r\n            \"x\": 462.8695005,\r\n            \"y\": -27.1\r\n        },\r\n        {\r\n            \"x\": 486.3195005,\r\n            \"y\": -27.1\r\n        },\r\n        {\r\n            \"x\": 486.3195005,\r\n            \"y\": -31.1\r\n        },\r\n        {\r\n            \"x\": 485.6796005,\r\n            \"y\": -31.1\r\n        },\r\n        {\r\n            \"x\": 486.3195005,\r\n            \"y\": -34.2\r\n        },\r\n        {\r\n            \"x\": 488.8195005,\r\n            \"y\": -34.2\r\n        },\r\n        {\r\n            \"x\": 488.6124005,\r\n            \"y\": -24.387\r\n        },\r\n        {\r\n            \"x\": 490.2434005,\r\n            \"y\": -24.711\r\n        },\r\n        {\r\n            \"x\": 489.8195005,\r\n            \"y\": -25.876\r\n        },\r\n        {\r\n            \"x\": 489.8195005,\r\n            \"y\": -34.2\r\n        },\r\n        {\r\n            \"x\": 492.3195005,\r\n            \"y\": -34.2\r\n        },\r\n        {\r\n            \"x\": 492.9594005,\r\n            \"y\": -31.1\r\n        },\r\n        {\r\n            \"x\": 492.3195005,\r\n            \"y\": -31.1\r\n        },\r\n        {\r\n            \"x\": 492.3195005,\r\n            \"y\": -27.1\r\n        },\r\n        {\r\n            \"x\": 495.3795005,\r\n            \"y\": -27.1\r\n        },\r\n        {\r\n            \"x\": 495.3795005,\r\n            \"y\": -23.102\r\n        },\r\n        {\r\n            \"x\": 493.1295005,\r\n            \"y\": -20.1\r\n        },\r\n        {\r\n            \"x\": 492.5945005,\r\n            \"y\": -17.1\r\n        },\r\n        {\r\n            \"x\": 493.1295005,\r\n            \"y\": -14.1\r\n        },\r\n        {\r\n            \"x\": 495.3795005,\r\n            \"y\": -11.099\r\n        },\r\n        {\r\n            \"x\": 495.3795005,\r\n            \"y\": -7.1\r\n        },\r\n        {\r\n            \"x\": 492.3195005,\r\n            \"y\": -7.1\r\n        },\r\n        {\r\n            \"x\": 492.3195005,\r\n            \"y\": -3.1\r\n        },\r\n        {\r\n            \"x\": 492.9594005,\r\n            \"y\": -3.1\r\n        },\r\n        {\r\n            \"x\": 492.3195005,\r\n            \"y\": 0\r\n        },\r\n        {\r\n            \"x\": 489.8195005,\r\n            \"y\": 0\r\n        },\r\n        {\r\n            \"x\": 490.0266005,\r\n            \"y\": -9.813\r\n        },\r\n        {\r\n            \"x\": 488.3956005,\r\n            \"y\": -9.489\r\n        },\r\n        {\r\n            \"x\": 488.8195005,\r\n            \"y\": -8.324\r\n        },\r\n        {\r\n            \"x\": 488.8195005,\r\n            \"y\": 0\r\n        },\r\n        {\r\n            \"x\": 486.3195005,\r\n            \"y\": 0\r\n        },\r\n        {\r\n            \"x\": 485.6796005,\r\n            \"y\": -3.1\r\n        },\r\n        {\r\n            \"x\": 486.3195005,\r\n            \"y\": -3.1\r\n        },\r\n        {\r\n            \"x\": 486.3195005,\r\n            \"y\": -7.1\r\n        },\r\n        {\r\n            \"x\": 462.8695005,\r\n            \"y\": -7.1\r\n        },\r\n        {\r\n            \"x\": 462.8695005,\r\n            \"y\": -3.1\r\n        },\r\n        {\r\n            \"x\": 463.5094005,\r\n            \"y\": -3.1\r\n        },\r\n        {\r\n            \"x\": 462.8695005,\r\n            \"y\": 0\r\n        },\r\n        {\r\n            \"x\": 460.3695005,\r\n            \"y\": 0\r\n        },\r\n        {\r\n            \"x\": 460.5766005,\r\n            \"y\": -9.813\r\n        },\r\n        {\r\n            \"x\": 458.9456005,\r\n            \"y\": -9.489\r\n        },\r\n        {\r\n            \"x\": 459.3695005,\r\n            \"y\": -8.324\r\n        },\r\n        {\r\n            \"x\": 459.3695005,\r\n            \"y\": 0\r\n        },\r\n        {\r\n            \"x\": 456.8695005,\r\n            \"y\": 0\r\n        },\r\n        {\r\n            \"x\": 456.2296005,\r\n            \"y\": -3.1\r\n        },\r\n        {\r\n            \"x\": 456.8695005,\r\n            \"y\": -3.1\r\n        },\r\n        {\r\n            \"x\": 456.8695005,\r\n            \"y\": -7.1\r\n        },\r\n        {\r\n            \"x\": 455.0195005,\r\n            \"y\": -7.1\r\n        },\r\n        {\r\n            \"x\": 455.0195005,\r\n            \"y\": -10.1\r\n        },\r\n        {\r\n            \"x\": 451.0195005,\r\n            \"y\": -10.1\r\n        },\r\n        {\r\n            \"x\": 451.0195005,\r\n            \"y\": -7.1\r\n        },\r\n        {\r\n            \"x\": 448.4695005,\r\n            \"y\": -7.1\r\n        },\r\n        {\r\n            \"x\": 448.4695005,\r\n            \"y\": -3.6\r\n        },\r\n        {\r\n            \"x\": 446.3816005,\r\n            \"y\": -3.6\r\n        },\r\n        {\r\n            \"x\": 444.6195005,\r\n            \"y\": -7.1\r\n        },\r\n        {\r\n            \"x\": 443.8195005,\r\n            \"y\": -17.1\r\n        },\r\n        {\r\n            \"x\": 444.6195005,\r\n            \"y\": -27.1\r\n        },\r\n        {\r\n            \"x\": 446.3816005,\r\n            \"y\": -30.6\r\n        },\r\n        {\r\n            \"x\": 448.4695005,\r\n            \"y\": -30.6\r\n        },\r\n        {\r\n            \"x\": 448.4695005,\r\n            \"y\": -27.1\r\n        },\r\n        {\r\n            \"x\": 451.0195005,\r\n            \"y\": -27.1\r\n        },\r\n        {\r\n            \"x\": 451.0195005,\r\n            \"y\": -24.1\r\n        },\r\n        {\r\n            \"x\": 455.0195005,\r\n            \"y\": -24.1\r\n        },\r\n        {\r\n            \"x\": 455.0195005,\r\n            \"y\": -27.1\r\n        },\r\n        {\r\n            \"x\": 456.8695005,\r\n            \"y\": -27.1\r\n        },\r\n        {\r\n            \"x\": 456.8695005,\r\n            \"y\": -31.1\r\n        },\r\n        {\r\n            \"x\": 456.2296005,\r\n            \"y\": -31.1\r\n        },\r\n        {\r\n            \"x\": 456.8695005,\r\n            \"y\": -34.2\r\n        }\r\n    ],\r\n    \"key\": {\r\n        \"A\": -1,\r\n        \"B\": 11,\r\n        \"inside\": true,\r\n        \"Arotation\": 0,\r\n        \"Brotation\": 180\r\n    }\r\n}";
            ////var b = "{\"A\":null,\"B\":null,\"key\":null}";

            //var test = b.Replace("[", "{\"points\":[").Replace("]", "]}");   //b.Replace("\"x\"", "\"X\"").Replace("\"y\"", "\"Y\"").Replace("\r\n", "");

            //var input = JsonConvert.DeserializeObject<NodesPair>(test);

            //await GetNodesPairsPathes(input);
            //return null;

            var generatedNfp = await Task.WhenAll(nfpPairs.Select(GetNodesPairsPathes));

            if (generatedNfp != null && generatedNfp.Any())
            {
                for (var i = 0; i < generatedNfp.Length; i++)
                {
                    var Nfp = generatedNfp[i];

                    if (Nfp != null)
                    {
                        // a null nfp means the nfp could not be generated, either because the parts simply don"t fit or an error in the nfp algo
                        var cacheKey = JsonConvert.SerializeObject(Nfp.Value.Key);
                        _nfpCache[cacheKey] = Nfp.Value.Value;
                    }
                }
            }
            worker.NfpCache = _nfpCache;

            var placements = await Task.WhenAll(new List<List<Node>> { new List<Node>(placelist) }.Select(worker.PlacePaths));

            if (placements == null || !placements.Any())
            {
                return null;
            }

            individual.Fitness = placements[0].Fitness;
            var bestresult = placements[0];

            for (var i = 1; i < placements.Length; i++)
            {
                if (placements[i].Fitness < bestresult.Fitness)
                {
                    bestresult = placements[i];
                }
            }

            if (best == null || bestresult.Fitness < best.Fitness)
            {
                best = bestresult;

                double placedArea = 0;
                double totalArea = 0;
                var numParts = placelist.Count;
                var numPlacedParts = 0;

                for (var i = 0; i < best.Placements.Count; i++)
                {
                    totalArea += Math.Abs(GeometryUtil.PolygonArea(binPolygon.Points));
                    for (var j = 0; j < best.Placements[i].Count; j++)
                    {
                        placedArea += Math.Abs(GeometryUtil.PolygonArea(tree[best.Placements[i][j].Id].Points.ToArray()));
                        numPlacedParts++;
                    }
                }

                var result = ApplyPlacement(best.Placements);

                var compactedSvg = SvgDocument.Create();

                //result.Where(e=>e.Name != "rect").ForEach()

                return result;
                //displayCallback(self.applyPlacement(best.placements), placedArea / totalArea, numPlacedParts, numParts);
            }
            else
            {
                return null;
            }
        }

        // assuming no intersections, return a tree where odd leaves are parts and even ones are holes
        // might be easier to use the DOM, but paths can"t have paths as children. So we"ll just make our own tree.
        private List<Node> GetParts(XmlElement[] paths)
        {
            int i, j;
            var nodes = new List<Node>();

            var numChildren = paths.Count();
            for (i = 0; i < numChildren; i++)
            {
                var poly = _svgParser.Polygonify(paths[i]);
                poly = CleanPolygon(poly)?.ToArray();

                // todo: warn user if poly could not be processed and is excluded from the nest
                if (poly != null && poly.Length > 2 && Math.Abs(GeometryUtil.PolygonArea(poly)) > _config.CurveTolerance * _config.CurveTolerance)
                {
                    nodes.Add(new Node { Points = poly.ToList(), Source = i });
                }
            }

            // turn the list into a tree
            ToTree(nodes);

            return nodes;
        }

        private int ToTree(List<Node> list, int id = 0)
        {
            var parents = new List<Node>();
            for (var i = 0; i < list.Count; i++)
            {
                var p = list[i];

                var ischild = false;
                for (var j = 0; j < list.Count; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    var pointInPolygon = GeometryUtil.PointInPolygon(p.Points[0], list[j].Points.ToArray());
                    if (pointInPolygon.HasValue && pointInPolygon.Value)
                    {
                        if (list[j].Children == null)
                        {
                            list[j].Children = new List<Node>();
                        }
                        list[j].Children.Add(p);
                        p.Parent = list[j];
                        ischild = true;
                        break;
                    }
                }

                if (!ischild)
                {
                    parents.Add(p);
                }
            }

            for (var i = 0; i < list.Count; i++)
            {
                if (parents.IndexOf(list[i]) < 0)
                {
                    list.RemoveRange(i, 1);
                    i--;
                }
            }

            for (var i = 0; i < parents.Count; i++)
            {
                parents[i].Id = id;
                id++;
            }

            for (var i = 0; i < parents.Count; i++)
            {
                if (parents[i].Children != null)
                {
                    id = ToTree(parents[i].Children, id);
                }
            }

            return id;
        }

        // use the clipper library to return an offset to the given polygon. Positive offset expands the polygon, negative contracts
        // note that this returns an array of polygons
        private List<DoublePoint[]> PolygonOffset(DoublePoint[] polygon, double offset)
        {
            var result = new List<DoublePoint[]>();
            if (offset == 0 || GeometryUtil.AlmostEqual(offset, 0))
            {
                result.Add(polygon);
                return result;
            }

            var p = ClipperHelper.ToClipperCoordinates(polygon);

            var miterLimit = 2;
            var co = new ClipperOffset(miterLimit, _config.CurveTolerance * SvgNestConstants.ClipperScale);
            co.AddPath(p, JoinType.jtRound, EndType.etClosedPolygon);

            var newpaths = new List<Path>();
            co.Execute(ref newpaths, offset * SvgNestConstants.ClipperScale);

            result = newpaths.Select(ClipperHelper.ToSvgNestCoordinates).ToList();
            return result;
        }

        // returns a less complex polygon that satisfies the curve tolerance
        private DoublePoint[] CleanPolygon(DoublePoint[] polygon)
        {
            var p = ClipperHelper.ToClipperCoordinates(polygon);
            // remove self-intersections and find the biggest polygon that"s left
            var simple = Clipper.SimplifyPolygon(p, PolyFillType.pftNonZero);

            if (simple == null || !simple.Any())
            {
                return null;
            }

            var biggest = simple[0];
            var biggestarea = Math.Abs(Clipper.Area(biggest));
            for (var i = 1; i < simple.Count(); i++)
            {
                var area = Math.Abs(Clipper.Area(simple[i]));
                if (area > biggestarea)
                {
                    biggest = simple[i];
                    biggestarea = area;
                }
            }

            // clean up singularities, coincident points and edges
            var clean = Clipper.CleanPolygon(biggest, _config.CurveTolerance * SvgNestConstants.ClipperScale);

            if (clean == null || !clean.Any())
            {
                return null;
            }

            return ClipperHelper.ToSvgNestCoordinates(clean);
        }

        // returns an array of SVG elements that represent the placement, for export or rendering
        public List<XmlElement> ApplyPlacement(List<List<Placement>> placement)
        {
            var clone = _parts.Select(p => p.CloneNode(false) as XmlElement).ToArray();
            var svglist = new List<XmlElement>();

            for (var i = 0; i < placement.Count; i++)
            {
                var newsvg = _svg.Element.CloneNode(false) as XmlElement;
                newsvg.SetAttribute("viewBox", "0 0 " + _binBounds.Width + " " + _binBounds.Height);
                newsvg.SetAttribute("width", _binBounds.Width + "px");
                newsvg.SetAttribute("height", _binBounds.Height + "px");

                //var binclone = bin.CloneNode(false) as XmlElement;
                //binclone.SetAttribute("class", "bin");
                //binclone.SetAttribute("transform", "translate(" + (-binBounds.X) + " " + (-binBounds.Y) + ")");
                //newsvg.AppendChild(binclone);

                for (var j = 0; j < placement[i].Count; j++)
                {
                    var p = placement[i][j];
                    var part = _tree[p.Id];

                    // the original path could have transforms and stuff on it, so apply our transforms on a group
                    var partgroup = _svg._document.CreateElement("g", newsvg.OwnerDocument.DocumentElement.NamespaceURI);
                    partgroup.SetAttribute("transform", "translate(" + p.X.ToString(culture) + " " + p.Y.ToString(culture) + ") rotate(" + p.Rotation + ")");
                    partgroup.AppendChild(clone[part.Source]);

                    if (part.Children != null && part.Children.Any())
                    {
                        var flattened = FlattenTree(part.Children.Select(c => new NodeHole
                        {
                            Node = c
                        }).ToList(), true);
                        for (var k = 0; k < flattened.Count; k++)
                        {

                            var c = clone[flattened[k].Node.Source];
                            // add class to indicate hole
                            if (flattened[k].Hole && (c.GetAttribute("class") == null || c.GetAttribute("class").IndexOf("hole") < 0))
                            {
                                c.SetAttribute("class", c.GetAttribute("class") + " hole");
                            }
                            partgroup.AppendChild(c);
                        }
                    }

                    newsvg.AppendChild(partgroup);
                }

                svglist.Add(newsvg);
            }

            CompactedSvgs = svglist;

            return svglist;
        }

        // flatten the given tree into a list
        private List<NodeHole> FlattenTree(List<NodeHole> t, bool hole)
        {
            var flat = new List<NodeHole>();
            for (var i = 0; i < t.Count; i++)
            {
                flat.Add(t[i]);
                t[i].Hole = hole;
                if (t[i].Node.Children != null && t[i].Node.Children.Any())
                {
                    flat.AddRange(FlattenTree(
                        t[i].Node.Children.Select(c => new NodeHole
                        {
                            Node = c
                        }).ToList(), !hole));
                }
            }

            return flat;
        }

        private async Task<KeyValuePair<SvgNestPair, List<DPath>>?> GetNodesPairsPathes(NodesPair pair)
        {
            if (pair == null)
            {
                return null;
            }
            var searchEdges = _config.ExploreConcave;
            var useHoles = _config.UseHoles;

            var A = PlacementWorker.RotatePolygon(pair.A, pair.Key.ARotation);
            var B = PlacementWorker.RotatePolygon(pair.B, pair.Key.BRotation);

            List<DPath> nfp;

            if (pair.Key.Inside)
            {
                if (GeometryUtil.IsRectangle(A.Points.ToArray(), 0.001))
                {
                    nfp = GeometryUtil.NoFitPolygonRectangle(A.Points.ToArray(), B.Points.ToArray());
                }
                else
                {
                    nfp = GeometryUtil.NoFitPolygon(A.Points.ToArray(), B.Points.ToArray(), true, searchEdges);
                }

                // ensure all interior NFPs have the same winding direction
                if (nfp != null && nfp.Any())
                {
                    for (var i = 0; i < nfp.Count; i++)
                    {
                        if (GeometryUtil.PolygonArea(nfp[i].ToArray()) > 0)
                        {
                            nfp[i].Reverse();
                        }
                    }
                }
                else
                {
                    // warning on null inner NFP
                    // this is not an error, as the part may simply be larger than the bin or otherwise unplaceable due to geometry
                    Console.WriteLine($"NFP Warning:  {pair.Key}");
                }
            }
            else
            {
                if (searchEdges)
                {
                    nfp = GeometryUtil.NoFitPolygon(A.Points.ToArray(), B.Points.ToArray(), false, searchEdges);
                }
                else
                {
                    nfp = MinkowskiDifference(A, B);
                }
                // sanity check
                if (nfp == null || !nfp.Any())
                {
                    Console.WriteLine($"NFP Error: {JsonConvert.SerializeObject(pair.Key)}");
                    Console.WriteLine($"A: {JsonConvert.SerializeObject(A)}");
                    Console.WriteLine($"B: {JsonConvert.SerializeObject(B)}");
                    return null;
                }

                for (var i = 0; i < nfp.Count; i++)
                {
                    if (!searchEdges || i == 0)
                    { // if searchedges is active, only the first NFP is guaranteed to pass sanity check
                        var nfpArea = Math.Abs(GeometryUtil.PolygonArea(nfp[i].ToArray()));
                        if (nfpArea < Math.Abs(GeometryUtil.PolygonArea(A.Points.ToArray())))
                        {
                            Console.WriteLine($"NFP Area Error: {nfpArea}, {JsonConvert.SerializeObject(pair.Key)}");
                            Console.WriteLine($"NFP: {JsonConvert.SerializeObject(nfp[i])}");
                            Console.WriteLine($"A: {JsonConvert.SerializeObject(A)}");
                            Console.WriteLine($"B: {JsonConvert.SerializeObject(B)}");
                            nfp.RemoveRange(i, 1);
                            return null;
                        }
                    }
                }

                if (nfp.Count == 0)
                {
                    return null;
                }

                // for outer NFPs, the first is guaranteed to be the largest. Any subsequent NFPs that lie inside the first are holes
                for (var i = 0; i < nfp.Count; i++)
                {
                    if (GeometryUtil.PolygonArea(nfp[i].ToArray()) > 0)
                    {
                        nfp[i].Reverse();
                    }

                    if (i > 0)
                    {
                        var pointInPolygon = GeometryUtil.PointInPolygon(nfp[i][0], nfp[0].ToArray());
                        if (pointInPolygon.HasValue && pointInPolygon.Value)
                        {
                            if (GeometryUtil.PolygonArea(nfp[i].ToArray()) < 0)
                            {
                                nfp[i].Reverse();
                            }
                        }
                    }
                }

                // generate nfps for children (holes of parts) if any exist
                if (useHoles && A.Children != null && A.Children.Any())
                {
                    var Bbounds = GeometryUtil.GetPolygonBounds(B.Points.ToArray());

                    for (var i = 0; i < A.Children.Count; i++)
                    {
                        var Abounds = GeometryUtil.GetPolygonBounds(A.Children[i].Points.ToArray());

                        // no need to find nfp if B"s bounding box is too big
                        if (Abounds.Width > Bbounds.Width && Abounds.Height > Bbounds.Height)
                        {

                            var cnfp = GeometryUtil.NoFitPolygon(A.Children[i].Points.ToArray(), B.Points.ToArray(), true, searchEdges);
                            // ensure all interior NFPs have the same winding direction
                            if (cnfp != null && cnfp.Any())
                            {
                                for (var j = 0; j < cnfp.Count; j++)
                                {
                                    if (GeometryUtil.PolygonArea(cnfp[j].ToArray()) < 0)
                                    {
                                        cnfp[j].Reverse();
                                    }
                                    nfp.Add(cnfp[j]);
                                }
                            }

                        }
                    }
                }
            }

            return new KeyValuePair<SvgNestPair, List<DPath>>(pair.Key, nfp);
        }

        private List<DPath> MinkowskiDifference(RotatedPolygons A, RotatedPolygons B)
        {
            var Acopy = A.Points.Select(p => new DoublePoint(p.X, p.Y)).ToArray();
            var APath = ClipperHelper.ToClipperCoordinates(Acopy);
            var BCopy = B.Points.Select(p => new DoublePoint(p.X, p.Y)).ToArray();
            var BPath = ClipperHelper.ToClipperCoordinates(BCopy)
                .Select(p => new IntPoint(-p.X, -p.Y)).ToList();

            var solution = Clipper.MinkowskiSum(APath, BPath, true);
            DoublePoint[] clipperNfp = null;

            double? largestArea = null;
            for (var i = 0; i < solution.Count; i++)
            {
                var n = ClipperHelper.ToSvgNestCoordinates(solution[i]);
                var sarea = GeometryUtil.PolygonArea(n);
                if (largestArea == null || largestArea > sarea)
                {
                    clipperNfp = n;
                    largestArea = sarea;
                }
            }

            for (var i = 0; i < clipperNfp.Length; i++)
            {
                clipperNfp[i].X += B.Points[0].X;
                clipperNfp[i].Y += B.Points[0].Y;
            }

            return new List<DPath> { clipperNfp.ToList() };
        }
    }
}

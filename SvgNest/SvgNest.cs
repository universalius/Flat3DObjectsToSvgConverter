using SvgLib;
using System.Xml;
using SvgNest.Utils;
using ClipperLib;
using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using DPath = System.Collections.Generic.List<ClipperLib.DoublePoint>;
using Plain3DObjectsToSvgConverter.Common.Extensions;
using Newtonsoft.Json;
using static SvgNest.PlacementWorker;
using SvgNest.Models.SvgNest;
using SvgNest.Helpers;
using SvgNest.Constants;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Globalization;

namespace SvgNest
{
    public class SvgNest
    {
        private SvgDocument svg = null;

        public List<XmlElement> compactedSvgs = null;

        // keep a reference to any style nodes, to maintain color/fill info
        private string style = null;

        private List<XmlElement> parts = null;

        private List<Node> tree = null;

        private XmlElement bin = null;
        private PolygonBounds binBounds = null;
        private Dictionary<string, List<DPath>> nfpCache = null;

        private CultureInfo culture = new CultureInfo("en-US", false);

        private SvgNestConfig config;

        private SvgParser svgParser;

        public SvgNest()
        {
            svgParser = new SvgParser();
            config = new SvgNestConfig();
            nfpCache = new Dictionary<string, List<DPath>>();
        }

        public SvgDocument parsesvg(string svgstring)
        {
            // reset if in progress
            //this.stop();

            bin = null;
            //binPolygon = null;
            tree = null;

            // parse svg
            svg = svgParser.load(svgstring);

            //this.style = svgParser.getStyle();

            svg = svgParser.cleanInput();

            tree = this.getParts(svg.Element.ChildNodes.Cast<XmlElement>().ToArray());

            return svg;
        }

        // progressCallback is called when progress is made
        // displayCallback is called when a new placement has been made
        public async Task start(/*progressCallback, displayCallback*/)
        {
            // Copy relevant scaling info
            bin = svg._document.CreateElement("rect");
            bin.SetAttribute("x", "0");
            bin.SetAttribute("y", "0");
            bin.SetAttribute("width", ((int)svg.Width).ToString());
            bin.SetAttribute("height", ((int)svg.Height).ToString());
            bin.SetAttribute("class", "fullRect");

            if (svg == null)
            {
                throw new Exception("Svg or bin is null");
            }

            var children = svg.Element.ChildNodes.Cast<XmlElement>().ToArray();

            var childrenCopy = new XmlElement[children.Length];
            Array.Copy(children, childrenCopy, children.Length);
            parts = childrenCopy.ToList();

            //var binindex = bin == null ? parts.IndexOf(bin) : 0;

            //if (binindex >= 0)
            //{
            //    // don"t process bin as a part of the tree
            //    parts.RemoveRange(binindex, 1);
            //}

            // build tree without bin
            //tree = this.getParts(parts.ToArray());

            offsetTree(tree, 0.5 * config.spacing);

            PolygonWithBounds binPolygon = new PolygonWithBounds { Points = svgParser.polygonify(bin) };
            binPolygon.Points = this.cleanPolygon(binPolygon.Points);

            if (binPolygon == null || binPolygon.Points.Length < 3)
            {
                throw new Exception("binPolygon is null or has less then 3 points");
            }

            binBounds = GeometryUtil.getPolygonBounds(binPolygon.Points);

            if (config.spacing > 0)
            {
                var offsetBin = this.polygonOffset(binPolygon.Points, -0.5 * config.spacing);
                if (offsetBin.Count == 1)
                {
                    // if the offset contains 0 or more than 1 path, something went wrong.
                    binPolygon.Points = offsetBin.Pop();
                }
            }

            binPolygon.id = -1;

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
            if (GeometryUtil.polygonArea(binPoints) > 0)
            {
                binPolygon.Points.Reverse();
            }

            // remove duplicate endpoints, ensure counterclockwise winding direction
            tree.ForEach(node =>
            {
                var start = node.points.First();
                var end = node.points.Last();
                if (start == end || (GeometryUtil.almostEqual(start.X, end.X) && GeometryUtil.almostEqual(start.Y, end.Y)))
                {
                    node.points.Pop();
                }

                if (GeometryUtil.polygonArea(node.points.ToArray()) > 0)
                {
                    node.points.Reverse();
                }
            });

            await launchWorkers(tree, binPolygon);

            //var self = this;
            //this.working = false;

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
        private void offsetTree(List<Node> tree, double offset)
        {
            tree.ForEach(node =>
        {
            var offsetpaths = polygonOffset(node.points.ToArray(), offset);
            if (offsetpaths.Count == 1)
            {
                // replace array items in place
                node.points = offsetpaths[0].ToList();
            }

            if (node.children != null && node.children.Any())
            {
                offsetTree(node.children, -offset);
            }
        });
        }

        private async Task<List<XmlElement>> launchWorkers(List<Node> tree, PolygonWithBounds binPolygon/*, progressCallback, displayCallback*/)
        {
            PlacementsFitness best = null;

            // initiate new GA
            var adam = new List<Node>(tree);

            // seed with decreasing area
            adam = adam.OrderByDescending((n) => (int)Math.Abs(GeometryUtil.polygonArea(n.points.ToArray()))).ToList();

            var GA = new GeneticAlgorithm(adam, binPolygon.Points, config);

            Individual individual = null;

            // evaluate all members of the population
            for (var i = 0; i < GA.population.Count; i++)
            {
                if (GA.population[i].fitness == 0)
                {
                    individual = GA.population[i];
                    break;
                }
            }

            if (individual == null)
            {
                // all individuals have been evaluated, start next generation
                GA.generation();
                individual = GA.population[1];
            }

            var placelist = individual.placement;
            var rotations = individual.rotation;

            var ids = new List<int>();
            for (var i = 0; i < placelist.Count; i++)
            {
                ids.Add(placelist[i].id);
                placelist[i].rotation = rotations[i];
            }

            var nfpPairs = new List<NodesPair>();
            SvgNestPair key;
            string keyJson;
            var newCache = new Dictionary<string, List<DPath>>();

            for (var i = 0; i < placelist.Count; i++)
            {
                var part = placelist[i];
                key = new SvgNestPair { A = binPolygon.id, B = part.id, inside = true, Arotation = 0, Brotation = rotations[i] };
                keyJson = JsonConvert.SerializeObject(key);

                if (!nfpCache.ContainsKey(keyJson))
                {
                    nfpPairs.Add(new NodesPair
                    {
                        A = new Node { id = binPolygon.id, points = binPolygon.Points.ToList() },
                        B = part,
                        key = key
                    });
                }
                else
                {
                    newCache[keyJson] = nfpCache[keyJson];
                }

                for (var j = 0; j < i; j++)
                {
                    var placed = placelist[j];
                    key = new SvgNestPair
                    {
                        A = placed.id,
                        B = part.id,
                        inside = false,
                        Arotation = rotations[j],
                        Brotation = rotations[i]
                    };

                    keyJson = JsonConvert.SerializeObject(key);
                    if (!nfpCache.ContainsKey(keyJson))
                    {
                        nfpPairs.Add(new NodesPair { A = placed, B = part, key = key });
                    }
                    else
                    {
                        newCache[keyJson] = nfpCache[keyJson];

                    }
                }
            }

            // only keep cache for one cycle
            nfpCache = newCache;

            var worker = new PlacementWorker(binPolygon, new List<Node>(placelist), ids, rotations, config, nfpCache);

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
                        nfpCache[cacheKey] = Nfp.Value.Value;
                    }
                }
            }
            worker.nfpCache = nfpCache;
            
            var placements = await Task.WhenAll(new List<List<Node>> { new List<Node>(placelist) }.Select(worker.placePaths));

            if (placements == null || !placements.Any())
            {
                return null;
            }

            individual.fitness = placements[0].fitness;
            var bestresult = placements[0];

            for (var i = 1; i < placements.Length; i++)
            {
                if (placements[i].fitness < bestresult.fitness)
                {
                    bestresult = placements[i];
                }
            }

            if (best == null || bestresult.fitness < best.fitness)
            {
                best = bestresult;

                double placedArea = 0;
                double totalArea = 0;
                var numParts = placelist.Count;
                var numPlacedParts = 0;

                for (var i = 0; i < best.placements.Count; i++)
                {
                    totalArea += Math.Abs(GeometryUtil.polygonArea(binPolygon.Points));
                    for (var j = 0; j < best.placements[i].Count; j++)
                    {
                        placedArea += Math.Abs(GeometryUtil.polygonArea(tree[best.placements[i][j].id].points.ToArray()));
                        numPlacedParts++;
                    }
                }

                var result = applyPlacement(best.placements);

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
        private List<Node> getParts(XmlElement[] paths)
        {
            int i, j;
            var nodes = new List<Node>();

            var numChildren = paths.Count();
            for (i = 0; i < numChildren; i++)
            {
                var poly = svgParser.polygonify(paths[i]);
                poly = this.cleanPolygon(poly)?.ToArray();

                // todo: warn user if poly could not be processed and is excluded from the nest
                if (poly != null && poly.Length > 2 && Math.Abs(GeometryUtil.polygonArea(poly)) > config.curveTolerance * config.curveTolerance)
                {
                    nodes.Add(new Node { points = poly.ToList(), source = i});
                }
            }

            // turn the list into a tree
            toTree(nodes);

            return nodes;
        }

        private int toTree(List<Node> list, int id = 0)
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

                    var pointInPolygon = GeometryUtil.pointInPolygon(p.points[0], list[j].points.ToArray());
                    if (pointInPolygon.HasValue && pointInPolygon.Value)
                    {
                        if (list[j].children == null)
                        {
                            list[j].children = new List<Node>();
                        }
                        list[j].children.Add(p);
                        p.parent = list[j];
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
                parents[i].id = id;
                id++;
            }

            for (var i = 0; i < parents.Count; i++)
            {
                if (parents[i].children != null)
                {
                    id = toTree(parents[i].children, id);
                }
            }

            return id;
        }

        // use the clipper library to return an offset to the given polygon. Positive offset expands the polygon, negative contracts
        // note that this returns an array of polygons
        private List<DoublePoint[]> polygonOffset(DoublePoint[] polygon, double offset)
        {
            var result = new List<DoublePoint[]>();
            if (offset == 0 || GeometryUtil.almostEqual(offset, 0))
            {
                result.Add(polygon);
                return result;
            }

            var p = ClipperHelper.ToClipperCoordinates(polygon);

            var miterLimit = 2;
            var co = new ClipperOffset(miterLimit, config.curveTolerance * SvgNestConstants.ClipperScale);
            co.AddPath(p, JoinType.jtRound, EndType.etClosedPolygon);

            var newpaths = new List<Path>();
            co.Execute(ref newpaths, offset * SvgNestConstants.ClipperScale);

            result = newpaths.Select(ClipperHelper.ToSvgNestCoordinates).ToList();
            return result;
        }

        // returns a less complex polygon that satisfies the curve tolerance
        private DoublePoint[] cleanPolygon(DoublePoint[] polygon)
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
            var clean = Clipper.CleanPolygon(biggest, config.curveTolerance * SvgNestConstants.ClipperScale);

            if (clean == null || !clean.Any())
            {
                return null;
            }

            return ClipperHelper.ToSvgNestCoordinates(clean);
        }

        // returns an array of SVG elements that represent the placement, for export or rendering
        public List<XmlElement> applyPlacement(List<List<Placement>> placement)
        {
            var clone = parts.Select(p => p.CloneNode(false) as XmlElement).ToArray();
            var svglist = new List<XmlElement>();

            for (var i = 0; i < placement.Count; i++)
            {
                var newsvg = svg.Element.CloneNode(false) as XmlElement;
                newsvg.SetAttribute("viewBox", "0 0 " + binBounds.Width + " " + binBounds.Height);
                newsvg.SetAttribute("width", binBounds.Width + "px");
                newsvg.SetAttribute("height", binBounds.Height + "px");

                //var binclone = bin.CloneNode(false) as XmlElement;
                //binclone.SetAttribute("class", "bin");
                //binclone.SetAttribute("transform", "translate(" + (-binBounds.X) + " " + (-binBounds.Y) + ")");
                //newsvg.AppendChild(binclone);

                for (var j = 0; j < placement[i].Count; j++)
                {
                    var p = placement[i][j];
                    var part = tree[p.id];

                    // the original path could have transforms and stuff on it, so apply our transforms on a group
                    var partgroup = svg._document.CreateElement("g", newsvg.OwnerDocument.DocumentElement.NamespaceURI);
                    partgroup.SetAttribute("transform", "translate(" + p.X.ToString(culture) + " " + p.Y.ToString(culture) + ") rotate(" + p.rotation + ")");
                    partgroup.AppendChild(clone[part.source]);

                    if (part.children != null && part.children.Any())
                    {
                        var flattened = _flattenTree(part.children.Select(c => new NodeHole
                        {
                            Node = c
                        }).ToList(), true);
                        for (var k = 0; k < flattened.Count; k++)
                        {

                            var c = clone[flattened[k].Node.source];
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

            compactedSvgs = svglist;

            return svglist;
        }

        class NodeHole
        {
            public Node Node { get; set; }

            public bool Hole { get; set; }
        }

        // flatten the given tree into a list
        private List<NodeHole> _flattenTree(List<NodeHole> t, bool hole)
        {
            var flat = new List<NodeHole>();
            for (var i = 0; i < t.Count; i++)
            {
                flat.Add(t[i]);
                t[i].Hole = hole;
                if (t[i].Node.children != null && t[i].Node.children.Any())
                {
                    flat.AddRange(_flattenTree(
                        t[i].Node.children.Select(c => new NodeHole
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
            var searchEdges = config.exploreConcave;
            var useHoles = config.useHoles;

            var A = PlacementWorker.rotatePolygon(pair.A, pair.key.Arotation);
            var B = PlacementWorker.rotatePolygon(pair.B, pair.key.Brotation);

            List<DPath> nfp;

            if (pair.key.inside)
            {
                if (GeometryUtil.isRectangle(A.points.ToArray(), 0.001))
                {
                    nfp = GeometryUtil.noFitPolygonRectangle(A.points.ToArray(), B.points.ToArray());
                }
                else
                {
                    nfp = GeometryUtil.noFitPolygon(A.points.ToArray(), B.points.ToArray(), true, searchEdges);
                }

                // ensure all interior NFPs have the same winding direction
                if (nfp != null && nfp.Any())
                {
                    for (var i = 0; i < nfp.Count; i++)
                    {
                        if (GeometryUtil.polygonArea(nfp[i].ToArray()) > 0)
                        {
                            nfp[i].Reverse();
                        }
                    }
                }
                else
                {
                    // warning on null inner NFP
                    // this is not an error, as the part may simply be larger than the bin or otherwise unplaceable due to geometry
                    Console.WriteLine($"NFP Warning:  {pair.key}");
                }
            }
            else
            {
                if (searchEdges)
                {
                    nfp = GeometryUtil.noFitPolygon(A.points.ToArray(), B.points.ToArray(), false, searchEdges);
                }
                else
                {
                    nfp = minkowskiDifference(A, B);
                }
                // sanity check
                if (nfp == null || !nfp.Any())
                {
                    Console.WriteLine($"NFP Error: {JsonConvert.SerializeObject(pair.key)}");
                    Console.WriteLine($"A: {JsonConvert.SerializeObject(A)}");
                    Console.WriteLine($"B: {JsonConvert.SerializeObject(B)}");
                    return null;
                }

                for (var i = 0; i < nfp.Count; i++)
                {
                    if (!searchEdges || i == 0)
                    { // if searchedges is active, only the first NFP is guaranteed to pass sanity check
                        var nfpArea = Math.Abs(GeometryUtil.polygonArea(nfp[i].ToArray()));
                        if (nfpArea < Math.Abs(GeometryUtil.polygonArea(A.points.ToArray())))
                        {
                            Console.WriteLine($"NFP Area Error: {nfpArea}, {JsonConvert.SerializeObject(pair.key)}");
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
                    if (GeometryUtil.polygonArea(nfp[i].ToArray()) > 0)
                    {
                        nfp[i].Reverse();
                    }

                    if (i > 0)
                    {
                        var pointInPolygon = GeometryUtil.pointInPolygon(nfp[i][0], nfp[0].ToArray());
                        if (pointInPolygon.HasValue && pointInPolygon.Value)
                        {
                            if (GeometryUtil.polygonArea(nfp[i].ToArray()) < 0)
                            {
                                nfp[i].Reverse();
                            }
                        }
                    }
                }

                // generate nfps for children (holes of parts) if any exist
                if (useHoles && A.children != null && A.children.Any())
                {
                    var Bbounds = GeometryUtil.getPolygonBounds(B.points.ToArray());

                    for (var i = 0; i < A.children.Count; i++)
                    {
                        var Abounds = GeometryUtil.getPolygonBounds(A.children[i].points.ToArray());

                        // no need to find nfp if B"s bounding box is too big
                        if (Abounds.Width > Bbounds.Width && Abounds.Height > Bbounds.Height)
                        {

                            var cnfp = GeometryUtil.noFitPolygon(A.children[i].points.ToArray(), B.points.ToArray(), true, searchEdges);
                            // ensure all interior NFPs have the same winding direction
                            if (cnfp != null && cnfp.Any())
                            {
                                for (var j = 0; j < cnfp.Count; j++)
                                {
                                    if (GeometryUtil.polygonArea(cnfp[j].ToArray()) < 0)
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

            return new KeyValuePair<SvgNestPair, List<DPath>>(pair.key, nfp);
        }

        private List<DPath> minkowskiDifference(RotatedPolygons A, RotatedPolygons B)
        {
            var Acopy = A.points.Select(p => new DoublePoint(p.X, p.Y)).ToArray();
            var APath = ClipperHelper.ToClipperCoordinates(Acopy);
            var BCopy = B.points.Select(p => new DoublePoint(p.X, p.Y)).ToArray();
            var BPath = ClipperHelper.ToClipperCoordinates(BCopy)
                .Select(p => new IntPoint(-p.X, -p.Y)).ToList();

            var solution = Clipper.MinkowskiSum(APath, BPath, true);
            DoublePoint[] clipperNfp = null;

            double? largestArea = null;
            for (var i = 0; i < solution.Count; i++)
            {
                var n = ClipperHelper.ToSvgNestCoordinates(solution[i]);
                var sarea = GeometryUtil.polygonArea(n);
                if (largestArea == null || largestArea > sarea)
                {
                    clipperNfp = n;
                    largestArea = sarea;
                }
            }

            for (var i = 0; i < clipperNfp.Length; i++)
            {
                clipperNfp[i].X += B.points[0].X;
                clipperNfp[i].Y += B.points[0].Y;
            }

            return new List<DPath> { clipperNfp.ToList() };
        }
    }
}

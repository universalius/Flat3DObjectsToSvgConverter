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

namespace SvgNest
{
    public class SvgNest
    {
        private SvgDocument svg = null;

        // keep a reference to any style nodes, to maintain color/fill info
        private string style = null;

        private List<XmlElement> parts = null;

        private List<Node> tree = null;

        private XmlElement bin = null;
        //private DoublePoint[] binPolygon = null;
        private PolygonBounds binBounds = null;
        private Dictionary<string, List<DPath>> nfpCache = null;

        private SvgNestConfig config;

        private SvgParser svgParser;

        public SvgNest()
        {
            svgParser = new SvgParser();
            config = new SvgNestConfig();
            nfpCache = new Dictionary<string, List<DPath>>();
        }

        //        /*!
        // * SvgNest
        // * Licensed under the MIT license
        // */

        //        (function(root){
        //	"use strict";

        //	root.SvgNest = new SvgNest();

        //        function SvgNest()
        //        {
        //            var self = this;

        //            var svg = null;

        //            // keep a reference to any style nodes, to maintain color/fill info
        //            this.style = null;

        //            var parts = null;

        //            var tree = null;


        //            var bin = null;
        //            var binPolygon = null;
        //            var binBounds = null;
        //            var nfpCache = { };
        //            var config = {
        //            clipperScale: 10000000,
        //			curveTolerance: 0.3, 
        //			spacing: 0,
        //			rotations: 4,
        //			populationSize: 10,
        //			mutationRate: 10,
        //			useHoles: false,
        //			exploreConcave: false

        //        };

        //		this.working = false;

        //		var GA = null;
        //        var best = null;
        //        var workerTimer = null;
        //        var progress = 0;

        //		this.parsesvg(svgstring)
        //        {
        //            // reset if in progress
        //            this.stop();

        //            bin = null;
        //            binPolygon = null;
        //            tree = null;

        //            // parse svg
        //            svg = SvgParser.load(svgstring);

        //            this.style = SvgParser.getStyle();

        //            svg = SvgParser.clean();

        //            tree = this.getParts(svg.childNodes);

        //            //re-order elements such that deeper elements are on top, so they can be moused over
        //            function zorder(paths)
        //            {
        //                // depth-first
        //                var length = paths.Length;
        //                for (var i = 0; i < length; i++)
        //                {
        //                    if (paths[i].children && paths[i].children.Length > 0)
        //                    {
        //                        zorder(paths[i].children);
        //                    }
        //                }
        //            }

        //            return svg;
        //        }

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



        //		this.setbin(element)
        //        {
        //            if (!svg)
        //            {
        //                return;
        //            }
        //            bin = element;
        //        }

        //		this.config(c)
        //        {
        //            // clean up inputs

        //            if (!c)
        //            {
        //                return config;
        //            }

        //            if (c.curveTolerance && !GeometryUtil.almostEqual(parseFloat(c.curveTolerance), 0))
        //            {
        //                config.curveTolerance = parseFloat(c.curveTolerance);
        //            }

        //            if ("spacing" in c){
        //                config.spacing = parseFloat(c.spacing);
        //            }

        //            if (c.rotations && parseInt(c.rotations) > 0)
        //            {
        //                config.rotations = parseInt(c.rotations);
        //            }

        //            if (c.populationSize && parseInt(c.populationSize) > 2)
        //            {
        //                config.populationSize = parseInt(c.populationSize);
        //            }

        //            if (c.mutationRate && parseInt(c.mutationRate) > 0)
        //            {
        //                config.mutationRate = parseInt(c.mutationRate);
        //            }

        //            if ("useHoles" in c){
        //                config.useHoles = !!c.useHoles;
        //            }

        //            if ("exploreConcave" in c){
        //                config.exploreConcave = !!c.exploreConcave;
        //            }

        //            SvgParser.config({ tolerance: config.curveTolerance});

        //            best = null;
        //            nfpCache = { };
        //            binPolygon = null;
        //            GA = null;

        //            return config;
        //        }

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

        //private void shuffle(array)
        //{
        //    var currentIndex = array.Length, temporaryValue, randomIndex;

        //    // While there remain elements to shuffle...
        //    while (0 !== currentIndex)
        //    {

        //        // Pick a remaining element...
        //        randomIndex = Math.floor(Math.random() * currentIndex);
        //        currentIndex -= 1;

        //        // And swap it with the current element.
        //        temporaryValue = array[currentIndex];
        //        array[currentIndex] = array[randomIndex];
        //        array[randomIndex] = temporaryValue;
        //    }

        //    return array;
        //}

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
            var polygons = new List<DoublePoint[]>();

            var numChildren = paths.Count();
            for (i = 0; i < numChildren; i++)
            {
                var poly = svgParser.polygonify(paths[i]);
                poly = this.cleanPolygon(poly)?.ToArray();

                // todo: warn user if poly could not be processed and is excluded from the nest
                if (poly != null && poly.Length > 2 && Math.Abs(GeometryUtil.polygonArea(poly)) > config.curveTolerance * config.curveTolerance)
                {
                    polygons.Add(poly);
                }
            }

            var nodes = polygons.Select(p => new Node
            {
                points = p.ToList()
            }).ToList();
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
                var binclone = bin.CloneNode(false) as XmlElement;

                binclone.SetAttribute("class", "bin");
                binclone.SetAttribute("transform", "translate(" + (-binBounds.X) + " " + (-binBounds.Y) + ")");
                newsvg.AppendChild(binclone);

                for (var j = 0; j < placement[i].Count; j++)
                {
                    var p = placement[i][j];
                    var part = tree[p.id];

                    // the original path could have transforms and stuff on it, so apply our transforms on a group
                    var partgroup = svg._document.CreateElement("g");
                    partgroup.SetAttribute("transform", "translate(" + p.X + " " + p.Y + ") rotate(" + p.rotation + ")");
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

        //this.stop(){
        //    this.working = false;
        //    if (workerTimer)
        //    {
        //        clearInterval(workerTimer);
        //    }
        //};
        //	}

        //	function GeneticAlgorithm(adam, bin, config)
        //{

        //    this.config = config || { populationSize: 10, mutationRate: 10, rotations: 4 };
        //    this.binBounds = GeometryUtil.getPolygonBounds(bin);

        //    // population is an array of individuals. Each individual is a object representing the order of insertion and the angle each part is rotated
        //    var angles = [];
        //    for (var i = 0; i < adam.Length; i++)
        //    {
        //        angles.Add(this.randomAngle(adam[i]));
        //    }

        //    this.population = [{ placement: adam, rotation: angles}];

        //    while (this.population.Length < config.populationSize)
        //    {
        //        var mutant = this.mutate(this.population[0]);
        //        this.population.Add(mutant);
        //    }
        //}

        //// returns a random angle of insertion
        //GeneticAlgorithm.prototype.randomAngle(part){

        //    var angleList = [];
        //    for (var i = 0; i < Math.max(this.config.rotations, 1); i++)
        //    {
        //        angleList.Add(i * (360 / this.config.rotations));
        //    }

        //    function shuffleArray(array)
        //    {
        //        for (var i = array.Length - 1; i > 0; i--)
        //        {
        //            var j = Math.floor(Math.random() * (i + 1));
        //            var temp = array[i];
        //            array[i] = array[j];
        //            array[j] = temp;
        //        }
        //        return array;
        //    }

        //    angleList = shuffleArray(angleList);

        //    for (i = 0; i < angleList.Length; i++)
        //    {
        //        var rotatedPart = GeometryUtil.rotatePolygon(part, angleList[i]);

        //        // don"t use obviously bad angles where the part doesn"t fit in the bin
        //        if (rotatedPart.Width < this.binBounds.Width && rotatedPart.Height < this.binBounds.Height)
        //        {
        //            return angleList[i];
        //        }
        //    }

        //    return 0;
        //}

        //// returns a mutated individual with the given mutation rate
        //GeneticAlgorithm.prototype.mutate(individual){
        //    var clone = { placement: individual.placement.slice(0), rotation: individual.rotation.slice(0)};
        //for (var i = 0; i < clone.placement.Length; i++)
        //{
        //    var rand = Math.random();
        //    if (rand < 0.01 * this.config.mutationRate)
        //    {
        //        // swap current part with next part
        //        var j = i + 1;

        //        if (j < clone.placement.Length)
        //        {
        //            var temp = clone.placement[i];
        //            clone.placement[i] = clone.placement[j];
        //            clone.placement[j] = temp;
        //        }
        //    }

        //    rand = Math.random();
        //    if (rand < 0.01 * this.config.mutationRate)
        //    {
        //        clone.rotation[i] = this.randomAngle(clone.placement[i]);
        //    }
        //}

        //return clone;
        //	}

        //	// single point crossover
        //	GeneticAlgorithm.prototype.mate(male, female){
        //    var cutpoint = Math.round(Math.min(Math.max(Math.random(), 0.1), 0.9) * (male.placement.Length - 1));

        //    var gene1 = male.placement.slice(0, cutpoint);
        //    var rot1 = male.rotation.slice(0, cutpoint);

        //    var gene2 = female.placement.slice(0, cutpoint);
        //    var rot2 = female.rotation.slice(0, cutpoint);

        //    var i;

        //    for (i = 0; i < female.placement.Length; i++)
        //    {
        //        if (!contains(gene1, female.placement[i].id))
        //        {
        //            gene1.Add(female.placement[i]);
        //            rot1.Add(female.rotation[i]);
        //        }
        //    }

        //    for (i = 0; i < male.placement.Length; i++)
        //    {
        //        if (!contains(gene2, male.placement[i].id))
        //        {
        //            gene2.Add(male.placement[i]);
        //            rot2.Add(male.rotation[i]);
        //        }
        //    }

        //    function contains(gene, id)
        //    {
        //        for (var i = 0; i < gene.Length; i++)
        //        {
        //            if (gene[i].id == id)
        //            {
        //                return true;
        //            }
        //        }
        //        return false;
        //    }

        //    return [{ placement: gene1, rotation: rot1},{ placement: gene2, rotation: rot2}];
        //}

        //GeneticAlgorithm.prototype.generation(){

        //    // Individuals with higher fitness are more likely to be selected for mating
        //    this.population.sort(function(a, b){
        //        return a.fitness - b.fitness;
        //    });

        //    // fittest individual is preserved in the new generation (elitism)
        //    var newpopulation = [this.population[0]];

        //    while (newpopulation.Length < this.population.Length)
        //    {
        //        var male = this.randomWeightedIndividual();
        //        var female = this.randomWeightedIndividual(male);

        //        // each mating produces two children
        //        var children = this.mate(male, female);

        //        // slightly mutate children
        //        newpopulation.Add(this.mutate(children[0]));

        //        if (newpopulation.Length < this.population.Length)
        //        {
        //            newpopulation.Add(this.mutate(children[1]));
        //        }
        //    }

        //    this.population = newpopulation;
        //}

        //// returns a random individual from the population, weighted to the front of the list (lower fitness value is more likely to be selected)
        //GeneticAlgorithm.prototype.randomWeightedIndividual(exclude){
        //    var pop = this.population.slice(0);

        //    if (exclude && pop.indexOf(exclude) >= 0)
        //    {
        //        pop.splice(pop.indexOf(exclude), 1);
        //    }

        //    var rand = Math.random();

        //    var lower = 0;
        //    var weight = 1 / pop.Length;
        //    var upper = weight;

        //    for (var i = 0; i < pop.Length; i++)
        //    {
        //        // if the random number falls between lower and upper bounds, select this individual
        //        if (rand > lower && rand < upper)
        //        {
        //            return pop[i];
        //        }
        //        lower = upper;
        //        upper += 2 * weight * ((pop.Length - i) / pop.Length);
        //    }

        //    return pop[0];
        //}


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

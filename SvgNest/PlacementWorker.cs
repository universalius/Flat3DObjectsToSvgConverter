using SvgNest.Utils;
using SvgNest;
using DPath = System.Collections.Generic.List<ClipperLib.DoublePoint>;
using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using static SvgNest.SvgNest;
using SvgNest.Models.SvgNest;
using System.IO;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using ClipperLib;
using SvgNest.Helpers;
using SvgNest.Constants;

namespace SvgNest
{
    public class PlacementWorker
    {
        public class PlacementsFitness : DoublePoint
        {
            public List<List<Placement>> placements { get; set; }
            public List<RotatedPolygons> paths { get; set; }
            public double fitness { get; set; }
            public double area { get; set; }
        }

        public class RotatedPolygons
        {
            public DPath points { get; set; }

            public List<RotatedPolygons> children { get; set; }

            public double rotation { get; set; }

            public int source { get; set; }

            public int id { get; set; }
        }

        public class Placement : DoublePoint
        {
            public int id { get; set; }
            public double rotation { get; set; }
            public List<Path> nfp { get; set; }
        }

        public static RotatedPolygons rotatePolygon(Node polygon, double degrees)
        {
            var angle = degrees * Math.PI / 180;
            var rotatedPolygon = GeometryUtil.rotatePolygon(polygon.points.ToArray(), angle);
            var rotated = new RotatedPolygons { points = rotatedPolygon.Points.ToList() };

            if (polygon.children != null && polygon.children.Any())
            {
                rotated.children = new List<RotatedPolygons>();
                for (var j = 0; j < polygon.children.Count; j++)
                {
                    rotated.children.Add(rotatePolygon(polygon.children[j], degrees));
                }
            }

            return rotated;
        }

        private PolygonWithBounds binPolygon;
        private List<Node> paths;
        private List<int> ids;
        private List<double> rotations;
        private SvgNestConfig config;

        public PlacementWorker(PolygonWithBounds binPolygon, List<Node> paths, List<int> ids, List<double> rotations, SvgNestConfig config,
            Dictionary<string, List<DPath>> nfpCache)
        {
            this.binPolygon = binPolygon;
            this.paths = paths;
            this.ids = ids;
            this.rotations = rotations;
            this.config = config;
            this.nfpCache = nfpCache ?? new Dictionary<string, List<DPath>>();
        }

        public Dictionary<string, List<DPath>> nfpCache { get; set; }

        // return a placement for the paths/rotations given
        // happens inside a webworker
        public async Task<PlacementsFitness> placePaths(List<Node> nodes)
        {
            if (binPolygon == null)
            {
                throw new ArgumentNullException("binPolygon is null");
            }

            // rotate paths by given rotation
            var rotated = nodes.Select(n =>
            {
                var r = rotatePolygon(n, n.rotation);
                r.rotation = n.rotation;
                r.source = n.source;
                r.id = n.id;

                return r;
            }).ToList();

            var paths = rotated;

            var allplacements = new List<List<Placement>>();
            double fitness = 0;
            var binarea = Math.Abs(GeometryUtil.polygonArea(binPolygon.Points));
            string key;
            List<DPath> nfp;

            while (paths.Count > 0)
            {
                double? minwidth = null;
                var placed = new List<RotatedPolygons>();
                var placements = new List<Placement>();
                fitness += 1; // add 1 for each new bin opened (lower fitness is better)

                for (var i = 0; i < paths.Count; i++)
                {
                    var path = paths[i];

                    // inner NFP
                    key = JsonConvert.SerializeObject(new SvgNestPair
                    {
                        A = -1,
                        B = path.id,
                        inside = true,
                        Arotation = 0,
                        Brotation = path.rotation
                    });
                    var binNfp = nfpCache[key];

                    // part unplaceable, skip
                    if (binNfp == null)
                    {
                        continue;
                    }

                    // ensure all necessary NFPs exist
                    var error = false;
                    for (var j = 0; j < placed.Count; j++)
                    {
                        key = JsonConvert.SerializeObject(new SvgNestPair
                        {
                            A = placed[j].id,
                            B = path.id,
                            inside = false,
                            Arotation = placed[j].rotation,
                            Brotation = path.rotation
                        });
                        nfp = nfpCache[key];

                        if (nfp == null)
                        {
                            error = true;
                            break;
                        }
                    }

                    // part unplaceable, skip
                    if (error)
                    {
                        continue;
                    }

                    Placement position = null;
                    if (placed.Count == 0)
                    {
                        // first placement, put it on the left
                        for (var j = 0; j < binNfp.Count; j++)
                        {
                            for (var k = 0; k < binNfp[j].Count; k++)
                            {
                                if (position == null || binNfp[j][k].X - path.points[0].X < position.X)
                                {
                                    position = new Placement
                                    {
                                        X = binNfp[j][k].X - path.points[0].X,
                                        Y = binNfp[j][k].Y - path.points[0].Y,
                                        id = path.id,
                                        rotation = path.rotation
                                    };
                                }
                            }
                        }

                        placements.Add(position);
                        placed.Add(path);

                        continue;
                    }

                    var clipperBinNfp = binNfp.Select(p => ClipperHelper.ToClipperCoordinates(p.ToArray())).ToList();
                    var clipper = new Clipper();
                    var combinedNfp = new List<Path>();

                    for (var j = 0; j < placed.Count; j++)
                    {
                        key = JsonConvert.SerializeObject(new SvgNestPair
                        {
                            A = placed[j].id,
                            B = path.id,
                            inside = false,
                            Arotation = placed[j].rotation,
                            Brotation = path.rotation
                        });
                        nfp = nfpCache[key];

                        if (nfp == null)
                        {
                            continue;
                        }

                        for (var k = 0; k < nfp.Count; k++)
                        {
                            var clone = nfp[k].Select(p => new DoublePoint(p.X, p.Y)).ToArray();
                            for (var m = 0; m < clone.Length; m++)
                            {
                                clone[m].X += placements[j].X;
                                clone[m].Y += placements[j].Y;
                            }

                            var clonePath = ClipperHelper.ToClipperCoordinates(clone);
                            clonePath = Clipper.CleanPolygon(clonePath, 0.0001 * SvgNestConstants.ClipperScale);
                            var area = Math.Abs(Clipper.Area(clonePath));
                            if (clonePath.Count > 2 && area > 0.1 * SvgNestConstants.ClipperScale * SvgNestConstants.ClipperScale)
                            {
                                clipper.AddPath(clonePath, PolyType.ptSubject, true);
                            }
                        }
                    }

                    if (!clipper.Execute(ClipType.ctUnion, combinedNfp, PolyFillType.pftNonZero, PolyFillType.pftNonZero))
                    {
                        continue;
                    }

                    // difference with bin polygon
                    var pathes = new List<Path>();

                    clipper.AddPaths(combinedNfp, PolyType.ptClip, true);
                    clipper.AddPaths(clipperBinNfp, PolyType.ptSubject, true);
                    if (!clipper.Execute(ClipType.ctDifference, pathes, PolyFillType.pftNonZero, PolyFillType.pftNonZero))
                    {
                        continue;
                    }

                    pathes = Clipper.CleanPolygons(pathes, 0.0001 * SvgNestConstants.ClipperScale);

                    for (var j = 0; j < pathes.Count; j++)
                    {
                        var area = Math.Abs(Clipper.Area(pathes[j]));
                        if (pathes[j].Count < 3 || area < 0.1 * SvgNestConstants.ClipperScale * SvgNestConstants.ClipperScale)
                        {
                            pathes.RemoveRange(j, 1);
                            j--;
                        }
                    }

                    if (pathes == null || !pathes.Any())
                    {
                        continue;
                    }

                    var finalNfp = pathes.Select(ClipperHelper.ToSvgNestCoordinates).ToList();

                    // choose placement that results in the smallest bounding box
                    // could use convex hull instead, but it can create oddly shaped nests (triangles or long slivers) which are not optimal for real-world use
                    // todo= generalize gravity direction

                    double? minarea = null;
                    double? minx = null;

                    for (var j = 0; j < finalNfp.Count; j++)
                    {
                        var nf = finalNfp[j];
                        if (Math.Abs(GeometryUtil.polygonArea(nf)) < 2)
                        {
                            continue;
                        }

                        for (var k = 0; k < nf.Length; k++)
                        {
                            var allpoints = new DPath();
                            for (var m = 0; m < placed.Count; m++)
                            {
                                for (var n = 0; n < placed[m].points.Count; n++)
                                {
                                    allpoints.Add(new DoublePoint(placed[m].points[n].X + placements[m].X, placed[m].points[n].Y + placements[m].Y));
                                }
                            }

                            var shiftvector = new Placement
                            {
                                X = nf[k].X - path.points[0].X,
                                Y = nf[k].Y - path.points[0].Y,
                                id = path.id,
                                rotation = path.rotation,
                                nfp = combinedNfp
                            };

                            for (var m = 0; m < path.points.Count; m++)
                            {
                                allpoints.Add(new DoublePoint(path.points[m].X + shiftvector.X, path.points[m].Y + shiftvector.Y));
                            }

                            var rectbounds = GeometryUtil.getPolygonBounds(allpoints.ToArray());

                            // weigh width more, to help compress in direction of gravity
                            var area = rectbounds.Width * 2 + rectbounds.Height;

                            if (minarea == null || area < minarea || (GeometryUtil.almostEqual(minarea.Value, area) && (minx == null || shiftvector.X < minx)))
                            {
                                minarea = area;
                                minwidth = rectbounds.Width;
                                position = shiftvector;
                                minx = shiftvector.X;
                            }
                        }
                    }
                    if (position != null)
                    {
                        placed.Add(path);
                        placements.Add(position);
                    }
                }

                if (minwidth != null)
                {
                    fitness += minwidth.Value / binarea;
                }

                for (var i = 0; i < placed.Count; i++)
                {
                    var index = paths.IndexOf(placed[i]);
                    if (index >= 0)
                    {
                        paths.RemoveRange(index, 1);
                    }
                }

                if (placements != null && placements.Any())
                {
                    allplacements.Add(placements);
                }
                else
                {
                    break; // something went wrong
                }
            }

            // there were parts that couldn't be placed
            fitness += 2 * paths.Count;

            return new PlacementsFitness { placements = allplacements, fitness = fitness, paths = paths, area = binarea };
        }
    }
}

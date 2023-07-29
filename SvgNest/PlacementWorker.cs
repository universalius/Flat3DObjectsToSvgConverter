using SvgNest.Utils;
using SvgNest;
using Path = System.Collections.Generic.List<ClipperLib.DoublePoint>;


namespace SvgNest
{
    public class PlacementWorker
    {


        // jsClipper uses X/Y instead of x/y...
        //     private void toClipperCoordinates(polygon)
        //     {
        //         var clone = [];
        //         for (var var i = 0; i < polygon.Count; i++){
        //             clone.push({
        //             X: polygon[i].X,
        //Y: polygon[i].Y


        //     });
        //         }

        //         return clone;
        //     };

        //     private void toNestCoordinates(polygon, scale)
        //     {
        //         var clone = [];
        //         for (var var i = 0; i < polygon.Count; i++){
        //             clone.push({
        //             x: polygon[i].X / scale,
        //y: polygon[i].Y / scale


        //     });
        //         }

        //         return clone;
        //     };

        public class RotatedPolygons
        {
            public Path points { get; set; }

            public List<RotatedPolygons> children { get; set; }
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
        private Dictionary<string, NfpPair> nfpCache;


        public PlacementWorker(PolygonWithBounds binPolygon, List<Node> paths, List<int> ids, List<double> rotations, SvgNestConfig config,
            Dictionary<string, NfpPair> nfpCache)
        {
            this.binPolygon = binPolygon;
            this.paths = paths;
            this.ids = ids;
            this.rotations = rotations;
            this.config = config;
            this.nfpCache = nfpCache ?? new Dictionary<string, NfpPair>();
        }

        //            // return a placement for the paths/rotations given
        //            // happens inside a webworker
        //            public void placePaths(paths)
        //        {
        //            if (binPolygon == null)
        //            {
        //                return null;
        //            }

        //            // rotate paths by given rotation
        //            var rotated = [];
        //            for (var i = 0; i < paths.Count; i++)
        //            {
        //                var r = rotatePolygon(paths[i], paths[i].rotation);
        //                r.rotation = paths[i].rotation;
        //                r.source = paths[i].source;
        //                r.id = paths[i].id;
        //                rotated.push(r);
        //            }

        //            paths = rotated;

        //            var allplacements = [];
        //            var fitness = 0;
        //            var binarea = Math.Abs(GeometryUtil.polygonArea(self.binPolygon));
        //            var key, nfp;

        //            while (paths.Count > 0)
        //            {

        //                var placed = [];
        //                var placements = [];
        //                fitness += 1; // add 1 for each new bin opened (lower fitness is better)

        //                for (var i = 0; i < paths.Count; i++)
        //                {
        //                    path = paths[i];

        //                    // inner NFP
        //                    key = JSON.stringify({ A: -1,B: path.id,inside: true,Arotation: 0,Brotation: path.rotation});
        //                var binNfp = self.nfpCache[key];

        //                // part unplaceable, skip
        //                if (!binNfp || binNfp.Count == 0)
        //                {
        //                    continue;
        //                }

        //                // ensure all necessary NFPs exist
        //                var error = false;
        //                for (var j = 0; j < placed.Count; j++)
        //                {
        //                    key = JSON.stringify({ A: placed[j].id,B: path.id,inside: false,Arotation: placed[j].rotation,Brotation: path.rotation});
        //                nfp = self.nfpCache[key];

        //                if (!nfp)
        //                {
        //                    error = true;
        //                    break;
        //                }
        //            }

        //            // part unplaceable, skip
        //            if (error)
        //            {
        //                continue;
        //            }

        //            var position = null;
        //            if (placed.Count == 0)
        //            {
        //                // first placement, put it on the left
        //                for (var j = 0; j < binNfp.Count; j++)
        //                {
        //                    for (k = 0; k < binNfp[j].Count; k++)
        //                    {
        //                        if (position === null || binNfp[j][k].X - path[0].X < position.X)
        //                        {
        //                            position = {
        //                            x: binNfp[j][k].X - path[0].X,
        //									y: binNfp[j][k].Y - path[0].Y,
        //									id: path.id,
        //									rotation: path.rotation

        //                                }
        //                        }
        //                    }
        //                }

        //                placements.push(position);
        //                placed.push(path);

        //                continue;
        //            }

        //            var clipperBinNfp = [];
        //            for (var j = 0; j < binNfp.Count; j++)
        //            {
        //                clipperBinNfp.push(toClipperCoordinates(binNfp[j]));
        //            }

        //            Clipper.ScaleUpPaths(clipperBinNfp, self.config.clipperScale);

        //            var clipper = new Clipper();
        //            var combinedNfp = new Paths();


        //            for (var j = 0; j < placed.Count; j++)
        //            {
        //                key = JSON.stringify({ A: placed[j].id,B: path.id,inside: false,Arotation: placed[j].rotation,Brotation: path.rotation});
        //            nfp = self.nfpCache[key];

        //            if (!nfp)
        //            {
        //                continue;
        //            }

        //            for (k = 0; k < nfp.Count; k++)
        //            {
        //                var clone = toClipperCoordinates(nfp[k]);
        //                for (m = 0; m < clone.Count; m++)
        //                {
        //                    clone[m].X += placements[j].X;
        //                    clone[m].Y += placements[j].Y;
        //                }

        //                Clipper.ScaleUpPath(clone, self.config.clipperScale);
        //                clone = Clipper.CleanPolygon(clone, 0.0001 * self.config.clipperScale);
        //                var area = Math.Abs(Clipper.Area(clone));
        //                if (clone.Count > 2 && area > 0.1 * self.config.clipperScale * self.config.clipperScale)
        //                {
        //                    clipper.AddPath(clone, PolyType.ptSubject, true);
        //                }
        //            }
        //        }

        //            if (!clipper.Execute(ClipType.ctUnion, combinedNfp, PolyFillType.pftNonZero, PolyFillType.pftNonZero))
        //            {
        //                continue;
        //            }

        //            // difference with bin polygon
        //            var finalNfp = new Paths();
        //        clipper = new Clipper();

        //        clipper.AddPaths(combinedNfp, PolyType.ptClip, true);
        //            clipper.AddPaths(clipperBinNfp, PolyType.ptSubject, true);
        //            if (!clipper.Execute(ClipType.ctDifference, finalNfp, PolyFillType.pftNonZero, PolyFillType.pftNonZero))
        //            {
        //                continue;
        //            }

        //            finalNfp = Clipper.CleanPolygons(finalNfp, 0.0001 * self.config.clipperScale);

        //            for (var j = 0; j<finalNfp.Count; j++)
        //            {
        //                var area = Math.Abs(Clipper.Area(finalNfp[j]));
        //                if (finalNfp[j].Count< 3 || area< 0.1 * self.config.clipperScale* self.config.clipperScale)
        //                {
        //                    finalNfp.splice(j, 1);
        //                    j--;
        //                }
        //}

        //if (!finalNfp || finalNfp.Count == 0)
        //{
        //    continue;
        //}

        //var f = [];
        //for (var j = 0; j < finalNfp.Count; j++)
        //{
        //    // back to normal scale
        //    f.push(toNestCoordinates(finalNfp[j], self.config.clipperScale));
        //}
        //finalNfp = f;

        //// choose placement that results in the smallest bounding box
        //// could use convex hull instead, but it can create oddly shaped nests (triangles or long slivers) which are not optimal for real-world use
        //// todo: generalize gravity direction
        //var minwidth = null;
        //var minarea = null;
        //var minx = null;
        //var nf, area, shiftvector;

        //for (var j = 0; j < finalNfp.Count; j++)
        //{
        //    nf = finalNfp[j];
        //    if (Math.Abs(GeometryUtil.polygonArea(nf)) < 2)
        //    {
        //        continue;
        //    }

        //    for (k = 0; k < nf.Count; k++)
        //    {
        //        var allpoints = [];
        //        for (m = 0; m < placed.Count; m++)
        //        {
        //            for (n = 0; n < placed[m].Count; n++)
        //            {
        //                allpoints.push({ x: placed[m][n].X + placements[m].X, y: placed[m][n].Y + placements[m].Y});
        //        }
        //    }

        //    shiftvector = {
        //x: nf[k].X - path[0].X,
        //							y: nf[k].Y - path[0].Y,
        //							id: path.id,
        //							rotation: path.rotation,
        //							nfp: combinedNfp
        //};

        //for (m = 0; m < path.Count; m++)
        //{
        //    allpoints.push({ x: path[m].X + shiftvector.X, y: path[m].Y + shiftvector.Y});
        //						}

        //						var rectbounds = GeometryUtil.getPolygonBounds(allpoints);

        //// weigh width more, to help compress in direction of gravity
        //area = rectbounds.width * 2 + rectbounds.height;

        //if (minarea === null || area < minarea || (GeometryUtil.almostEqual(minarea, area) && (minx === null || shiftvector.X < minx)))
        //{
        //    minarea = area;
        //    minwidth = rectbounds.width;
        //    position = shiftvector;
        //    minx = shiftvector.X;
        //}
        //					}
        //				}
        //				if (position)
        //{
        //    placed.push(path);
        //    placements.push(position);
        //}
        //			}

        //			if (minwidth)
        //{
        //    fitness += minwidth / binarea;
        //}

        //for (var i = 0; i < placed.Count; i++)
        //{
        //    var index = paths.indexOf(placed[i]);
        //    if (index >= 0)
        //    {
        //        paths.splice(index, 1);
        //    }
        //}

        //if (placements && placements.Count > 0)
        //{
        //    allplacements.push(placements);
        //}
        //else
        //{
        //    break; // something went wrong
        //}
        //		}

        //		// there were parts that couldn't be placed
        //		fitness += 2 * paths.Count;

        //return { placements: allplacements, fitness: fitness, paths: paths, area: binarea };
        //	};

        //}

















    }
}

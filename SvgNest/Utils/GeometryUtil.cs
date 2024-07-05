using ClipperLib;
using SvgNest.Models.GeometryUtil;
using System.ComponentModel;
using static SvgNest.Utils.GeometryUtil;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Path = System.Collections.Generic.List<ClipperLib.DoublePoint>;


namespace SvgNest.Utils
{
    public static class GeometryUtil
    {
        // floating point comparison tolerance
        private static double TOL = Math.Pow(10, -9); // Floating point error is likely to be above 1 epsilon

        public static bool AlmostEqual(double a, double b, double? tolerance = null)
        {
            return Math.Abs(a - b) < (tolerance ?? TOL);
        }

        // returns the area of the polygon, assuming no self-intersections
        // a negative area indicates counter-clockwise winding direction
        public static double PolygonArea(DoublePoint[] polygon)
        {
            var area = 0.0;
            for (int i = 0; i < polygon.Length; i++)
            {
                var j = i == 0 ? polygon.Length - 1 : i - 1;
                area += (polygon[j].X + polygon[i].X) * (polygon[j].Y - polygon[i].Y);
            }

            return 0.5 * area;
        }

        /// <summary>
        /// Return true if point is in the polygon, false if outside, and null if exactly on a point or edge
        /// </summary>
        public static bool? PointInPolygon(DoublePoint point, DoublePoint[] polygon)
        {
            if (polygon == null || polygon.Length < 3)
            {
                return null;
            }

            var inside = false;
            var offsetx = 0;
            var offsety = 0;

            for (int i = 0; i < polygon.Length; i++)
            {
                var j = i == 0 ? polygon.Length - 1 : i - 1;
                var xi = polygon[i].X + offsetx;
                var yi = polygon[i].Y + offsety;
                var xj = polygon[j].X + offsetx;
                var yj = polygon[j].Y + offsety;

                if (AlmostEqual(xi, point.X) && AlmostEqual(yi, point.Y))
                {
                    return null; // no result
                }

                if (OnSegment(new DoublePoint(xi, yi), new DoublePoint(xj, yj), point))
                {
                    return null; // exactly on the segment
                }

                if (AlmostEqual(xi, xj) && AlmostEqual(yi, yj))
                { // ignore very small lines
                    continue;
                }

                var intersect = ((yi > point.Y) != (yj > point.Y)) && (point.X < (xj - xi) * (point.Y - yi) / (yj - yi) + xi);
                if (intersect) inside = !inside;
            }

            return inside;
        }

        // returns the rectangular bounding box of the given polygon
        public static PolygonBounds GetPolygonBounds(DoublePoint[] polygon)
        {
            if (polygon == null || polygon.Length < 3)
            {
                return null;
            }

            var xmin = polygon[0].X;
            var xmax = polygon[0].X;
            var ymin = polygon[0].Y;
            var ymax = polygon[0].Y;

            for (var i = 1; i < polygon.Length; i++)
            {
                if (polygon[i].X > xmax)
                {
                    xmax = polygon[i].X;
                }
                else if (polygon[i].X < xmin)
                {
                    xmin = polygon[i].X;
                }

                if (polygon[i].Y > ymax)
                {
                    ymax = polygon[i].Y;
                }
                else if (polygon[i].Y < ymin)
                {
                    ymin = polygon[i].Y;
                }
            }

            return new PolygonBounds()
            {
                X = xmin,
                Y = ymin,
                Width = xmax - xmin,
                Height = ymax - ymin
            };
        }

        public static PolygonWithBounds RotatePolygon(DoublePoint[] polygon, double angle)
        {
            angle = angle * Math.PI / 180;

            var rotated = polygon.Select(point =>
            {
                var x = point.X;
                var y = point.Y;
                var x1 = x * Math.Cos(angle) - y * Math.Sin(angle);
                var y1 = x * Math.Sin(angle) + y * Math.Cos(angle);

                return new DoublePoint(x1, y1);
            }).ToArray();

            // reset bounding box
            var bounds = GetPolygonBounds(rotated);
            return new PolygonWithBounds
            {
                Points = rotated,
                Bounds = bounds
            };
        }

        public static bool IsRectangle(DoublePoint[] poly, double? tolerance)
        {
            var bb = GetPolygonBounds(poly);
            tolerance = tolerance ?? TOL;

            for (var i = 0; i < poly.Length; i++)
            {
                if (!AlmostEqual(poly[i].X, bb.X) && !AlmostEqual(poly[i].X, bb.X + bb.Width))
                {
                    return false;
                }
                if (!AlmostEqual(poly[i].Y, bb.Y) && !AlmostEqual(poly[i].Y, bb.Y + bb.Height))
                {
                    return false;
                }
            }

            return true;
        }

        // returns an interior NFP for the special case where A is a rectangle
        public static List<Path> NoFitPolygonRectangle(DoublePoint[] A, DoublePoint[] B)
        {
            var minAx = A[0].X;
            var minAy = A[0].Y;
            var maxAx = A[0].X;
            var maxAy = A[0].Y;

            for (var i = 1; i < A.Length; i++)
            {
                if (A[i].X < minAx)
                {
                    minAx = A[i].X;
                }
                if (A[i].Y < minAy)
                {
                    minAy = A[i].Y;
                }
                if (A[i].X > maxAx)
                {
                    maxAx = A[i].X;
                }
                if (A[i].Y > maxAy)
                {
                    maxAy = A[i].Y;
                }
            }

            var minBx = B[0].X;
            var minBy = B[0].Y;
            var maxBx = B[0].X;
            var maxBy = B[0].Y;
            for (var i = 1; i < B.Length; i++)
            {
                if (B[i].X < minBx)
                {
                    minBx = B[i].X;
                }
                if (B[i].Y < minBy)
                {
                    minBy = B[i].Y;
                }
                if (B[i].X > maxBx)
                {
                    maxBx = B[i].X;
                }
                if (B[i].Y > maxBy)
                {
                    maxBy = B[i].Y;
                }
            }

            if (maxBx - minBx > maxAx - minAx)
            {
                return null;
            }
            if (maxBy - minBy > maxAy - minAy)
            {
                return null;
            }

            return new List<Path> {
                new Path {
                    new DoublePoint(minAx - minBx + B[0].X,minAy - minBy + B[0].Y),
                    new DoublePoint(maxAx - maxBx + B[0].X,minAy - minBy + B[0].Y),
                    new DoublePoint(maxAx - maxBx + B[0].X,maxAy - maxBy + B[0].Y),
                    new DoublePoint(minAx - minBx + B[0].X,maxAy - maxBy + B[0].Y)
                }
            };
        }

        // given a static polygon A and a movable polygon B, compute a no fit polygon by orbiting B about A
        // if the inside flag is set, B is orbited inside of A rather than outside
        // if the searchEdges flag is set, all edges of A are explored for NFPs - multiple 
        public static List<Path> NoFitPolygon(DoublePoint[] poly1, DoublePoint[] poly2, bool inside, bool searchEdges)
        {
            if (poly1 == null || poly1.Length < 3 || poly2 == null || poly2.Length < 3)
            {
                return null;
            }

            var a = poly1.Select(p => new PointWithMark { X = p.X, Y = p.Y }).ToArray();
            var b = poly2.Select(p => new PointWithMark { X = p.X, Y = p.Y }).ToArray();
            var A = new PointsWithOffset { Points = a };
            var B = new PointsWithOffset { Points = b };

            var minA = a[0].Y;
            var minAindex = 0;

            var maxB = b[0].Y;
            var maxBindex = 0;

            for (var i = 1; i < a.Length; i++)
            {
                a[i].Marked = false;
                if (a[i].Y < minA)
                {
                    minA = a[i].Y;
                    minAindex = i;
                }
            }

            for (var i = 1; i < b.Length; i++)
            {
                b[i].Marked = false;
                if (b[i].Y > maxB)
                {
                    maxB = b[i].Y;
                    maxBindex = i;
                }
            }

            DoublePoint startpoint;
            if (!inside)
            {
                // shift B such that the bottom-most point of B is at the top-most point of A. This guarantees an initial placement with no intersections
                startpoint = new DoublePoint(a[minAindex].X - b[maxBindex].X, a[minAindex].Y - b[maxBindex].Y);
            }
            else
            {
                // no reliable heuristic for inside
                startpoint = SearchStartPoint(A, B, true);
            }

            var NFPlist = new List<Path>();

            while (startpoint != null)
            {

                B.OffsetX = startpoint.X;
                B.OffsetY = startpoint.Y;

                // maintain a list of touching points/edges
                var touching = new List<Touching>();

                Vector prevvector = null; // keep track of previous vector
                var NFP = new Path { new DoublePoint(b[0].X + B.OffsetX, b[0].Y + B.OffsetY) };

                var referencex = b[0].X + B.OffsetX;
                var referencey = b[0].Y + B.OffsetY;
                var startx = referencex;
                var starty = referencey;
                var counter = 0;

                while (counter < 10 * (a.Length + b.Length))
                { // sanity check, prevent infinite loop
                    touching = new List<Touching>();
                    // find touching vertices/edges
                    for (var i = 0; i < a.Length; i++)
                    {
                        var nexti = (i == a.Length - 1) ? 0 : i + 1;
                        for (var j = 0; j < b.Length; j++)
                        {
                            var nextj = (j == b.Length - 1) ? 0 : j + 1;
                            if (AlmostEqual(a[i].X, b[j].X + B.OffsetX) && AlmostEqual(a[i].Y, b[j].Y + B.OffsetY))
                            {
                                touching.Add(new Touching { Type = 0, A = i, B = j });
                            }

                            else if (OnSegment(a[i], a[nexti], new DoublePoint(b[j].X + B.OffsetX, b[j].Y + B.OffsetY)))
                            {
                                touching.Add(new Touching { Type = 1, A = nexti, B = j });
                            }

                            else if (OnSegment(new DoublePoint(b[j].X + B.OffsetX, y: b[j].Y + B.OffsetY),
                                new DoublePoint(b[nextj].X + B.OffsetX, b[nextj].Y + B.OffsetY), a[i]))
                            {
                                touching.Add(new Touching { Type = 2, A = i, B = nextj });
                            }
                        }
                    }

                    // generate translation vectors from touching vertices/edges
                    var vectors = new List<Vector>();
                    for (var i = 0; i < touching.Count; i++)
                    {
                        var vertexA = a[touching[i].A];
                        vertexA.Marked = true;

                        // adjacent A vertices
                        var prevAindex = touching[i].A - 1;
                        var nextAindex = touching[i].A + 1;

                        prevAindex = (prevAindex < 0) ? a.Length - 1 : prevAindex; // loop
                        nextAindex = (nextAindex >= a.Length) ? 0 : nextAindex; // loop

                        var prevA = a[prevAindex];
                        var nextA = a[nextAindex];

                        // adjacent B vertices
                        var vertexB = b[touching[i].B];

                        var prevBindex = touching[i].B - 1;
                        var nextBindex = touching[i].B + 1;

                        prevBindex = (prevBindex < 0) ? b.Length - 1 : prevBindex; // loop
                        nextBindex = (nextBindex >= b.Length) ? 0 : nextBindex; // loop

                        var prevB = b[prevBindex];
                        var nextB = b[nextBindex];

                        if (touching[i].Type == 0)
                        {

                            var vA1 = new Vector
                            {
                                X = prevA.X - vertexA.X,
                                Y = prevA.Y - vertexA.Y,
                                Start = vertexA,
                                End = prevA
                            };

                            var vA2 = new Vector
                            {
                                X = nextA.X - vertexA.X,
                                Y = nextA.Y - vertexA.Y,
                                Start = vertexA,
                                End = nextA
                            };

                            // B vectors need to be inverted
                            var vB1 = new Vector
                            {
                                X = vertexB.X - prevB.X,
                                Y = vertexB.Y - prevB.Y,
                                Start = prevB,
                                End = vertexB
                            };

                            var vB2 = new Vector
                            {
                                X = vertexB.X - nextB.X,
                                Y = vertexB.Y - nextB.Y,
                                Start = nextB,
                                End = vertexB
                            };

                            vectors.Add(vA1);
                            vectors.Add(vA2);
                            vectors.Add(vB1);
                            vectors.Add(vB2);
                        }

                        else if (touching[i].Type == 1)
                        {
                            vectors.Add(new Vector
                            {
                                X = vertexA.X - (vertexB.X + B.OffsetX),
                                Y = vertexA.Y - (vertexB.Y + B.OffsetY),
                                Start = prevA,
                                End = vertexA

                            });

                            vectors.Add(new Vector
                            {
                                X = prevA.X - (vertexB.X + B.OffsetX),
                                Y = prevA.Y - (vertexB.Y + B.OffsetY),
                                Start = vertexA,
                                End = prevA
                            });
                        }
                        else if (touching[i].Type == 2)
                        {
                            vectors.Add(new Vector
                            {
                                X = vertexA.X - (vertexB.X + B.OffsetX),
                                Y = vertexA.Y - (vertexB.Y + B.OffsetY),
                                Start = prevB,
                                End = vertexB
                            });

                            vectors.Add(new Vector
                            {
                                X = vertexA.X - (prevB.X + B.OffsetX),
                                Y = vertexA.Y - (prevB.Y + B.OffsetY),
                                Start = vertexB,
                                End = prevB
                            });
                        }
                    }

                    // todo: there should be a faster way to reject vectors that will cause immediate intersection. For now just check them all

                    Vector translate = null;
                    double maxd = 0;

                    for (var i = 0; i < vectors.Count; i++)
                    {
                        if (vectors[i].X == 0 && vectors[i].Y == 0)
                        {
                            continue;
                        }

                        // if this vector points us back to where we came from, ignore it.
                        // ie cross product = 0, dot product < 0
                        if (prevvector != null && vectors[i].Y * prevvector.Y + vectors[i].X * prevvector.X < 0)
                        {

                            // compare magnitude with unit vectors
                            var vectorlength = Math.Sqrt(vectors[i].X * vectors[i].X + vectors[i].Y * vectors[i].Y);
                            var unitv = new DoublePoint(vectors[i].X / vectorlength, vectors[i].Y / vectorlength);

                            var prevlength = Math.Sqrt(prevvector.X * prevvector.X + prevvector.Y * prevvector.Y);
                            var prevunit = new DoublePoint(prevvector.X / prevlength, prevvector.Y / prevlength);

                            // we need to scale down to unit vectors to normalize vector length. Could also just do a tan here
                            if (Math.Abs(unitv.Y * prevunit.X - unitv.X * prevunit.Y) < 0.0001)
                            {
                                continue;
                            }
                        }

                        var d = PolygonSlideDistance(A, B, vectors[i], true);
                        var vecd2 = vectors[i].X * vectors[i].X + vectors[i].Y * vectors[i].Y;

                        if (d == null || d * d > vecd2)
                        {
                            var vecd = Math.Sqrt(vectors[i].X * vectors[i].X + vectors[i].Y * vectors[i].Y);
                            d = vecd;
                        }

                        if (d != null && d > maxd)
                        {
                            maxd = d.Value;
                            translate = vectors[i];
                        }
                    }

                    if (translate == null || AlmostEqual(maxd, 0))
                    {
                        // didn't close the loop, something went wrong here
                        NFP = null;
                        break;
                    }

                    translate.Start.Marked = true;
                    translate.End.Marked = true;

                    prevvector = translate;

                    // trim
                    var vlength2 = translate.X * translate.X + translate.Y * translate.Y;
                    if (maxd * maxd < vlength2 && !AlmostEqual(maxd * maxd, vlength2))
                    {
                        var scale = Math.Sqrt((maxd * maxd) / vlength2);
                        translate.X *= scale;
                        translate.Y *= scale;
                    }

                    referencex += translate.X;
                    referencey += translate.Y;

                    if (AlmostEqual(referencex, startx) && AlmostEqual(referencey, starty))
                    {
                        // we've made a full loop
                        break;
                    }

                    // if A and B start on a touching horizontal line, the end point may not be the start point
                    var looped = false;
                    if (NFP.Count > 0)
                    {
                        for (var i = 0; i < NFP.Count - 1; i++)
                        {
                            if (AlmostEqual(referencex, NFP[i].X) && AlmostEqual(referencey, NFP[i].Y))
                            {
                                looped = true;
                            }
                        }
                    }

                    if (looped)
                    {
                        // we've made a full loop
                        break;
                    }

                    NFP.Add(new DoublePoint(referencex, referencey));

                    B.OffsetX += translate.X;
                    B.OffsetY += translate.Y;

                    counter++;
                }

                if (NFP != null && NFP.Any())
                {
                    NFPlist.Add(NFP);
                }

                if (!searchEdges)
                {
                    // only get outer NFP or first inner NFP
                    break;
                }

                startpoint = SearchStartPoint(A, B, inside, NFPlist);

            }

            return NFPlist;
        }

        // returns the intersection of AB and EF
        // or null if there are no intersections or other numerical error
        // if the infinite flag is set, AE and EF describe infinite lines without endpoints, they are finite line segments otherwise
        public static DoublePoint LineIntersect(DoublePoint A, DoublePoint B, DoublePoint E, DoublePoint F, bool infinite = false)
        {
            var a1 = B.Y - A.Y;
            var b1 = A.X - B.X;
            var c1 = B.X * A.Y - A.X * B.Y;
            var a2 = F.Y - E.Y;
            var b2 = E.X - F.X;
            var c2 = F.X * E.Y - E.X * F.Y;

            var denom = a1 * b2 - a2 * b1;

            var x = (b1 * c2 - b2 * c1) / denom;
            var y = (a2 * c1 - a1 * c2) / denom;

            if (!double.IsFinite(x) || !double.IsFinite(y))
            {
                return null;
            }

            // lines are colinear
            /*var crossABE = (E.Y - A.Y) * (B.X - A.X) - (E.X - A.X) * (B.Y - A.Y);
            var crossABF = (F.Y - A.Y) * (B.X - A.X) - (F.X - A.X) * (B.Y - A.Y);
            if(almostEqual(crossABE,0) && almostEqual(crossABF,0)){
                return null;
            }*/

            if (!infinite)
            {
                // coincident points do not count as intersecting
                if (Math.Abs(A.X - B.X) > TOL && ((A.X < B.X) ? x < A.X || x > B.X : x > A.X || x < B.X)) return null;
                if (Math.Abs(A.Y - B.Y) > TOL && ((A.Y < B.Y) ? y < A.Y || y > B.Y : y > A.Y || y < B.Y)) return null;

                if (Math.Abs(E.X - F.X) > TOL && ((E.X < F.X) ? x < E.X || x > F.X : x > E.X || x < F.X)) return null;
                if (Math.Abs(E.Y - F.Y) > TOL && ((E.Y < F.Y) ? y < E.Y || y > F.Y : y > E.Y || y < F.Y)) return null;
            }

            return new DoublePoint(x, y);
        }

        // todo: swap this for a more efficient sweep-line implementation
        // returnEdges: if set, return all edges on A that have intersections
        private static bool Intersect(PointsWithOffset A, PointsWithOffset B)
        {
            var Aoffsetx = A.OffsetX;
            var Aoffsety = A.OffsetY;

            var Boffsetx = B.OffsetX;
            var Boffsety = B.OffsetY;

            A = A.Clone();
            B = B.Clone();
            var a = A.Points;
            var b = B.Points;

            for (var i = 0; i < a.Length - 1; i++)
            {
                for (var j = 0; j < b.Length - 1; j++)
                {
                    var a1 = new DoublePoint(a[i].X + Aoffsetx, a[i].Y + Aoffsety);
                    var a2 = new DoublePoint(a[i + 1].X + Aoffsetx, a[i + 1].Y + Aoffsety);
                    var b1 = new DoublePoint(b[j].X + Boffsetx, b[j].Y + Boffsety);
                    var b2 = new DoublePoint(b[j + 1].X + Boffsetx, b[j + 1].Y + Boffsety);

                    var prevbindex = (j == 0) ? b.Length - 1 : j - 1;
                    var prevaindex = (i == 0) ? a.Length - 1 : i - 1;
                    var nextbindex = (j + 1 == b.Length - 1) ? 0 : j + 2;
                    var nextaindex = (i + 1 == a.Length - 1) ? 0 : i + 2;

                    // go even further back if we happen to hit on a loop end point
                    if (b[prevbindex] == b[j] || (AlmostEqual(b[prevbindex].X, b[j].X) && AlmostEqual(b[prevbindex].Y, b[j].Y)))
                    {
                        prevbindex = (prevbindex == 0) ? b.Length - 1 : prevbindex - 1;
                    }

                    if (a[prevaindex] == a[i] || (AlmostEqual(a[prevaindex].X, a[i].X) && AlmostEqual(a[prevaindex].Y, a[i].Y)))
                    {
                        prevaindex = (prevaindex == 0) ? a.Length - 1 : prevaindex - 1;
                    }

                    // go even further forward if we happen to hit on a loop end point
                    if (b[nextbindex] == b[j + 1] || (AlmostEqual(b[nextbindex].X, b[j + 1].X) && AlmostEqual(b[nextbindex].Y, b[j + 1].Y)))
                    {
                        nextbindex = (nextbindex == b.Length - 1) ? 0 : nextbindex + 1;
                    }

                    if (a[nextaindex] == a[i + 1] || (AlmostEqual(a[nextaindex].X, a[i + 1].X) && AlmostEqual(a[nextaindex].Y, a[i + 1].Y)))
                    {
                        nextaindex = (nextaindex == a.Length - 1) ? 0 : nextaindex + 1;
                    }

                    var a0 = new DoublePoint(a[prevaindex].X + Aoffsetx, a[prevaindex].Y + Aoffsety);
                    var b0 = new DoublePoint(b[prevbindex].X + Boffsetx, b[prevbindex].Y + Boffsety);
                    var a3 = new DoublePoint(a[nextaindex].X + Aoffsetx, a[nextaindex].Y + Aoffsety);
                    var b3 = new DoublePoint(b[nextbindex].X + Boffsetx, b[nextbindex].Y + Boffsety);

                    if (OnSegment(a1, a2, b1) || (AlmostEqual(a1.X, b1.X) && AlmostEqual(a1.Y, b1.Y)))
                    {
                        // if a point is on a segment, it could intersect or it could not. Check via the neighboring points
                        var b0in = PointInPolygon(b0, a);
                        var b2in = PointInPolygon(b2, a);
                        if ((b0in == true && b2in == false) || (b0in == false && b2in == true))
                        {
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (OnSegment(a1, a2, b2) || (AlmostEqual(a2.X, b2.X) && AlmostEqual(a2.Y, b2.Y)))
                    {
                        // if a point is on a segment, it could intersect or it could not. Check via the neighboring points
                        var b1in = PointInPolygon(b1, a);
                        var b3in = PointInPolygon(b3, a);

                        if ((b1in == true && b3in == false) || (b1in == false && b3in == true))
                        {
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (OnSegment(b1, b2, a1) || (AlmostEqual(a1.X, b2.X) && AlmostEqual(a1.Y, b2.Y)))
                    {
                        // if a point is on a segment, it could intersect or it could not. Check via the neighboring points
                        var a0in = PointInPolygon(a0, b);
                        var a2in = PointInPolygon(a2, b);

                        if ((a0in == true && a2in == false) || (a0in == false && a2in == true))
                        {
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (OnSegment(b1, b2, a2) || (AlmostEqual(a2.X, b1.X) && AlmostEqual(a2.Y, b1.Y)))
                    {
                        // if a point is on a segment, it could intersect or it could not. Check via the neighboring points
                        var a1in = PointInPolygon(a1, b);
                        var a3in = PointInPolygon(a3, b);

                        if ((a1in == true && a3in == false) || (a1in == false && a3in == true))
                        {
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    var p = LineIntersect(b1, b2, a1, a2);

                    if (p != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // normalize vector into a unit vector
        private static DoublePoint NormalizeVector(DoublePoint v)
        {
            if (AlmostEqual(v.X * v.X + v.Y * v.Y, 1))
            {
                return v; // given vector was already a unit vector
            }
            var len = Math.Sqrt(v.X * v.X + v.Y * v.Y);
            var inverse = 1 / len;

            return new DoublePoint(v.X * inverse, y: v.Y * inverse);
        }

        private static double? PointDistance(DoublePoint p, DoublePoint s1, DoublePoint s2, DoublePoint normal, bool infinite = false)
        {
            normal = NormalizeVector(normal);

            var dir = new DoublePoint(normal.Y, -normal.X);

            var pdot = p.X * dir.X + p.Y * dir.Y;
            var s1dot = s1.X * dir.X + s1.Y * dir.Y;
            var s2dot = s2.X * dir.X + s2.Y * dir.Y;

            var pdotnorm = p.X * normal.X + p.Y * normal.Y;
            var s1dotnorm = s1.X * normal.X + s1.Y * normal.Y;
            var s2dotnorm = s2.X * normal.X + s2.Y * normal.Y;

            if (!infinite)
            {
                if (((pdot < s1dot || AlmostEqual(pdot, s1dot)) && (pdot < s2dot || AlmostEqual(pdot, s2dot))) || ((pdot > s1dot || AlmostEqual(pdot, s1dot)) && (pdot > s2dot || AlmostEqual(pdot, s2dot))))
                {
                    return null; // dot doesn't collide with segment, or lies directly on the vertex
                }
                if ((AlmostEqual(pdot, s1dot) && AlmostEqual(pdot, s2dot)) && (pdotnorm > s1dotnorm && pdotnorm > s2dotnorm))
                {
                    return Math.Min(pdotnorm - s1dotnorm, pdotnorm - s2dotnorm);
                }
                if ((AlmostEqual(pdot, s1dot) && AlmostEqual(pdot, s2dot)) && (pdotnorm < s1dotnorm && pdotnorm < s2dotnorm))
                {
                    return -Math.Min(s1dotnorm - pdotnorm, s2dotnorm - pdotnorm);
                }
            }

            return -(pdotnorm - s1dotnorm + (s1dotnorm - s2dotnorm) * (s1dot - pdot) / (s1dot - s2dot));
        }

        // project each point of B onto A in the given direction, and return the 
        private static double? PolygonProjectionDistance(PointsWithOffset A, PointsWithOffset B, DoublePoint direction)
        {
            var Boffsetx = B.OffsetX;
            var Boffsety = B.OffsetY;

            var Aoffsetx = A.OffsetX;
            var Aoffsety = A.OffsetY;

            A = A.Clone();
            B = B.Clone();
            var a = A.Points.ToList();
            var b = B.Points.ToList();


            // close the loop for polygons
            if (a[0] != a[a.Count - 1])
            {
                a.Add(a[0]);
            }

            if (b[0] != b[b.Count - 1])
            {
                b.Add(b[0]);
            }

            var edgeA = a;
            var edgeB = b;

            double? distance = null;

            for (var i = 0; i < edgeB.Count; i++)
            {
                // the shortest/most negative projection of B onto A
                double? minprojection = null;
                DoublePoint minp;
                for (var j = 0; j < edgeA.Count - 1; j++)
                {
                    var p = new DoublePoint(edgeB[i].X + Boffsetx, edgeB[i].Y + Boffsety);
                    var s1 = new DoublePoint(edgeA[j].X + Aoffsetx, edgeA[j].Y + Aoffsety);
                    var s2 = new DoublePoint(edgeA[j + 1].X + Aoffsetx, edgeA[j + 1].Y + Aoffsety);

                    if (Math.Abs((s2.Y - s1.Y) * direction.X - (s2.X - s1.X) * direction.Y) < TOL)
                    {
                        continue;
                    }

                    // project point, ignore edge boundaries
                    var d = PointDistance(p, s1, s2, direction);

                    if (d != null && (minprojection == null || d < minprojection))
                    {
                        minprojection = d;
                        minp = p;
                    }
                }
                if (minprojection != null && (distance == null || minprojection > distance))
                {
                    distance = minprojection;
                }
            }

            return distance;
        }

        // searches for an arrangement of A and B such that they do not overlap
        // if an NFP is given, only search for startpoints that have not already been traversed in the given NFP
        private static DoublePoint SearchStartPoint(PointsWithOffset A, PointsWithOffset B, bool inside, List<Path> NFP = null)
        {
            // clone arrays
            A = A.Clone();
            B = B.Clone();

            var a = A.Points.Select(p => new PointWithMark { X = p.X, Y = p.Y }).ToList();
            var b = B.Points.Select(p => new PointWithMark { X = p.X, Y = p.Y }).ToList();

            // close the loop for polygons
            if (a[0] != a[a.Count - 1])
            {
                a.Add(a[0]);
            }

            if (b[0] != b[b.Count - 1])
            {
                b.Add(b[0]);
            }

            for (var i = 0; i < a.Count - 1; i++)
            {
                if (!a[i].Marked)
                {
                    a[i].Marked = true;
                    for (var j = 0; j < b.Count; j++)
                    {
                        B.OffsetX = a[i].X - b[j].X;
                        B.OffsetY = a[i].Y - b[j].Y;

                        bool? Binside = null;
                        for (var k = 0; k < b.Count; k++)
                        {
                            var inpoly = PointInPolygon(new DoublePoint(b[k].X + B.OffsetX, b[k].Y + B.OffsetY),
                                a.Select(p => p as DoublePoint).ToArray());
                            if (inpoly != null)
                            {
                                Binside = inpoly;
                                break;
                            }
                        }

                        if (Binside == null)
                        { // A and B are the same
                            return null;
                        }

                        var startPoint = new DoublePoint(B.OffsetX, B.OffsetY);
                        A.Points = a.Select(p => p as DoublePoint).ToArray();
                        B.Points = b.Select(p => p as DoublePoint).ToArray();

                        if (((Binside.Value && inside) || (!Binside.Value && !inside)) && !Intersect(A, B) && !InNfp(startPoint, NFP))
                        {
                            return startPoint;
                        }

                        // slide B along vector
                        var vx = a[i + 1].X - a[i].X;
                        var vy = a[i + 1].Y - a[i].Y;

                        var d1 = PolygonProjectionDistance(A, B, new DoublePoint(vx, vy));
                        var d2 = PolygonProjectionDistance(B, A, new DoublePoint(-vx, -vy));

                        double? d = null;

                        // todo: clean this up
                        if (d1 == null && d2 == null)
                        {
                            // nothin
                        }
                        else if (d1 == null)
                        {
                            d = d2;
                        }
                        else if (d2 == null)
                        {
                            d = d1;
                        }
                        else
                        {
                            d = Math.Min(d1.Value, d2.Value);
                        }

                        // only slide until no longer negative
                        // todo: clean this up
                        if (d != null && !AlmostEqual(d.Value, 0) && d > 0)
                        {

                        }
                        else
                        {
                            continue;
                        }

                        var vd2 = vx * vx + vy * vy;

                        if (d * d < vd2 && !AlmostEqual(d.Value * d.Value, vd2))
                        {
                            var vd = Math.Sqrt(vx * vx + vy * vy);
                            vx *= d.Value / vd;
                            vy *= d.Value / vd;
                        }

                        B.OffsetX += vx;
                        B.OffsetY += vy;

                        for (var k = 0; k < b.Count; k++)
                        {
                            var inpoly = PointInPolygon(new DoublePoint(b[k].X + B.OffsetX, b[k].Y + B.OffsetY),
                                a.Select(p => p as DoublePoint).ToArray());
                            if (inpoly != null)
                            {
                                Binside = inpoly;
                                break;
                            }
                        }
                        startPoint = new DoublePoint(B.OffsetX, B.OffsetY);
                        if (((Binside.Value && inside) || (!Binside.Value && !inside)) && !Intersect(A, B) && !InNfp(startPoint, NFP))
                        {
                            return startPoint;
                        }
                    }
                }
            }

            return null;
        }

        private static bool InNfp(DoublePoint p, List<Path> nfp)
        {
            if (nfp == null || !nfp.Any())
            {
                return false;
            }

            for (var i = 0; i < nfp.Count; i++)
            {
                for (var j = 0; j < nfp[i].Count; j++)
                {
                    if (AlmostEqual(p.X, nfp[i][j].X) && AlmostEqual(p.Y, nfp[i][j].Y))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static double? SegmentDistance(DoublePoint A, DoublePoint B, DoublePoint E, DoublePoint F, DoublePoint direction)
        {
            var normal = new DoublePoint(direction.Y, -direction.X);

            var reverse = new DoublePoint(-direction.X, -direction.Y);

            var dotA = A.X * normal.X + A.Y * normal.Y;
            var dotB = B.X * normal.X + B.Y * normal.Y;
            var dotE = E.X * normal.X + E.Y * normal.Y;
            var dotF = F.X * normal.X + F.Y * normal.Y;

            var crossA = A.X * direction.X + A.Y * direction.Y;
            var crossB = B.X * direction.X + B.Y * direction.Y;
            var crossE = E.X * direction.X + E.Y * direction.Y;
            var crossF = F.X * direction.X + F.Y * direction.Y;

            var crossABmin = Math.Min(crossA, crossB);
            var crossABmax = Math.Max(crossA, crossB);

            var crossEFmax = Math.Max(crossE, crossF);
            var crossEFmin = Math.Min(crossE, crossF);

            var ABmin = Math.Min(dotA, dotB);
            var ABmax = Math.Max(dotA, dotB);

            var EFmax = Math.Max(dotE, dotF);
            var EFmin = Math.Min(dotE, dotF);

            // segments that will merely touch at one point
            if (AlmostEqual(ABmax, EFmin, TOL) || AlmostEqual(ABmin, EFmax, TOL))
            {
                return null;
            }
            // segments miss eachother completely
            if (ABmax < EFmin || ABmin > EFmax)
            {
                return null;
            }

            double overlap;

            if ((ABmax > EFmax && ABmin < EFmin) || (EFmax > ABmax && EFmin < ABmin))
            {
                overlap = 1;
            }

            else
            {
                var minMax = Math.Min(ABmax, EFmax);
                var maxMin = Math.Max(ABmin, EFmin);

                var maxMax = Math.Max(ABmax, EFmax);
                var minMin = Math.Min(ABmin, EFmin);

                overlap = (minMax - maxMin) / (maxMax - minMin);
            }

            var crossABE = (E.Y - A.Y) * (B.X - A.X) - (E.X - A.X) * (B.Y - A.Y);
            var crossABF = (F.Y - A.Y) * (B.X - A.X) - (F.X - A.X) * (B.Y - A.Y);

            // lines are colinear
            if (AlmostEqual(crossABE, 0) && AlmostEqual(crossABF, 0))
            {

                var ABnorm = new DoublePoint(B.Y - A.Y, A.X - B.X);
                var EFnorm = new DoublePoint(F.Y - E.Y, E.X - F.X);

                var ABnormlength = Math.Sqrt(ABnorm.X * ABnorm.X + ABnorm.Y * ABnorm.Y);
                ABnorm.X /= ABnormlength;
                ABnorm.Y /= ABnormlength;

                var EFnormlength = Math.Sqrt(EFnorm.X * EFnorm.X + EFnorm.Y * EFnorm.Y);
                EFnorm.X /= EFnormlength;
                EFnorm.Y /= EFnormlength;

                // segment normals must point in opposite directions
                if (Math.Abs(ABnorm.Y * EFnorm.X - ABnorm.X * EFnorm.Y) < TOL && ABnorm.Y * EFnorm.Y + ABnorm.X * EFnorm.X < 0)
                {
                    // normal of AB segment must point in same direction as given direction vector
                    var normdot = ABnorm.Y * direction.Y + ABnorm.X * direction.X;
                    // the segments merely slide along eachother
                    if (AlmostEqual(normdot, 0, TOL))
                    {
                        return null;
                    }
                    if (normdot < 0)
                    {
                        return 0;
                    }
                }
                return null;
            }

            var distances = new List<double>();

            // coincident points
            if (AlmostEqual(dotA, dotE))
            {
                distances.Add(crossA - crossE);
            }
            else if (AlmostEqual(dotA, dotF))
            {
                distances.Add(crossA - crossF);
            }
            else if (dotA > EFmin && dotA < EFmax)
            {
                var d = PointDistance(A, E, F, reverse);
                if (d != null && AlmostEqual(d.Value, 0))
                { //  A currently touches EF, but AB is moving away from EF
                    var dB = PointDistance(B, E, F, reverse, true);
                    if (dB == null || dB < 0 || AlmostEqual(dB.Value * overlap, 0))
                    {
                        d = null;
                    }
                }
                if (d != null)
                {
                    distances.Add(d.Value);
                }
            }

            if (AlmostEqual(dotB, dotE))
            {
                distances.Add(crossB - crossE);
            }
            else if (AlmostEqual(dotB, dotF))
            {
                distances.Add(crossB - crossF);
            }
            else if (dotB > EFmin && dotB < EFmax)
            {
                var d = PointDistance(B, E, F, reverse);

                if (d != null && AlmostEqual(d.Value, 0))
                { // crossA>crossB A currently touches EF, but AB is moving away from EF
                    var dA = PointDistance(A, E, F, reverse, true);
                    if (dA == null || dA < 0 || AlmostEqual(dA.Value * overlap, 0))
                    {
                        d = null;
                    }
                }
                if (d != null)
                {
                    distances.Add(d.Value);
                }
            }

            if (dotE > ABmin && dotE < ABmax)
            {
                var d = PointDistance(E, A, B, direction);
                if (d != null && AlmostEqual(d.Value, 0))
                { // crossF<crossE A currently touches EF, but AB is moving away from EF
                    var dF = PointDistance(F, A, B, direction, true);
                    if (dF == null || dF < 0 || AlmostEqual(dF.Value * overlap, 0))
                    {
                        d = null;
                    }
                }
                if (d != null)
                {
                    distances.Add(d.Value);
                }
            }

            if (dotF > ABmin && dotF < ABmax)
            {
                var d = PointDistance(F, A, B, direction);
                if (d != null && AlmostEqual(d.Value, 0))
                { // && crossE<crossF A currently touches EF, but AB is moving away from EF
                    var dE = PointDistance(E, A, B, direction, true);
                    if (dE == null || dE < 0 || AlmostEqual(dE.Value * overlap, 0))
                    {
                        d = null;
                    }
                }
                if (d != null)
                {
                    distances.Add(d.Value);
                }
            }

            if (!distances.Any())
            {
                return null;
            }

            return distances.Min();
        }

        private static double? PolygonSlideDistance(PointsWithOffset A, PointsWithOffset B, DoublePoint direction, bool ignoreNegative)
        {
            A = A.Clone();
            B = B.Clone();

            var a = A.Points.ToList();
            var b = B.Points.ToList();

            var Aoffsetx = A.OffsetX;
            var Aoffsety = A.OffsetY;
            var Boffsetx = B.OffsetX;
            var Boffsety = B.OffsetY;

            // close the loop for polygons
            if (a[0] != a[a.Count - 1])
            {
                a.Add(a[0]);
            }

            if (b[0] != b[b.Count - 1])
            {
                b.Add(b[0]);
            }

            var edgeA = a;
            var edgeB = b;

            double? distance = null;

            var dir = NormalizeVector(direction);

            var normal = new DoublePoint(dir.Y, -dir.X);

            var reverse = new DoublePoint(-dir.X, -dir.Y);

            for (var i = 0; i < edgeB.Count - 1; i++)
            {
                for (var j = 0; j < edgeA.Count - 1; j++)
                {
                    var A1 = new DoublePoint(edgeA[j].X + Aoffsetx, edgeA[j].Y + Aoffsety);
                    var A2 = new DoublePoint(edgeA[j + 1].X + Aoffsetx, y: edgeA[j + 1].Y + Aoffsety);
                    var B1 = new DoublePoint(edgeB[i].X + Boffsetx, edgeB[i].Y + Boffsety);
                    var B2 = new DoublePoint(edgeB[i + 1].X + Boffsetx, edgeB[i + 1].Y + Boffsety);

                    if ((AlmostEqual(A1.X, A2.X) && AlmostEqual(A1.Y, A2.Y)) || (AlmostEqual(B1.X, B2.X) && AlmostEqual(B1.Y, B2.Y)))
                    {
                        continue; // ignore extremely small lines
                    }

                    var d = SegmentDistance(A1, A2, B1, B2, dir);

                    if (d != null && (distance == null || d < distance))
                    {
                        if (!ignoreNegative || d > 0 || AlmostEqual(d.Value, 0))
                        {
                            distance = d;
                        }
                    }
                }
            }
            return distance;
        }

        // returns true if p lies on the line segment defined by AB, but not at any endpoints
        // may need work!
        private static bool OnSegment(DoublePoint A, DoublePoint B, DoublePoint p)
        {
            // vertical line
            if (AlmostEqual(A.X, B.X) && AlmostEqual(p.X, A.X))
            {
                if (!AlmostEqual(p.Y, B.Y) && !AlmostEqual(p.Y, A.Y) && p.Y < Math.Max(B.Y, A.Y) && p.Y > Math.Min(B.Y, A.Y))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // horizontal line
            if (AlmostEqual(A.Y, B.Y) && AlmostEqual(p.Y, A.Y))
            {
                if (!AlmostEqual(p.X, B.X) && !AlmostEqual(p.X, A.X) && p.X < Math.Max(B.X, A.X) && p.X > Math.Min(B.X, A.X))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            //range check
            if ((p.X < A.X && p.X < B.X) || (p.X > A.X && p.X > B.X) || (p.Y < A.Y && p.Y < B.Y) || (p.Y > A.Y && p.Y > B.Y))
            {
                return false;
            }


            // exclude end points
            if ((AlmostEqual(p.X, A.X) && AlmostEqual(p.Y, A.Y)) || (AlmostEqual(p.X, B.X) && AlmostEqual(p.Y, B.Y)))
            {
                return false;
            }

            var cross = (p.Y - A.Y) * (B.X - A.X) - (p.X - A.X) * (B.Y - A.Y);

            if (Math.Abs(cross) > TOL)
            {
                return false;
            }

            var dot = (p.X - A.X) * (B.X - A.X) + (p.Y - A.Y) * (B.Y - A.Y);



            if (dot < 0 || AlmostEqual(dot, 0))
            {
                return false;
            }

            var len2 = (B.X - A.X) * (B.X - A.X) + (B.Y - A.Y) * (B.Y - A.Y);



            if (dot > len2 || AlmostEqual(dot, len2))
            {
                return false;
            }

            return true;
        }

        public static double GetSegmentLength(DoublePoint A, DoublePoint B)
        {
            return Math.Sqrt(Math.Pow(B.X - A.X, 2) + Math.Pow(B.Y - A.Y, 2));
        }

        public static double GetSegmentLength(DoublePoint[] segment)
        {
            return GetSegmentLength(segment[1], segment[0]);
        }

        private static DoublePoint[] GetRectangle(PolygonBounds bounds)
        {
            var firstPoint = new DoublePoint(bounds.X, bounds.Y);
            var secondPoint = new DoublePoint(bounds.X + bounds.Width, bounds.Y);
            var thirdPoint = new DoublePoint(secondPoint.X, bounds.Y + bounds.Height);
            var forthPoint = new DoublePoint(bounds.X, thirdPoint.Y);

            return new DoublePoint[] { firstPoint, secondPoint, thirdPoint, forthPoint };
        }

        public static bool BoundsIntersect(PolygonBounds bounds1, PolygonBounds bounds2)
        {
            var rectangle1 = GetRectangle(bounds1);
            var rectangle2 = GetRectangle(bounds2);

            return rectangle1.Any(p => PointInPolygon(p, rectangle2) ?? true) ||
                rectangle2.Any(p => PointInPolygon(p, rectangle1) ?? true);
        }

        /// <summary>
        /// turn Bezier into line segments via de Casteljau, returns an array of points
        /// </summary>
        public static Path LinearizeQuadraticBezier(DoublePoint p1, DoublePoint p2, DoublePoint c1, double tolerance)
        {
            var finished = new Path { p1 }; // list of points to return
            var todo = new List<BezierSegment> { new BezierSegment { P1 = p1, P2 = p2, C1 = c1 } }; // list of Beziers to divide

            // recursion could stack overflow, loop instead
            while (todo.Count > 0)
            {
                var segment = todo[0];

                if (IsQuadraticBezierFlat(segment.P1, segment.P2, segment.C1, tolerance))
                {
                    // reached subdivision limit
                    finished.Add(new DoublePoint(segment.P2.X, segment.P2.Y));
                    todo.RemoveAt(0);
                }
                else
                {
                    var divided = SubdivideQuadraticBezier(segment.P1, segment.P2, segment.C1, 0.5);
                    todo.RemoveAt(0);
                    var newTodo = new List<BezierSegment>(divided);
                    newTodo.AddRange(todo);
                    todo = newTodo;


                    //todo = todo.Prepend(divided[1]).Prepend(divided[0]).ToList();

                    //todo.Prepend(divided[1]);
                    //todo;
                    //todo.splice(0, 1, divided[0], divided[1]);
                }
            }
            return finished;
        }

        // Roger Willcocks bezier flatness criterion
        private static bool IsQuadraticBezierFlat(DoublePoint p1, DoublePoint p2, DoublePoint c1, double tolerance)
        {
            tolerance = 4 * tolerance * tolerance;

            var ux = 2 * c1.X - p1.X - p2.X;
            ux *= ux;

            var uy = 2 * c1.Y - p1.Y - p2.Y;
            uy *= uy;

            return ux + uy <= tolerance;
        }

        // subdivide a single Bezier
        // t is the percent along the Bezier to divide at. eg. 0.5
        private static BezierSegment[] SubdivideQuadraticBezier(DoublePoint p1, DoublePoint p2, DoublePoint c1, double t)
        {
            var mid1 = new DoublePoint(
                    p1.X + (c1.X - p1.X) * t,
                    p1.Y + (c1.Y - p1.Y) * t);

            var mid2 = new DoublePoint(
                        c1.X + (p2.X - c1.X) * t,
                        c1.Y + (p2.Y - c1.Y) * t);

            var mid3 = new DoublePoint(
                        mid1.X + (mid2.X - mid1.X) * t,
                        mid1.Y + (mid2.Y - mid1.Y) * t);

            var seg1 = new BezierSegment { P1 = p1, P2 = mid3, C1 = mid1 };
            var seg2 = new BezierSegment { P1 = mid3, P2 = p2, C1 = mid2 };

            return new BezierSegment[] { seg1, seg2 };
        }

        public static double GetLineYVectorLength(DoublePoint A, DoublePoint B)
        {
            var lineLength = GetSegmentLength(A, B);
            var normalYVector = new DoublePoint[] { A, new DoublePoint(A.X, 1) };
            var normalYVectorLength = GetSegmentLength(normalYVector[0], normalYVector[1]);

            var dotProduct = B.X * normalYVector[1].X + B.Y * normalYVector[1].Y;
            var cosAngleBetween = dotProduct / (lineLength * normalYVectorLength);

            return lineLength * cosAngleBetween;
        }

        public static double GetSegmentVectorXAngle(DoublePoint A, DoublePoint B)
        {
            var deltaY = B.Y - A.Y;
            var deltaX = B.X - A.X;

            return Math.Atan2(deltaY, deltaX);
        }
    }

    public class BezierSegment
    {
        public DoublePoint P1 { get; set; }
        public DoublePoint P2 { get; set; }
        public DoublePoint C1 { get; set; }
    }
}

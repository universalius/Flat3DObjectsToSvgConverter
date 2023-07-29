using ClipperLib;
using System.ComponentModel;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Path = System.Collections.Generic.List<ClipperLib.DoublePoint>;


namespace SvgNest.Utils
{
    public static class GeometryUtil
    {
        // floating point comparison tolerance
        private static double TOL = Math.Pow(10, -9); // Floating point error is likely to be above 1 epsilon

        public static bool almostEqual(double a, double b, double? tolerance = null)
        {
            return Math.Abs(a - b) < (tolerance ?? TOL);
        }

        // returns the area of the polygon, assuming no self-intersections
        // a negative area indicates counter-clockwise winding direction
        public static double polygonArea(DoublePoint[] polygon)
        {
            var area = 0.0;
            for (int i = 0; i < polygon.Length; i++)
            {
                var j = i == 0 ? polygon.Length - 1 : i - 1;
                area += (polygon[j].X + polygon[i].X) * (polygon[j].Y - polygon[i].Y);
            }

            return 0.5 * area;
        }

        // return true if point is in the polygon, false if outside, and null if exactly on a point or edge
        public static bool? pointInPolygon(DoublePoint point, DoublePoint[] polygon)
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

                if (almostEqual(xi, point.X) && almostEqual(yi, point.Y))
                {
                    return null; // no result
                }

                if (_onSegment(new DoublePoint(xi, yi), new DoublePoint(xj, yj), point))
                {
                    return null; // exactly on the segment
                }

                if (almostEqual(xi, xj) && almostEqual(yi, yj))
                { // ignore very small lines
                    continue;
                }

                var intersect = ((yi > point.Y) != (yj > point.Y)) && (point.X < (xj - xi) * (point.Y - yi) / (yj - yi) + xi);
                if (intersect) inside = !inside;
            }

            return inside;
        }

        // returns the rectangular bounding box of the given polygon
        public static PolygonBounds getPolygonBounds(DoublePoint[] polygon)
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

        public static PolygonWithBounds rotatePolygon(DoublePoint[] polygon, double angle)
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
            var bounds = getPolygonBounds(rotated);
            return new PolygonWithBounds
            {
                Points = rotated,
                Bounds = bounds
            };
        }

        public static bool isRectangle(DoublePoint[] poly, double? tolerance)
        {
            var bb = getPolygonBounds(poly);
            tolerance = tolerance ?? TOL;

            for (var i = 0; i < poly.Length; i++)
            {
                if (!almostEqual(poly[i].X, bb.X) && !almostEqual(poly[i].X, bb.X + bb.Width))
                {
                    return false;
                }
                if (!almostEqual(poly[i].Y, bb.Y) && !almostEqual(poly[i].Y, bb.Y + bb.Height))
                {
                    return false;
                }
            }

            return true;
        }

        // returns an interior NFP for the special case where A is a rectangle
        public static List<Path> noFitPolygonRectangle(DoublePoint[] A, DoublePoint[] B)
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


        // returns the intersection of AB and EF
        // or null if there are no intersections or other numerical error
        // if the infinite flag is set, AE and EF describe infinite lines without endpoints, they are finite line segments otherwise
        public static DoublePoint lineIntersect(DoublePoint A, DoublePoint B, DoublePoint E, DoublePoint F, bool infinite = false)
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
        private static bool intersect(PointsWithOffset A, PointsWithOffset B)
        {
            var Aoffsetx = A.offsetx;
            var Aoffsety = A.offsety;

            var Boffsetx = B.offsetx;
            var Boffsety = B.offsety;

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
                    if (b[prevbindex] == b[j] || (almostEqual(b[prevbindex].X, b[j].X) && almostEqual(b[prevbindex].Y, b[j].Y)))
                    {
                        prevbindex = (prevbindex == 0) ? b.Length - 1 : prevbindex - 1;
                    }

                    if (a[prevaindex] == a[i] || (almostEqual(a[prevaindex].X, a[i].X) && almostEqual(a[prevaindex].Y, a[i].Y)))
                    {
                        prevaindex = (prevaindex == 0) ? a.Length - 1 : prevaindex - 1;
                    }

                    // go even further forward if we happen to hit on a loop end point
                    if (b[nextbindex] == b[j + 1] || (almostEqual(b[nextbindex].X, b[j + 1].X) && almostEqual(b[nextbindex].Y, b[j + 1].Y)))
                    {
                        nextbindex = (nextbindex == b.Length - 1) ? 0 : nextbindex + 1;
                    }

                    if (a[nextaindex] == a[i + 1] || (almostEqual(a[nextaindex].X, a[i + 1].X) && almostEqual(a[nextaindex].Y, a[i + 1].Y)))
                    {
                        nextaindex = (nextaindex == a.Length - 1) ? 0 : nextaindex + 1;
                    }

                    var a0 = new DoublePoint(a[prevaindex].X + Aoffsetx, a[prevaindex].Y + Aoffsety);
                    var b0 = new DoublePoint(b[prevbindex].X + Boffsetx, b[prevbindex].Y + Boffsety);
                    var a3 = new DoublePoint(a[nextaindex].X + Aoffsetx, a[nextaindex].Y + Aoffsety);
                    var b3 = new DoublePoint(b[nextbindex].X + Boffsetx, b[nextbindex].Y + Boffsety);

                    if (_onSegment(a1, a2, b1) || (almostEqual(a1.X, b1.X) && almostEqual(a1.Y, b1.Y)))
                    {
                        // if a point is on a segment, it could intersect or it could not. Check via the neighboring points
                        var b0in = pointInPolygon(b0, a);
                        var b2in = pointInPolygon(b2, a);
                        if ((b0in == true && b2in == false) || (b0in == false && b2in == true))
                        {
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (_onSegment(a1, a2, b2) || (almostEqual(a2.X, b2.X) && almostEqual(a2.Y, b2.Y)))
                    {
                        // if a point is on a segment, it could intersect or it could not. Check via the neighboring points
                        var b1in = pointInPolygon(b1, a);
                        var b3in = pointInPolygon(b3, a);

                        if ((b1in == true && b3in == false) || (b1in == false && b3in == true))
                        {
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (_onSegment(b1, b2, a1) || (almostEqual(a1.X, b2.X) && almostEqual(a1.Y, b2.Y)))
                    {
                        // if a point is on a segment, it could intersect or it could not. Check via the neighboring points
                        var a0in = pointInPolygon(a0, b);
                        var a2in = pointInPolygon(a2, b);

                        if ((a0in == true && a2in == false) || (a0in == false && a2in == true))
                        {
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (_onSegment(b1, b2, a2) || (almostEqual(a2.X, b1.X) && almostEqual(a2.Y, b1.Y)))
                    {
                        // if a point is on a segment, it could intersect or it could not. Check via the neighboring points
                        var a1in = pointInPolygon(a1, b);
                        var a3in = pointInPolygon(a3, b);

                        if ((a1in == true && a3in == false) || (a1in == false && a3in == true))
                        {
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    var p = lineIntersect(b1, b2, a1, a2);

                    if (p != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // normalize vector into a unit vector
        private static DoublePoint normalizeVector(DoublePoint v)
        {
            if (almostEqual(v.X * v.X + v.Y * v.Y, 1))
            {
                return v; // given vector was already a unit vector
            }
            var len = Math.Sqrt(v.X * v.X + v.Y * v.Y);
            var inverse = 1 / len;

            return new DoublePoint(v.X * inverse, y: v.Y * inverse);
        }

        private static double? pointDistance(DoublePoint p, DoublePoint s1, DoublePoint s2, DoublePoint normal, bool infinite = false)
        {
            normal = normalizeVector(normal);

            var dir = new DoublePoint(normal.Y, -normal.X);

            var pdot = p.X * dir.X + p.Y * dir.Y;
            var s1dot = s1.X * dir.X + s1.Y * dir.Y;
            var s2dot = s2.X * dir.X + s2.Y * dir.Y;

            var pdotnorm = p.X * normal.X + p.Y * normal.Y;
            var s1dotnorm = s1.X * normal.X + s1.Y * normal.Y;
            var s2dotnorm = s2.X * normal.X + s2.Y * normal.Y;

            if (!infinite)
            {
                if (((pdot < s1dot || almostEqual(pdot, s1dot)) && (pdot < s2dot || almostEqual(pdot, s2dot))) || ((pdot > s1dot || almostEqual(pdot, s1dot)) && (pdot > s2dot || almostEqual(pdot, s2dot))))
                {
                    return null; // dot doesn't collide with segment, or lies directly on the vertex
                }
                if ((almostEqual(pdot, s1dot) && almostEqual(pdot, s2dot)) && (pdotnorm > s1dotnorm && pdotnorm > s2dotnorm))
                {
                    return Math.Min(pdotnorm - s1dotnorm, pdotnorm - s2dotnorm);
                }
                if ((almostEqual(pdot, s1dot) && almostEqual(pdot, s2dot)) && (pdotnorm < s1dotnorm && pdotnorm < s2dotnorm))
                {
                    return -Math.Min(s1dotnorm - pdotnorm, s2dotnorm - pdotnorm);
                }
            }

            return -(pdotnorm - s1dotnorm + (s1dotnorm - s2dotnorm) * (s1dot - pdot) / (s1dot - s2dot));
        }

        // project each point of B onto A in the given direction, and return the 
        public static double? polygonProjectionDistance(PointsWithOffset A, PointsWithOffset B, DoublePoint direction)
        {
            var Boffsetx = B.offsetx;
            var Boffsety = B.offsety;

            var Aoffsetx = A.offsetx;
            var Aoffsety = A.offsety;

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
                    var d = pointDistance(p, s1, s2, direction);

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
        private static DoublePoint searchStartPoint(PointsWithOffset A, PointsWithOffset B, bool inside, bool? NFP = null)
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
                if (!a[i].marked)
                {
                    a[i].marked = true;
                    for (var j = 0; j < b.Count; j++)
                    {
                        B.offsetx = a[i].X - b[j].X;
                        B.offsety = a[i].Y - b[j].Y;

                        bool? Binside = null;
                        for (var k = 0; k < b.Count; k++)
                        {
                            var inpoly = pointInPolygon(new DoublePoint(b[k].X + B.offsetx, b[k].Y + B.offsety),
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

                        var startPoint = new DoublePoint(B.offsetx, B.offsety);
                        A.Points = a.Select(p => p as DoublePoint).ToArray();
                        B.Points = b.Select(p => p as DoublePoint).ToArray();

                        if (((Binside.Value && inside) || (!Binside.Value && !inside)) && !intersect(A, B) && !inNfp(startPoint, NFP))
                        {
                            return startPoint;
                        }

                        // slide B along vector
                        var vx = a[i + 1].X - a[i].X;
                        var vy = a[i + 1].Y - a[i].Y;

                        var d1 = polygonProjectionDistance(A, B, new DoublePoint(vx, vy));
                        var d2 = polygonProjectionDistance(B, A, new DoublePoint(-vx, -vy));

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
                        if (d != null && !almostEqual(d.Value, 0) && d > 0)
                        {

                        }
                        else
                        {
                            continue;
                        }

                        var vd2 = vx * vx + vy * vy;

                        if (d * d < vd2 && !almostEqual(d.Value * d.Value, vd2))
                        {
                            var vd = Math.Sqrt(vx * vx + vy * vy);
                            vx *= d.Value / vd;
                            vy *= d.Value / vd;
                        }

                        B.offsetx += vx;
                        B.offsety += vy;

                        for (var k = 0; k < b.Count; k++)
                        {
                            var inpoly = pointInPolygon(new DoublePoint(b[k].X + B.offsetx, b[k].Y + B.offsety),
                                a.Select(p => p as DoublePoint).ToArray());
                            if (inpoly != null)
                            {
                                Binside = inpoly;
                                break;
                            }
                        }
                        startPoint = new DoublePoint(B.offsetx, B.offsety);
                        if (((Binside.Value && inside) || (!Binside.Value && !inside)) && !intersect(A, B) && !inNfp(startPoint, NFP))
                        {
                            return startPoint;
                        }
                    }
                }
            }

            return null;
        }

        private static bool inNfp(DoublePoint p, bool? nfp)
        {
            //if (!nfp || nfp.Length == 0)
            //{
            //    return false;
            //}

            //for (var i = 0; i < nfp.Length; i++)
            //{
            //    for (var j = 0; j < nfp[i].Length; j++)
            //    {
            //        if (almostEqual(p.X, nfp[i][j].X) && almostEqual(p.Y, nfp[i][j].Y))
            //        {
            //            return true;
            //        }
            //    }
            //}

            return false;
        }


        public class PointsWithOffset
        {
            public DoublePoint[] Points { get; set; }
            public double offsetx { get; set; }
            public double offsety { get; set; }

            public PointsWithOffset Clone()
            {
                return new PointsWithOffset
                {
                    Points = Points.ToList().ToArray(),
                    offsetx = (double)offsetx,
                    offsety = (double)offsety
                };
            }
        }

        class PointWithMark : DoublePoint
        {
            public bool marked { get; set; }
        }

        // given a static polygon A and a movable polygon B, compute a no fit polygon by orbiting B about A
        // if the inside flag is set, B is orbited inside of A rather than outside
        // if the searchEdges flag is set, all edges of A are explored for NFPs - multiple 
        public static List<Path> noFitPolygon(DoublePoint[] poly1, DoublePoint[] poly2, bool inside, bool searchEdges)
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
                a[i].marked = false;
                if (a[i].Y < minA)
                {
                    minA = a[i].Y;
                    minAindex = i;
                }
            }

            for (var i = 1; i < b.Length; i++)
            {
                b[i].marked = false;
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
                startpoint = searchStartPoint(A, B, true);
            }

            var NFPlist = [];

            while (startpoint != null)
            {

                B.offsetx = startpoint.X;
                B.offsety = startpoint.Y;

                // maintain a list of touching points/edges
                var touching = new List<Touching>();

                var prevvector = null; // keep track of previous vector
                var NFP = new Path { new DoublePoint(b[0].X + B.offsetx, b[0].Y + B.offsety) };

                var referencex = b[0].X + B.offsetx;
                var referencey = b[0].Y + B.offsety;
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
                            if (almostEqual(a[i].X, b[j].X + B.offsetx) && almostEqual(a[i].Y, b[j].Y + B.offsety))
                            {
                                touching.Add(new Touching { type = 0, A = i, B = j });
                            }

                            else if (_onSegment(a[i], a[nexti], new DoublePoint(b[j].X + B.offsetx, b[j].Y + B.offsety)))
                            {
                                touching.Add(new Touching { type = 1, A = nexti, B = j });
                            }

                            else if (_onSegment(new DoublePoint(b[j].X + B.offsetx, y: b[j].Y + B.offsety),
                                new DoublePoint(b[nextj].X + B.offsetx, b[nextj].Y + B.offsety), a[i]))
                            {
                                touching.Add(new Touching { type = 2, A = i, B = nextj });
                            }
                        }
                    }

                    // generate translation vectors from touching vertices/edges
                    var vectors = new List<Vector>();
                    for (var i = 0; i < touching.Count; i++)
                    {
                        var vertexA = a[touching[i].A];
                        vertexA.marked = true;

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

                        if (touching[i].type == 0)
                        {

                            var vA1 = new Vector
                            {
                                X = prevA.X - vertexA.X,
                                Y = prevA.Y - vertexA.Y,
                                start = vertexA,
                                end = prevA
                            };

                            var vA2 = new Vector
                            {
                                X = nextA.X - vertexA.X,
                                Y = nextA.Y - vertexA.Y,
                                start = vertexA,
                                end = nextA
                            };

                            // B vectors need to be inverted
                            var vB1 = new Vector
                            {
                                X = vertexB.X - prevB.X,
                                Y = vertexB.Y - prevB.Y,
                                start = prevB,
                                end = vertexB
                            };

                            var vB2 = new Vector
                            {
                                X = vertexB.X - nextB.X,
                                Y = vertexB.Y - nextB.Y,
                                start = nextB,
                                end = vertexB
                            };

                            vectors.Add(vA1);
                            vectors.Add(vA2);
                            vectors.Add(vB1);
                            vectors.Add(vB2);
                        }

                        else if (touching[i].type == 1)
                        {
                            vectors.Add(new Vector
                            {
                                X = vertexA.X - (vertexB.X + B.offsetx),
                                Y = vertexA.Y - (vertexB.Y + B.offsety),
                                start = prevA,
                                end = vertexA

                            });

                            vectors.Add(new Vector
                            {
                                X = prevA.X - (vertexB.X + B.offsetx),
                                Y = prevA.Y - (vertexB.Y + B.offsety),
                                start = vertexA,
                                end = prevA
                            });
                        }
                        else if (touching[i].type == 2)
                        {
                            vectors.Add(new Vector
                            {
                                X = vertexA.X - (vertexB.X + B.offsetx),
                                Y = vertexA.Y - (vertexB.Y + B.offsety),
                                start = prevB,
                                end = vertexB
                            });

                            vectors.Add(new Vector
                            {
                                X = vertexA.X - (prevB.X + B.offsetx),
                                Y = vertexA.Y - (prevB.Y + B.offsety),
                                start = vertexB,
                                end = prevB
                            });
                        }
                    }

                    // todo: there should be a faster way to reject vectors that will cause immediate intersection. For now just check them all

                    var translate = null;
                    var maxd = 0;

                    for (var i = 0; i < vectors.Count; i++)
                    {
                        if (vectors[i].X == 0 && vectors[i].Y == 0)
                        {
                            continue;
                        }

                        // if this vector points us back to where we came from, ignore it.
                        // ie cross product = 0, dot product < 0
                        if (prevvector && vectors[i].Y * prevvector.Y + vectors[i].X * prevvector.X < 0)
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

                        var d = polygonSlideDistance(A, B, vectors[i], true);
                        var vecd2 = vectors[i].X * vectors[i].X + vectors[i].Y * vectors[i].Y;

                        if (d == null || d * d > vecd2)
                        {
                            var vecd = Math.Sqrt(vectors[i].X * vectors[i].X + vectors[i].Y * vectors[i].Y);
                            d = vecd;
                        }

                        if (d != null && d > maxd)
                        {
                            maxd = d;
                            translate = vectors[i];
                        }
                    }


                    if (translate == null || almostEqual(maxd, 0))
                    {
                        // didn't close the loop, something went wrong here
                        NFP = null;
                        break;
                    }

                    translate.start.marked = true;
                    translate.end.marked = true;

                    prevvector = translate;

                    // trim
                    var vlength2 = translate.X * translate.X + translate.Y * translate.Y;
                    if (maxd * maxd < vlength2 && !almostEqual(maxd * maxd, vlength2))
                    {
                        var scale = Math.Sqrt((maxd * maxd) / vlength2);
                        translate.X *= scale;
                        translate.Y *= scale;
                    }

                    referencex += translate.X;
                    referencey += translate.Y;

                    if (almostEqual(referencex, startx) && almostEqual(referencey, starty))
                    {
                        // we've made a full loop
                        break;
                    }

                    // if A and B start on a touching horizontal line, the end point may not be the start point
                    var looped = false;
                    if (NFP.Length > 0)
                    {
                        for (i = 0; i < NFP.Length - 1; i++)
                        {
                            if (almostEqual(referencex, NFP[i].X) && almostEqual(referencey, NFP[i].Y))
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

                    NFP.push({
                    x: referencex,
						y: referencey
        

                    });

                    B.offsetx += translate.X;
                    B.offsety += translate.Y;

                    counter++;
                }

                if (NFP && NFP.Length > 0)
                {
                    NFPlist.push(NFP);
                }

                if (!searchEdges)
                {
                    // only get outer NFP or first inner NFP
                    break;
                }

                startpoint = searchStartPoint(A, B, inside, NFPlist);

            }

            return NFPlist;
        },


        // returns true if p lies on the line segment defined by AB, but not at any endpoints
        // may need work!
        private static bool _onSegment(DoublePoint A, DoublePoint B, DoublePoint p)
        {
            // vertical line
            if (almostEqual(A.X, B.X) && almostEqual(p.X, A.X))
            {
                if (!almostEqual(p.Y, B.Y) && !almostEqual(p.Y, A.Y) && p.Y < Math.Max(B.Y, A.Y) && p.Y > Math.Min(B.Y, A.Y))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // horizontal line
            if (almostEqual(A.Y, B.Y) && almostEqual(p.Y, A.Y))
            {
                if (!almostEqual(p.X, B.X) && !almostEqual(p.X, A.X) && p.X < Math.Max(B.X, A.X) && p.X > Math.Min(B.X, A.X))
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
            if ((almostEqual(p.X, A.X) && almostEqual(p.Y, A.Y)) || (almostEqual(p.X, B.X) && almostEqual(p.Y, B.Y)))
            {
                return false;
            }

            var cross = (p.Y - A.Y) * (B.X - A.X) - (p.X - A.X) * (B.Y - A.Y);

            if (Math.Abs(cross) > TOL)
            {
                return false;
            }

            var dot = (p.X - A.X) * (B.X - A.X) + (p.Y - A.Y) * (B.Y - A.Y);



            if (dot < 0 || almostEqual(dot, 0))
            {
                return false;
            }

            var len2 = (B.X - A.X) * (B.X - A.X) + (B.Y - A.Y) * (B.Y - A.Y);



            if (dot > len2 || almostEqual(dot, len2))
            {
                return false;
            }

            return true;
        }


    }

    public class PolygonWithBounds
    {
        public int id { get; set; }
        public DoublePoint[] Points { get; set; }

        public PolygonBounds Bounds { get; set; }
    }


    public class PolygonBounds : DoublePoint
    {
        public double Width { get; set; }
        public double Height { get; set; }
    }

    class Touching
    {
        public int type { get; set; }
        public int A { get; set; }
        public int B { get; set; }
    }

    class Vector : DoublePoint
    {
        public DoublePoint start { get; set; }
        public DoublePoint end { get; set; }
    }

}

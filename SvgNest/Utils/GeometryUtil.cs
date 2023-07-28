using ClipperLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
}

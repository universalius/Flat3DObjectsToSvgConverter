using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SvgNest.Utils
{
    public class Matrix
    {
        public List<IEnumerable<double>> queue;
        public double[] cache;

        public Matrix()
        {
            queue = new List<IEnumerable<double>>();
        }


        //// matrix utility from SvgPath
        //// https://github.com/fontello/svgpath

        //function Matrix()
        //{
        //    if (!(this instanceof Matrix)) { return new Matrix(); }
        //    queue = [];   // list of matrixes to apply
        //    cache = null; // combined matrix cache
        //}
        ////(typeof window != 'undefined' ? window : self).Matrix = Matrix;

        //// combine 2 matrixes
        //// m1, m2 - [a, b, c, d, e, g]
        ////
        //public  combine(m1, m2)
        //{
        //    return [
        //      m1[0] * m2[0] + m1[2] * m2[1],
        //      m1[1] * m2[0] + m1[3] * m2[1],
        //      m1[0] * m2[2] + m1[2] * m2[3],
        //      m1[1] * m2[2] + m1[3] * m2[3],
        //      m1[0] * m2[4] + m1[2] * m2[5] + m1[4],
        //      m1[1] * m2[4] + m1[3] * m2[5] + m1[5]
        //    ];
        //}

        // combine 2 matrixes
        // m1, m2 - [a, b, c, d, e, g]
        public double[] combine(double[] m1, double[] m2)
        {
            return new[] {
              m1[0] * m2[0] + m1[2] * m2[1],
              m1[1] * m2[0] + m1[3] * m2[1],
              m1[0] * m2[2] + m1[2] * m2[3],
              m1[1] * m2[2] + m1[3] * m2[3],
              m1[0] * m2[4] + m1[2] * m2[5] + m1[4],
              m1[1] * m2[4] + m1[3] * m2[5] + m1[5]
            };
        }

        //public  isIdentity()
        //{
        //    if (!cache)
        //    {
        //        cache = toArray();
        //    }

        //    var m = cache;

        //    if (m[0]== 1 && m[1]== 0 && m[2]== 0 && m[3]== 1 && m[4]== 0 && m[5]== 0)
        //    {
        //        return true;
        //    }
        //    return false;
        //}        

        public bool isIdentity()
        {
            if (cache == null)
            {
                cache = toArray();
            }

            var m = cache;

            if (m[0] == 1 && m[1] == 0 && m[2] == 0 && m[3] == 1 && m[4] == 0 && m[5] == 0)
            {
                return true;
            }
            return false;
        }

        //public  matrix(m)
        //{
        //    if (m[0]== 1 && m[1]== 0 && m[2]== 0 && m[3]== 1 && m[4]== 0 && m[5]== 0)
        //    {
        //        return this;
        //    }
        //    cache = null;
        //    queue.push(m);
        //    return this;
        //};

        public IEnumerable<double> matrix(double[] m)
        {
            if (m[0] == 1 && m[1] == 0 && m[2] == 0 && m[3] == 1 && m[4] == 0 && m[5] == 0)
            {
                return m;
            }
            cache = null;
            queue.Add(m);
            return m;
        }


        //public  translate(tx, ty)
        //{
        //    if (tx != 0 || ty != 0)
        //    {
        //        cache = null;
        //        queue.push([1, 0, 0, 1, tx, ty]);
        //    }
        //    return this;
        //};

        public void translate(double tx, double ty)
        {
            if (tx != 0 || ty != 0)
            {
                cache = null;
                queue.Add(new[] { 1, 0, 0, 1, tx, ty });
            }
        }


        public void scale(double sx, double sy)
        {
            if (sx != 1 || sy != 1)
            {
                cache = null;
                queue.Add(new[] { sx, 0, 0, sy, 0, 0 });
            }
        }


        public void rotate(double angle, double rx, double ry)
        {
            double rad, cos, sin;

            if (angle != 0)
            {
                translate(rx, ry);

                rad = angle * Math.PI / 180;
                cos = Math.Cos(rad);
                sin = Math.Sin(rad);

                queue.Add(new[] { cos, sin, -sin, cos, 0, 0 });
                cache = null;

                translate(-rx, -ry);
            }
        }


        //public  skewX(angle)
        //{
        //    if (angle != 0)
        //    {
        //        cache = null;
        //        queue.push([1, 0, Math.tan(angle * Math.PI / 180), 1, 0, 0]);
        //    }
        //    return this;
        //};


        //public  skewY(angle)
        //{
        //    if (angle != 0)
        //    {
        //        cache = null;
        //        queue.push([1, Math.tan(angle * Math.PI / 180), 0, 1, 0, 0]);
        //    }
        //    return this;
        //};

        public void skewX(double angle)
        {
            if (angle != 0)
            {
                cache = null;
                queue.Add(new[] { 1, 0, Math.Tan(angle * Math.PI / 180), 1, 0, 0 });
            }
        }


        public void skewY(double angle)
        {
            if (angle != 0)
            {
                cache = null;
                queue.Add(new[] { 1, Math.Tan(angle * Math.PI / 180), 0, 1, 0, 0 });
            }
        }


        //// Flatten queue
        ////
        //public  toArray()
        //{
        //    if (cache)
        //    {
        //        return cache;
        //    }

        //    if (!queue.Count())
        //    {
        //        cache = [1, 0, 0, 1, 0, 0];
        //        return cache;
        //    }

        //    cache = queue[0];

        //    if (queue.Count()== 1)
        //    {
        //        return cache;
        //    }

        //    for (var i = 1; i < queue.Count(); i++)
        //    {
        //        cache = combine(cache, queue[i]);
        //    }

        //    return cache;
        //};

        // Flatten queue
        public double[] toArray()
        {
            if (cache != null)
            {
                return cache;
            }

            if (!queue.Any())
            {
                cache = new[] { 1.0, 0, 0, 1, 0, 0 };
                return cache;
            }

            cache = queue[0].ToArray();

            if (queue.Count() == 1)
            {
                return cache;
            }

            for (var i = 1; i < queue.Count(); i++)
            {
                cache = combine(cache, queue[i].ToArray());
            }

            return cache;
        }

        //// Apply list of matrixes to (x,y) point.
        //// If `isRelative` set, `translate` component of matrix will be skipped
        ////
        //public  calc(x, y, isRelative)
        //{
        //    var m, i, len;

        //    // Don't change point on empty transforms queue
        //    if (!queue.Count()) { return [x, y]; }

        //    // Calculate final matrix, if not exists
        //    //
        //    // NB. if you deside to apply transforms to point one-by-one,
        //    // they should be taken in reverse order

        //    if (!cache)
        //    {
        //        cache = toArray();
        //    }

        //    m = cache;

        //    // Apply matrix to point
        //    return [
        //      x * m[0] + y * m[2] + (isRelative ? 0 : m[4]),
        //      x * m[1] + y * m[3] + (isRelative ? 0 : m[5])
        //    ];
        //};

        // Apply list of matrixes to (x,y) point.
        // If `isRelative` set, `translate` component of matrix will be skipped
        public double[] calc(double x, double y, bool isRelative)
        {
            double[] m;

            // Don't change point on empty transforms queue
            if (!queue.Any()) { return new double[] { x, y }; }

            // Calculate final matrix, if not exists
            //
            // NB. if you deside to apply transforms to point one-by-one,
            // they should be taken in reverse order

            if (cache != null)
            {
                cache = toArray();
            }

            m = cache;

            // Apply matrix to point
            return new double[] {
              x * m[0] + y * m[2] + (isRelative ? 0 : m[4]),
              x * m[1] + y * m[3] + (isRelative ? 0 : m[5])
            };
        }

        //module.exports.Matrix = Matrix;

    }
}

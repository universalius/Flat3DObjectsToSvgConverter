namespace SvgNest.Utils
{
    public class Matrix
    {
        public List<IEnumerable<double>> _queue;
        public double[] _cache;

        // matrix utility from SvgPath
        // https://github.com/fontello/svgpath
        public Matrix()
        {
            _queue = new List<IEnumerable<double>>();
        }

        // combine 2 matrixes
        // m1, m2 - [a, b, c, d, e, g]
        public double[] Combine(double[] m1, double[] m2)
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
     
        public bool IsIdentity()
        {
            if (_cache == null)
            {
                _cache = ToArray();
            }

            var m = _cache;

            if (m[0] == 1 && m[1] == 0 && m[2] == 0 && m[3] == 1 && m[4] == 0 && m[5] == 0)
            {
                return true;
            }
            return false;
        }

        public IEnumerable<double> Create(double[] m)
        {
            if (m[0] == 1 && m[1] == 0 && m[2] == 0 && m[3] == 1 && m[4] == 0 && m[5] == 0)
            {
                return m;
            }
            _cache = null;
            _queue.Add(m);
            return m;
        }

        public void Translate(double tx, double ty)
        {
            if (tx != 0 || ty != 0)
            {
                _cache = null;
                _queue.Add(new[] { 1, 0, 0, 1, tx, ty });
            }
        }

        public void Scale(double sx, double sy)
        {
            if (sx != 1 || sy != 1)
            {
                _cache = null;
                _queue.Add(new[] { sx, 0, 0, sy, 0, 0 });
            }
        }

        public void Rotate(double angle, double rx, double ry)
        {
            double rad, cos, sin;

            if (angle != 0)
            {
                Translate(rx, ry);

                rad = angle * Math.PI / 180;
                cos = Math.Cos(rad);
                sin = Math.Sin(rad);

                _queue.Add(new[] { cos, sin, -sin, cos, 0, 0 });
                _cache = null;

                Translate(-rx, -ry);
            }
        }

        public void SkewX(double angle)
        {
            if (angle != 0)
            {
                _cache = null;
                _queue.Add(new[] { 1, 0, Math.Tan(angle * Math.PI / 180), 1, 0, 0 });
            }
        }

        public void SkewY(double angle)
        {
            if (angle != 0)
            {
                _cache = null;
                _queue.Add(new[] { 1, Math.Tan(angle * Math.PI / 180), 0, 1, 0, 0 });
            }
        }

        // Flatten queue
        public double[] ToArray()
        {
            if (_cache != null)
            {
                return _cache;
            }

            if (!_queue.Any())
            {
                _cache = new[] { 1.0, 0, 0, 1, 0, 0 };
                return _cache;
            }

            _cache = _queue[0].ToArray();

            if (_queue.Count() == 1)
            {
                return _cache;
            }

            for (var i = 1; i < _queue.Count(); i++)
            {
                _cache = Combine(_cache, _queue[i].ToArray());
            }

            return _cache;
        }

        // Apply list of matrixes to (x,y) point.
        // If `isRelative` set, `translate` component of matrix will be skipped
        public double[] Calc(double x, double y, bool isRelative)
        {
            double[] m;

            // Don't change point on empty transforms queue
            if (!_queue.Any()) { return new double[] { x, y }; }

            // Calculate final matrix, if not exists
            //
            // NB. if you deside to apply transforms to point one-by-one,
            // they should be taken in reverse order

            if (_cache != null)
            {
                _cache = ToArray();
            }

            m = _cache;

            // Apply matrix to point
            return new double[] {
              x * m[0] + y * m[2] + (isRelative ? 0 : m[4]),
              x * m[1] + y * m[3] + (isRelative ? 0 : m[5])
            };
        }
    }
}

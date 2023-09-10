using SvgNest.Utils;
using System.Globalization;

namespace SvgNest.Models.SVGPathSeg
{
    public class SVGPathSeg
    {
        public const int PATHSEG_UNKNOWN = 0;
        public const int PATHSEG_CLOSEPATH = 1;
        public const int PATHSEG_MOVETO_ABS = 2;
        public const int PATHSEG_MOVETO_REL = 3;
        public const int PATHSEG_LINETO_ABS = 4;
        public const int PATHSEG_LINETO_REL = 5;
        public const int PATHSEG_CURVETO_CUBIC_ABS = 6;
        public const int PATHSEG_CURVETO_CUBIC_REL = 7;
        public const int PATHSEG_CURVETO_QUADRATIC_ABS = 8;
        public const int PATHSEG_CURVETO_QUADRATIC_REL = 9;
        public const int PATHSEG_ARC_ABS = 10;
        public const int PATHSEG_ARC_REL = 11;
        public const int PATHSEG_LINETO_HORIZONTAL_ABS = 12;
        public const int PATHSEG_LINETO_HORIZONTAL_REL = 13;
        public const int PATHSEG_LINETO_VERTICAL_ABS = 14;
        public const int PATHSEG_LINETO_VERTICAL_REL = 15;
        public const int PATHSEG_CURVETO_CUBIC_SMOOTH_ABS = 16;
        public const int PATHSEG_CURVETO_CUBIC_SMOOTH_REL = 17;
        public const int PATHSEG_CURVETO_QUADRATIC_SMOOTH_ABS = 18;
        public const int PATHSEG_CURVETO_QUADRATIC_SMOOTH_REL = 19;

        public int _pathSegType;
        public SVGPathSegList _owningPathSegList;

        protected CultureInfo _culture = new CultureInfo("en-US", false);

        public SVGPathSeg(int type, string typeAsLetter, SVGPathSegList owningPathSegList)
        {
            _pathSegType = type;
            PathSegTypeAsLetter = typeAsLetter;
            _owningPathSegList = owningPathSegList;
        }

        public string PathSegTypeAsLetter { get; set; }

        public double? X { get; set; }

        public double? Y { get; set; }

        public virtual SVGPathSeg Clone()
        {
            return new SVGPathSeg(_pathSegType, PathSegTypeAsLetter, null)
            {
                X = this.X,
                Y = this.Y
            };
        }

        public virtual string _asPathString()
        {
            var coords = "";
            if (X.HasValue || Y.HasValue)
            {
                coords = X.HasValue && Y.HasValue ? $"{X?.ToString(_culture)} {Y?.ToString(_culture)}" : $"{X?.ToString(_culture)}{Y?.ToString(_culture)}";
                coords = $" {coords}";
            }

            return $"{PathSegTypeAsLetter}{coords}";
        }
    }

    public class SVGPathSegCurvetoQuadratic : SVGPathSeg
    {
        public SVGPathSegCurvetoQuadratic(int type, string typeAsLetter, SVGPathSegList owningPathSegList) : base(type, typeAsLetter, owningPathSegList)
        {
        }
        public double X1 { get; set; }

        public double Y1 { get; set; }

        public override SVGPathSeg Clone()
        {
            return new SVGPathSegCurvetoQuadratic(_pathSegType, PathSegTypeAsLetter, null)
            {
                X = this.X,
                Y = this.Y,
                X1 = this.X1,
                Y1 = this.Y1
            };
        }

        public override string _asPathString()
        {
            return $"{PathSegTypeAsLetter} {X1.ToString(_culture)} {Y1.ToString(_culture)} {X?.ToString(_culture)} {Y?.ToString(_culture)}";
        }
    }
}

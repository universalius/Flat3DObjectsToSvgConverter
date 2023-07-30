using ClipperLib;
using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using DPath = System.Collections.Generic.List<ClipperLib.DoublePoint>;
using SvgNest.Constants;
using System.Linq;

namespace SvgNest.Helpers
{
    public class ClipperHelper
    {
        // converts a polygon from normal float coordinates to integer coordinates used by clipper, as well as x/y -> X/Y
        public static Path ToClipperCoordinates(DoublePoint[] polygon)
        {
            return new Path(
                polygon.Select(p =>
                    new IntPoint(Math.Round(p.X * SvgNestConstants.ClipperScale), Math.Round(p.Y * SvgNestConstants.ClipperScale))).ToList());
        }

        public static DoublePoint[] ToSvgNestCoordinates(Path polygon)
        {
            var normal = polygon.Select(p => new DoublePoint(p.X / SvgNestConstants.ClipperScale, p.Y / SvgNestConstants.ClipperScale));
            return normal.ToArray();
        }
    }
}

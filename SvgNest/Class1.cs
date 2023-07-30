using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using static SvgNest.PlacementWorker;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SvgNest
{
    internal class Class1
    {

    private void minkowskiDifference(RotatedPolygons A, RotatedPolygons B)
        {
            var Acopy = A.points.Select(p=> new DoublePoint(p.X, p.Y)).ToArray(); 
            var APath = Clipper.ScaleUpPath(Acopy, 10000000);
            var BCopy = B.points.Select(p => new DoublePoint(p.X, p.Y)).ToArray();
            var BPath = Clipper.ScaleUpPath(BCopy, 10000000);

            for (var i = 0; i < BPath.Length; i++)
            {
                BPath[i].X *= -1;
                BPath[i].Y *= -1;
            }
            var solution = Clipper.MinkowskiSum(APath, BPath, true);
            var clipperNfp;

            var largestArea = null;
            for (i = 0; i < solution.Length; i++)
            {
                var n = toNestCoordinates(solution[i], 10000000);
                var sarea = GeometryUtil.polygonArea(n);
                if (largestArea == null || largestArea > sarea)
                {
                    clipperNfp = n;
                    largestArea = sarea;
                }
            }

            for (var i = 0; i < clipperNfp.Length; i++)
            {
                clipperNfp[i].X += B[0].X;
                clipperNfp[i].Y += B[0].Y;
            }

            return [clipperNfp];
        }

        return { key: pair.key, value: nfp
    };
})


        }

    }
}

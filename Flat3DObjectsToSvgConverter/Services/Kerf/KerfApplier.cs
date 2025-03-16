using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models;
using GeometRi;
using Microsoft.Extensions.Options;

namespace Flat3DObjectsToSvgConverter.Services.Kerf;

public class KerfApplier(IOptions<KerfSettings> options)
{
    public void ApplyKerf(IEnumerable<MeshObjects> meshes)
    {
        var config = options.Value;

        if (!config.Enabled)
            return;

        meshes.ToList().ForEach(mesh =>
        {
            mesh.Objects.ToList().ForEach(obj =>
            {
                var mainLoop = obj.Loops.First();
                var segments = mainLoop.ToSegments();
                var points = mainLoop.Points.ToArray();

                var kerfSegments = segments.Select(s =>
                {
                    var kerfSegment = new KerfSegment(s);

                    var p1 = s.P1;
                    var p2 = s.P2;
                    var vector = new Vector3d(p1, p2);

                    var tolerance = 0.1;
                    var xSame = Math.Abs(p1.X - p2.X) <= 0.1;
                    var ySame = Math.Abs(p1.Y - p2.Y) <= 0.1;

                    if (xSame)
                    {
                        var shift = (float)(config.Y * vector.Y);
                        kerfSegment.ShiftedSegment = new Segment3d(
                            new Point3d(p1.X - shift, p1.Y, 0),
                            new Point3d(p2.X - shift, p2.Y, 0));
                    }

                    if (ySame)
                    {
                        var shift = (float)(config.X * vector.X);
                        kerfSegment.ShiftedSegment = new Segment3d(
                            new Point3d(p1.X, p1.Y + shift, 0),
                            new Point3d(p2.X, p2.Y + shift, 0));
                    }

                    if (!(xSame || ySame))
                    {
                        var shiftVector = vector.OrthogonalVector.Normalized.Mult(-config.XY);
                        kerfSegment.ShiftedSegment = s.Translate(shiftVector);
                    }

                    return kerfSegment;
                }).ToArray();

                var length = kerfSegments.Length;
                for (var i = 0; i < length; i++)
                {
                    var last = length - 1;
                    var next = i + 1;

                    if (i == last)
                    {
                        break;
                    }

                    var line1 = kerfSegments[i].ShiftedSegment.ToLine;
                    var line2 = kerfSegments[next].ShiftedSegment.ToLine;

                    var crossPoint = line1.IntersectionWith(line2) as Point3d;

                    if (i == 0)
                    {
                        line2 = kerfSegments[last].ShiftedSegment.ToLine;
                        var startPoint = line1.IntersectionWith(line2) as Point3d;
                        kerfSegments[0].SegmentWithKerf = new Segment3d(startPoint, crossPoint);
                        kerfSegments[1].SegmentWithKerf = new Segment3d(crossPoint, new Point3d());
                        kerfSegments[last].SegmentWithKerf = new Segment3d(new Point3d(), startPoint);
                        continue;
                    }

                    kerfSegments[i].SegmentWithKerf.P2 = crossPoint;

                    if (next != last)
                    {
                        kerfSegments[next].SegmentWithKerf = new Segment3d(crossPoint, new Point3d());
                    }
                    else
                    {
                        kerfSegments[last].SegmentWithKerf.P1 = crossPoint;
                    }
                }

                //mainLoop.Points = kerfPoints.Select(kp => kp.NewPoint.Value).ToArray();

                mainLoop.Points = kerfSegments.Select(ks => ks.SegmentWithKerf).ToArray().ToPointFs();
            });
        });

        Console.WriteLine();
    }

    public class KerfSegment
    {
        public KerfSegment(Segment3d segment)
        {
            Segment = segment;
        }

        public Segment3d Segment { get; set; }
        public Segment3d ShiftedSegment { get; set; }
        public Segment3d SegmentWithKerf { get; set; }

    }
}

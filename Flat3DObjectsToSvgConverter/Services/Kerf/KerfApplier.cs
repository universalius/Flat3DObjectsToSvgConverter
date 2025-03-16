using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models;
using Flat3DObjectsToSvgConverter.Models.EdgeLoopParser;
using GeometRi;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Drawing;

namespace Flat3DObjectsToSvgConverter.Services.Kerf;

public class KerfApplier(IOptions<KerfSettings> options)
{
    public void ApplyKerf(IEnumerable<MeshObjects> meshes)
    {
        var config = options.Value;
        meshes.ToList().ForEach(mesh =>
        {
            mesh.Objects.ToList().ForEach(obj =>
            {
                var mainLoop = obj.Loops.First();
                var segments = mainLoop.ToSegments();
                var points = mainLoop.Points.ToArray();

                var kerfPoints = points.Select(p => new KerfPoint(p)).ToArray();
                var length = kerfPoints.Length;
                for (var i = 0; i < length; i++)
                {
                    var last = length - 1;
                    if (i == last)
                    {
                        kerfPoints[0].NewPoint = kerfPoints[last].NewPoint;
                        break;
                    }

                    var kerfP1 = kerfPoints[i];
                    var kerfP2 = kerfPoints[i + 1];
                    var p1 = kerfP1.Point;
                    var p2 = kerfP2.Point;
                    var vector = new Vector3d(p1.ToPoint3d(), p2.ToPoint3d());

                    var tolerance = 0.1;
                    var xSame = Math.Abs(p1.X - p2.X) <= 0.1;
                    var ySame = Math.Abs(p1.Y - p2.Y) <= 0.1;

                    if (xSame)
                    {
                        var shift = (float)(config.Y * vector.Y);
                        kerfP1.NewPoint = new PointF(p1.X - shift, kerfP1.NewPoint?.Y ?? p1.Y);
                        kerfP2.NewPoint = new PointF(p2.X - shift, kerfP2.NewPoint?.Y ?? p2.Y);
                    }

                    if (ySame)
                    {
                        var shift = (float)(config.X * vector.X);
                        kerfP1.NewPoint = new PointF(kerfP1.NewPoint?.X ?? p1.X, p1.Y + shift);
                        kerfP2.NewPoint = new PointF(kerfP2.NewPoint?.X ?? p2.X, p2.Y + shift);
                    }

                    if (i == 0)
                    {
                        kerfPoints[last].NewPoint = kerfP1.NewPoint;
                    }
                }

                mainLoop.Points = kerfPoints.Select(kp => kp.NewPoint.Value).ToArray();
            });
        });

        Console.WriteLine();
    }

    public class KerfPoint
    {
        public KerfPoint(PointF point)
        {
            Point = point;
        }

        public PointF Point { get; set; }
        public PointF? NewPoint { get; set; } = new PointF();
    }
}

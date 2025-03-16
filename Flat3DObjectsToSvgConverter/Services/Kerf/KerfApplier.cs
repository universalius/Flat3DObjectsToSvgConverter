using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models;
using Flat3DObjectsToSvgConverter.Models.EdgeLoopParser;
using GeometRi;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

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

                segments.Select((s, i) =>
                {
                    var tolerance = 0.1;
                    var xSame = Math.Abs(s.P1.X - s.P2.X) <= 0.1;
                    var ySame = Math.Abs(s.P1.Y - s.P2.Y) <= 0.1;

                    if (xSame)
                    {
                        var newSegment = s.Copy();
                        newSegment.P1.Y -= config.X;
                        newSegment.P2.Y -= config.X;

                        return newSegment;
                    }

                    if (ySame)
                    {
                        var newSegment = s.Copy();
                        newSegment.P1.X -= config.X;
                        newSegment.P2.X -= config.X;

                        return newSegment;
                    }
                });



            });
        });

        Console.WriteLine();
    }
}

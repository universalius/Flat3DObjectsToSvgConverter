using ObjParser;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Flat3DObjectsToSvgConverter.Services
{
    public class ObjectsToSvgConverter
    {
        public string Convert(IEnumerable<MeshObjects> meshes)
        {
            var objectsSizes = meshes.SelectMany(m =>
            {
                return m.Objects.Select(o =>
                {
                    var points = o.Loops.First().Points;
                    return new
                    {
                        m.MeshName,
                        o.Loops,
                        Size = new Extent
                        {
                            XMin = points.Min(p => p.X),
                            XMax = points.Max(p => p.X),
                            YMin = points.Min(p => p.Y),
                            YMax = points.Max(p => p.Y)
                        }
                    };
                }).ToList();
            }).ToList();


            var objectsTransorms = objectsSizes.Select((o, i) =>
            {
                var distanceBetween = 10;
                var shiftByX = i == 0 ? 0 : objectsSizes.Take(i).Select(os => os.Size.XSize + distanceBetween).Sum();
                return new
                {
                    Object = o,
                    //o.Size,
                    Transform = GetTransformToXYZero(new PointF((float)o.Size.XMin, (float)o.Size.YMax), shiftByX)
                };
            }).ToList();

            var svgGroups = objectsTransorms.Select((ot, i) =>
            {
                var loops = ot.Object.Loops;
                var meshName = ot.Object.MeshName;
                //var size = ot.Size;

                var pathes = loops.Select((l, j) =>
                {
                    var pathCoords = string.Join(" ",
                        l.Points.Select(p => $"{p.X.ToString(new CultureInfo("en-US", false))} {p.Y.ToString(new CultureInfo("en-US", false))}")
                        .ToList());

                    var @class = j == 0 ? "main" : string.Empty;
                    return $@"<path id=""{meshName}-{i}-{j}"" d=""M {pathCoords} z"" style=""fill:none;stroke-width:0.264583;stroke:#000000;"" class=""{@class}"" />";
                }).ToList();

                var pathesString = string.Join("\r\n", pathes);
                return @$"<g id=""{meshName}-{i}"" {ot.Transform}>
                            {pathesString}
                          </g>";

            }).ToList();

            var svgGroupsString = string.Join("\r\n", svgGroups);

            var body = @$"
            <svg xmlns=""http://www.w3.org/2000/svg"" width=""300mm"" height=""400mm"" viewBox=""0 0 300 400"">
              {svgGroupsString}
            </svg>
            ";

            return body;
        }

        private string GetTransformToXYZero(PointF point, double shiftByX)
        {
            var x = (-point.X + shiftByX).ToString(new CultureInfo("en-US", false));
            var y = (-point.Y).ToString(new CultureInfo("en-US", false));
            var transform = $"transform=\"translate({x} {y})\"";
            return transform;
        }

    }

    public class MeshObjects
    {
        public string MeshName { get; set; }

        public IEnumerable<ObjectLoops> Objects { get; set; }
    }
}

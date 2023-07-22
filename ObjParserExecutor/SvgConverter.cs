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

namespace ObjParserExecutor
{
    public class SvgConverter
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
                    Transform = GetTransformToXYZero(new PointF((float)o.Size.XMin, (float)o.Size.YMax), shiftByX)
                };
            }).ToList();

            var svgGroups = objectsTransorms.Select((ot, i) =>
            {
                var loops = ot.Object.Loops;
                var meshName = ot.Object.MeshName;

                var pathes = loops.Select((l, j) =>
                {
                    var pathCoords = string.Join(" ",
                        l.Points.Select(p => $"{p.X.ToString(new CultureInfo("en-US", false))} {p.Y.ToString(new CultureInfo("en-US", false))}")
                        .ToList());

                    return $@"<path id=""{meshName}-{i}-{j}"" d=""M {pathCoords} z"" style=""fill:none;stroke-width:0.264583;stroke:#000000;"" />";
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

        private string textTemplate(string x, string y, string value) => @$"
        <text
            xml:space=""preserve""
            transform=""scale(0.26458333)""
            style=""font-size:13.3333px;white-space:pre;fill:none;stroke:#000000;fill:#000000"">
            <tspan x=""{x}"" y=""{y}"">
                <tspan style=""-inkscape-font-specification:sans-serif"">{value}</tspan>
            </tspan>
        </text>
        ";

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

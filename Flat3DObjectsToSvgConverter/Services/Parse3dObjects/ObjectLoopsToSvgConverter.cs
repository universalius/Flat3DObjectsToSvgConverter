using ClipperLib;
using Flat3DObjectsToSvgConverter.Models;
using Microsoft.Extensions.Options;
using ObjParser;
using SvgLib;
using SvgNest.Models;
using System.Drawing;
using System.Globalization;

namespace Flat3DObjectsToSvgConverter.Services.Parse3dObjects
{
    public class ObjectLoopsToSvgConverter
    {
        private readonly IOFileService _file;
        private readonly SvgNestConfig _svgNestConfig;

        public ObjectLoopsToSvgConverter(IOFileService file, IOptions<SvgNestConfig> options)
        {
            _file = file;
            _svgNestConfig = options.Value;
        }

        public string Convert(IEnumerable<MeshObjects> meshes)
        {
            var docSize = _svgNestConfig.Document;
            var document = SvgDocument.Create();
            document.Units = "mm";
            document.Width = docSize.Width;
            document.Height = docSize.Height;
            document.ViewBox = new SvgViewBox
            {
                Left = 0,
                Top = 0,
                Width = docSize.Width,
                Height = docSize.Height
            };

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
                var point = new PointF((float)o.Size.XMin, (float)o.Size.YMax);
                return new
                {
                    Object = o,
                    //o.Size,
                    Transform = new DoublePoint(-o.Size.XMin + shiftByX, -o.Size.YMax) //GetTransformToXYZero(, shiftByX)
                };
            }).ToList();

            //var svgGroups = 
            var i = 0;
            objectsTransorms.ForEach(ot =>
        {
            var loops = ot.Object.Loops.ToList();
            var meshName = ot.Object.MeshName;
            //var size = ot.Size;

            string mainPathId = null;
            int j = 0;
            //var pathes = 
            loops.ForEach(l =>
            {
                var group = document.AddGroup();
                group.Id = $"{meshName}-{i}";
                group.Transform = $"translate({ot.Transform})";

                var pathCoords = string.Join(" ",
                    l.Points.Select(p => $"{p.X.ToString(new CultureInfo("en-US", false))} {p.Y.ToString(new CultureInfo("en-US", false))}")
                    .ToList());

                var pathId = $"{meshName}-{i}-{j}";
                var @class = string.Empty;
                string dataParentId = string.Empty;
                if (j == 0)
                {
                    mainPathId = $"{pathId}";
                    @class = "main";
                }
                else
                {
                    dataParentId = mainPathId; //$"data-parentId=\"{mainPathId}\"";
                }

                var path = group.AddPath();
                path.Id = pathId;
                path.D = $"M {pathCoords} z";

                path.AddClass(@class);
                path.SetStyle(new[]
                {
                        ("fill", "none"),
                        ("stroke-width", "0.264583"),
                        ("stroke", "red"),
                });

                if (!string.IsNullOrEmpty(dataParentId))
                    path.AddData("parentId", dataParentId);

                j++;

                //return $@"<path id=""{pathId}"" d=""M {pathCoords} z"" style=""fill:none;stroke-width:0.264583;stroke:red;"" class=""{@class}"" {dataParentId} />";
            });//.ToList();

            //var pathesString = string.Join("\r\n", pathes);
            //return @$"<g id=""{meshName}-{i}"" {ot.Transform}>
            //            {pathesString}
            //          </g>";
            i++;
        });//.ToList();

            //var svgGroupsString = string.Join("\r\n", svgGroups);

            //var svg = @$"
            //<svg xmlns=""http://www.w3.org/2000/svg"" 
            //    width=""{docSize.Width}mm"" 
            //    height=""{docSize.Height}mm"" 
            //    viewBox=""0 0 {docSize.Width} {docSize.Height}"">
            //  {svgGroupsString}
            //</svg>
            //";

            var svg = document._document.OuterXml;
            _file.SaveSvg("parsed", svg);
            _file.CopyObjFile();

            return svg;
        }

        private string GetTransformToXYZero(PointF point, double shiftByX)
        {
            var x = (-point.X + shiftByX).ToString(new CultureInfo("en-US", false));
            var y = (-point.Y).ToString(new CultureInfo("en-US", false));
            var transform = $"transform=\"translate({x} {y})\"";
            return transform;
        }

    }
}

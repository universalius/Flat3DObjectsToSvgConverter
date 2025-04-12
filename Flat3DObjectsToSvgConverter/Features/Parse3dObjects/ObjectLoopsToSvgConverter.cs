using ClipperLib;
using Flat3DObjectsToSvgConverter.Features;
using Flat3DObjectsToSvgConverter.Models;
using Microsoft.Extensions.Options;
using ObjParser;
using SvgLib;
using SvgNest.Models;
using System.Drawing;
using System.Globalization;

namespace Flat3DObjectsToSvgConverter.Features.Parse3dObjects;

public class ObjectLoopsToSvgConverter
{
    private readonly IOFileService _file;
    private readonly SvgNestConfig _svgNestConfig;

    public ObjectLoopsToSvgConverter(IOFileService file, IOptions<SvgNestConfig> options)
    {
        _file = file;
        _svgNestConfig = options.Value;
    }

    public string ConvertAndSave(IEnumerable<MeshObjects> meshes)
    {
        var stroke = new PathStroke("red", "0.264583");
        var svg = Convert(meshes, stroke);

        //var objectsSizes = meshes.SelectMany(m =>
        //{
        //    return m.Objects.Select(o =>
        //    {
        //        var points = o.Loops.First().Points;
        //        return new
        //        {
        //            m.MeshName,
        //            o.Loops,
        //            Size = new Extent
        //            {
        //                XMin = points.Min(p => p.X),
        //                XMax = points.Max(p => p.X),
        //                YMin = points.Min(p => p.Y),
        //                YMax = points.Max(p => p.Y)
        //            }
        //        };
        //    }).ToList();
        //}).ToList();

        //var objectsTransorms = objectsSizes.Select((o, i) =>
        //{
        //    var distanceBetween = 10;
        //    var shiftByX = i == 0 ? 0 : objectsSizes.Take(i).Select(os => os.Size.XSize + distanceBetween).Sum();
        //    return new
        //    {
        //        Object = o,
        //        Transform = new DoublePoint(-o.Size.XMin + shiftByX, -o.Size.YMax)
        //    };
        //}).ToList();

        //var i = 0;
        //objectsTransorms.ForEach(ot =>
        //{
        //    var loops = ot.Object.Loops.ToList();
        //    var meshName = ot.Object.MeshName;

        //    string mainPathId = null;
        //    int j = 0;
        //    loops.ForEach(l =>
        //    {
        //        var group = document.AddGroup();
        //        group.Id = $"{meshName}-{i}";
        //        group.Transform = $"translate({ot.Transform})";

        //        var pathCoords = string.Join(" ",
        //            l.Points.Select(p => $"{p.X.ToString(new CultureInfo("en-US", false))} {p.Y.ToString(new CultureInfo("en-US", false))}")
        //            .ToList());

        //        var pathId = $"{meshName}-{i}-{j}";
        //        var @class = string.Empty;
        //        string dataParentId = string.Empty;
        //        if (j == 0)
        //        {
        //            mainPathId = $"{pathId}";
        //            @class = "main";
        //        }
        //        else
        //        {
        //            dataParentId = mainPathId;
        //        }

        //        var path = group.AddPath();
        //        path.Id = pathId;
        //        path.D = $"M {pathCoords} z";

        //        path.AddClass(@class);

        //        var strokeColor = l.IsResized ? "blue" : "red";
        //        path.SetStyle(new[]
        //        {
        //                    ("fill", "none"),
        //                    ("stroke-width", "0.264583"),
        //                    ("stroke", strokeColor),
        //        });

        //        if (!string.IsNullOrEmpty(dataParentId))
        //            path.AddData("parentId", dataParentId);

        //        j++;
        //    });

        //    i++;
        //});

        //var svg = document._document.OuterXml;
        _file.SaveSvg("parsed", svg);
        _file.CopyObjFile();

        return svg;
    }

    public string Convert(IEnumerable<MeshObjects> meshes, PathStroke stroke)
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
            return new
            {
                Object = o,
                Transform = new DoublePoint(-o.Size.XMin + shiftByX, -o.Size.YMax)
            };
        }).ToList();

        var i = 0;
        objectsTransorms.ForEach(ot =>
        {
            var loops = ot.Object.Loops.ToList();
            var meshName = ot.Object.MeshName;

            string mainPathId = null;
            int j = 0;
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
                    dataParentId = mainPathId;
                }

                var path = group.AddPath();
                path.Id = pathId;
                path.D = $"M {pathCoords} z";

                path.AddClass(@class);

                var strokeColor = l.IsResized ? "blue" : stroke.Color;
                path.SetStyle([
                    ("fill", "none"),
                    ("stroke-width", stroke.Width),
                    ("stroke", strokeColor),
                ]);

                if (!string.IsNullOrEmpty(dataParentId))
                    path.AddData("parentId", dataParentId);

                j++;
            });

            i++;
        });

        var svg = document._document.OuterXml;

        return svg;
    }
}

public record PathStroke(string Color, string Width);

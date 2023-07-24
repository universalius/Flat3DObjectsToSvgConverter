using ObjParser;
using Plain3DObjectsToSvgConverter.Extensions;
using Plain3DObjectsToSvgConverter.Models;
using Plain3DObjectsToSvgConverter.Models.Enums;
using SvgLib;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using static SvgLib.SvgDefaults.Attributes;

namespace Plain3DObjectsToSvgConverter
{
    public class ObjectsLabelsToSvgConverter
    {
        private CultureInfo culture = new CultureInfo("en-US", false);

        public async Task<string> Convert()
        {
            var content = await File.ReadAllTextAsync(@"D:\Виталик\Cat_Hack\Svg\test_compacted.svg");
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(content);

            SvgDocument svgDocument = new SvgDocument(xmlDocument, xmlDocument.DocumentElement);

            var groups = svgDocument.Element.GetElementsByTagName("g").Cast<XmlElement>().ToArray();

            groups.ToList().ForEach(e =>
            {
                var group = new SvgGroup(e);
                if (group.Transform != "translate(0 0)")
                {
                    var pathes = group.Element.GetElementsByTagName("path").Cast<XmlElement>()
                    .Select(pe => new SvgPath(pe));

                    var path = pathes.First(p => p.GetClasses().Contains("main"));
                    var label = GetLabel(path.Id);
                    var leftTopPoint = GetLabelCoords(path);

                    var labelGroup = group.AddGroup();
                    var rotate = int.Parse(group.TransformRotate);

                    if (rotate == 0)
                    {
                        leftTopPoint.YMin -= 5;
                    }

                    if (rotate == 90)
                    {
                        leftTopPoint.YMin += leftTopPoint.YSize;
                    }

                    if (rotate == 180)
                    {
                        leftTopPoint.XMin += leftTopPoint.XSize;
                        leftTopPoint.YMin += leftTopPoint.YSize;
                    }

                    if (rotate == 270)
                    {
                        leftTopPoint.XMin += leftTopPoint.XSize;
                    }

                    labelGroup.Transform =
                        $"translate({leftTopPoint.XMin.ToString(culture)} {leftTopPoint.YMin.ToString(culture)}) scale(0.26458333) rotate({-rotate})";
                    AddLabelToGroup("1"/*label*/, labelGroup);
                }
            });


            return svgDocument._document.OuterXml;
        }

        private string GetLabel(string pathId)
        {
            var idSections = pathId.Split("-")[0].Split("_");
            var firstPart = idSections[0];

            int secondNumber;
            var secondPart = string.Empty;
            if (idSections.Count() > 1)
            {
                secondPart = int.TryParse(idSections[1], out secondNumber) ?
                    (secondNumber > 10 ? secondNumber.ToString() : string.Empty) : string.Empty;
            }


            var label = $"{firstPart}{(string.IsNullOrEmpty(secondPart) ? string.Empty : $"_{secondPart}")}";
            return label;
        }

        private void AddLabelToGroup(string label, SvgGroup group)
        {
            label.ToList().ForEach(c =>
            {
                var path = group.AddPath();
                path.D = ((SvgNumber)int.Parse(c.ToString())).GetDescription();
                path.Fill = "#000000";
            });
        }

        private Extent GetLabelCoords(SvgPath path)
        {
            var pointsString = new Regex("[mz]").Replace(path.D.ToLowerInvariant(), string.Empty).Trim().Split("l");
            var points = pointsString.Select((s, i) =>
            {
                var points = s.Trim().Split(" ");
                return new PointD(double.Parse(points[0], culture), double.Parse(points[1], culture));
            }).ToList();

            return new Extent
            {
                XMin = points.Min(p => p.X),
                XMax = points.Max(p => p.X),
                YMin = points.Min(p => p.Y),
                YMax = points.Max(p => p.Y)
            };
        }
    }
}

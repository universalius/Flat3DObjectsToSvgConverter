using ClipperLib;
using Microsoft.Extensions.Logging;
using ObjParser;
using ObjParserExecutor.Helpers;
using SvgLib;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;

namespace Flat3DObjectsToSvgConverter.Services
{
    public class ObjectsLabelsToSvgConverter
    {
        private CultureInfo culture = new CultureInfo("en-US", false);
        private readonly IEnumerable<SvgPath> _svgLetters;
        private readonly double _letterWidth;
        private readonly double _letterHeight;
        private readonly ILogger<ObjectsLabelsToSvgConverter> _logger;
        private readonly IOFileService _file;

        public ObjectsLabelsToSvgConverter(ILogger<ObjectsLabelsToSvgConverter> logger, IOFileService file)
        {
            var mainFolder = AppDomain.CurrentDomain.BaseDirectory;

            SvgDocument svgDocument = ParseSvgFile(Path.Combine(mainFolder, "Asserts\\Letters.svg"));
            var pathElements = svgDocument.Element.GetElementsByTagName("path").Cast<XmlElement>().ToArray();
            _svgLetters = pathElements.Select(e => new SvgPath(e));
            _letterHeight = GetOrHeight();
            _letterWidth = GetUnderscoreWidth();
            _file = file;

            _logger = logger;
        }

        public async Task<string> Convert(string svg)
        {
            var watch = Stopwatch.StartNew();
            Console.WriteLine("Start placing labels for svg curves!");
            Console.WriteLine();

            //_logger.LogInformation("Test qwer asdf cvbbb");

            SvgDocument svgDocument = ParseSvgString(svg);
            var groupElements = svgDocument.Element.GetElementsByTagName("g").Cast<XmlElement>().ToArray();

            foreach (var element in groupElements)
            {
                var group = new SvgGroup(element);
                if (group.Transform != "translate(0 0)")
                {
                    var pathes = group.Element.GetElementsByTagName("path").Cast<XmlElement>()
                    .Select(pe => new SvgPath(pe));

                    var path = pathes.FirstOrDefault(p => p.GetClasses().Contains("main"));
                    if(path == null)
                    {
                        throw new Exception("At least one path in a group should have main class");
                    }

                    var label = GetLabel(path.Id);

                    var labelGroup = group.AddGroup();
                    labelGroup.Transform = GetLabelGroupTransform(path, group.GetTransformRotate());

                    await AddLabelToGroup(label, labelGroup);
                }
            }

            watch.Stop();
            Console.WriteLine($"Finished placing labels for svg curves! Took - {watch.ElapsedMilliseconds / 1000.0} sec");
            Console.WriteLine();

            var labelsSvg = svgDocument._document.OuterXml;
            _file.SaveSvg("labels", labelsSvg);

            return labelsSvg;
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
                    secondNumber > 10 ? secondNumber.ToString() : string.Empty : string.Empty;
            }


            var label = $"{firstPart}{(string.IsNullOrEmpty(secondPart) ? string.Empty : $"_{secondPart}")}";
            return label;
        }

        private async Task AddLabelToGroup(string label, SvgGroup group)
        {
            //int spacesCount = 0;
            int i = 0;
            label.ToList().ForEach(c =>
            {
                var s = c.ToString();
                if (s != " ")
                {
                    var shiftByX = i * _letterWidth * 0.8;

                    var path = group.AddPath();
                    path.D = _svgLetters.FirstOrDefault(p => p.Id == s).D;
                    path.Fill = "#000000";
                    path.Transform = $"translate({shiftByX.ToString(culture)})";
                }

                i++;
                //labelPathes.Add(path);
            });
        }

        private Extent GetLabelCoords(SvgPath path)
        {
            var pointsString = new Regex("[mz]").Replace(path.D.ToLowerInvariant(), string.Empty).Trim().Split("l");
            var points = pointsString.Select((s, i) => s.ToDoublePoint()).ToList();

            return new Extent
            {
                XMin = points.Min(p => p.X),
                XMax = points.Max(p => p.X),
                YMin = points.Min(p => p.Y),
                YMax = points.Max(p => p.Y)
            };
        }

        private string GetLabelGroupTransform(SvgPath path, string parentGroupRotate)
        {
            var leftTopPoint = GetLabelCoords(path);
            var rotate = int.Parse(parentGroupRotate);
            var shift = _letterHeight * 1.2;
            if (rotate == 0)
            {
                leftTopPoint.YMin -= shift;
            }

            if (rotate == 90)
            {
                leftTopPoint.XMin -= shift;
                leftTopPoint.YMin += leftTopPoint.YSize;
            }

            if (rotate == 180)
            {
                leftTopPoint.XMin += leftTopPoint.XSize;
                leftTopPoint.YMin += leftTopPoint.YSize + shift;
            }

            if (rotate == 270)
            {
                leftTopPoint.XMin += leftTopPoint.XSize + shift;
            }

            return $"translate({leftTopPoint.XMin.ToString(culture)} {leftTopPoint.YMin.ToString(culture)}) rotate({-rotate})";
        }

        public static SvgDocument ParseSvgFile(string filePath)
        {
            var content = File.ReadAllText(filePath);
            return ParseSvgString(content);
        }

        private static SvgDocument ParseSvgString(string svg)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(svg);

            return new SvgDocument(xmlDocument, xmlDocument.DocumentElement);
        }

        private double GetUnderscoreWidth()
        {
            var undescorePath = _svgLetters.First(p => p.Id == "_");
            var d = undescorePath.D.ToLower();
            var marker = "h ";
            var index = d.IndexOf(marker);
            var first = index + marker.Count();
            var last = d.IndexOf(" ", first);
            return double.Parse(d.Substring(first, last - first), culture);

        }

        private double GetOrHeight()
        {
            var orPath = _svgLetters.First(p => p.Id == "|");
            var d = orPath.D.ToLower();
            var marker = "v ";
            var index = d.LastIndexOf(marker);
            var first = index + marker.Count();
            var last = d.IndexOf(" z", first);
            return double.Parse(d.Substring(first, last - first), culture);
        }
    }
}

using Flat3DObjectsToSvgConverter.Models;
using Microsoft.Extensions.Options;
using SvgLib;
using SvgNest.Models;
using System.Diagnostics;
using System.Xml;

namespace Flat3DObjectsToSvgConverter.Features
{
    public class SvgCompactingService
    {
        private readonly SvgNestConfig _svgNestConfig;
        private readonly Statistics _statistics;

        private readonly IOFileService _file;

        public SvgCompactingService(IOptions<SvgNestConfig> options, IOFileService file, Statistics statistics)
        {
            _svgNestConfig = options.Value;
            _file = file;
            _statistics = statistics;
        }

        public async Task<string> Compact(string inputSvg)
        {
            var watch = Stopwatch.StartNew();
            Console.WriteLine("Start compacting svg curves!");
            Console.WriteLine();

            var svgNest = new SvgNest.SvgNest(_svgNestConfig);
            svgNest.ParseSvg(inputSvg);
            await svgNest.Start();

            var index = 0;
            foreach (var svgDocument in svgNest.CompactedSvgs)
            {
                var svgString = svgDocument.OuterXml;
                _file.SaveSvg($"compacted{(index == 0 ? string.Empty : index)}", svgString);
                index++;
            }

            watch.Stop();

            var document = svgNest.CompactedSvgs.First();
            var groupElements = document.GetElementsByTagName("g").Cast<XmlElement>().ToArray();
            _statistics.CompactedLoopsCount = groupElements.Length;

            Console.WriteLine($"Finished compacting svg curves! Compacted {groupElements.Length} loops. Took - {watch.ElapsedMilliseconds / 1000.0} sec");
            Console.WriteLine();

            return document.OuterXml;
        }
    }
}

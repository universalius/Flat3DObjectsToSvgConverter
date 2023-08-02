using Microsoft.Extensions.Options;
using SvgNest.Models;
using System.Diagnostics;

namespace Flat3DObjectsToSvgConverter.Services
{
    public class SvgCompactingService
    {
        private readonly SvgNestConfig _svgNestConfig;

        private readonly IOFileService _file;

        public SvgCompactingService(IOptions<SvgNestConfig> options, IOFileService file)
        {
            _svgNestConfig = options.Value;
            _file = file;
        }

        public async Task<string> Compact(string inputSvg)
        {
            var watch = Stopwatch.StartNew();
            Console.WriteLine("Start compacting svg curves!");
            Console.WriteLine();

            var svgNest = new SvgNest.SvgNest(_svgNestConfig);
            svgNest.ParseSvg(inputSvg);
            await svgNest.Start();

            var result = svgNest.CompactedSvgs.First().OuterXml;
            _file.SaveSvg("compacted", result);

            watch.Stop();
            Console.WriteLine($"Finished compacting svg curves! Took - {watch.ElapsedMilliseconds / 1000.0} sec");
            Console.WriteLine();

            return result;
        }
    }
}

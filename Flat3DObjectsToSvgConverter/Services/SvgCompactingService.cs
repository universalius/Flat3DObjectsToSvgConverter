using SvgNest.Models;
using System.Diagnostics;

namespace Flat3DObjectsToSvgConverter.Services
{
    public class SvgCompactingService : ISvgCompactingService
    {
        public async Task<string> Compact(string inputSvg)
        {
            var watch = Stopwatch.StartNew();
            Console.WriteLine("Start compacting svg curves!");
            Console.WriteLine();

            var svgNest = new SvgNest.SvgNest(new SvgNestConfig {  Spacing = 10});
            svgNest.ParseSvg(inputSvg);
            await svgNest.Start();

            var result = svgNest.CompactedSvgs.First().OuterXml;
            File.WriteAllText(@"D:\Виталик\Cat_Hack\Svg\result_compacted.svg", result);

            watch.Stop();
            Console.WriteLine($"Finished compacting svg curves! Took - {watch.ElapsedMilliseconds / 1000.0} sec");
            Console.WriteLine();

            return result;
        }
    }
}

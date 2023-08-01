namespace Plain3DObjectsToSvgConverter.Services
{
    public class SvgCompactingService : ISvgCompactingService
    {
        public async Task<string> Compact(string inputSvg)
        {
            var svgNest = new SvgNest.SvgNest();
            svgNest.ParseSvg(inputSvg);
            await svgNest.Start();

            var result = svgNest.CompactedSvgs.First().OuterXml;
            File.WriteAllText(@"D:\Виталик\Cat_Hack\Svg\result_compacted.svg", result);

            return result;
        }
    }
}

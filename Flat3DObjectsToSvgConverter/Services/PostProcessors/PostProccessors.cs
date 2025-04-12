using Flat3DObjectsToSvgConverter.Models;
using Flat3DObjectsToSvgConverter.Services.CleanLoops;
using Microsoft.Extensions.Options;

namespace Flat3DObjectsToSvgConverter.Services.PostProcessors
{
    public class PostProccessors(
            ObjectLoopsTinyGapsRemover objectLoopsTinyGapsRemover,
            ObjectsLabelsPreciseLocator objectsLabelsPreciseLocator,
            LoopsColorDivider loopsColorDivider,
            LoopsTabsGenerator loopsTabsGenerator,
            MergeLabelsWithTabsSvg mergeLabelsWithTabsSvg,
            IOptions<FeaturesSettings> options)
    {
        public async Task Run(string kerfedSvg)
        {
            //SvgDocument svgDocument = SvgFileHelpers.ParseSvgFile(@"D:\Виталик\Cat_Hack\Svg\Test1 01.06.2024 14-23-28\Test1_compacted.svg");
            //SvgDocument svgDocument = SvgFileHelpers.ParseSvgFile(@"D:\Виталик\Cat_Hack\Svg\Test15 21.07.2024 12-09-34\Test15_compacted.svg");
            //var compactedSvg = svgDocument.Element.OuterXml;

            var withoutGapsSvg = objectLoopsTinyGapsRemover.ReplaceGapsWithLine(kerfedSvg);
            //var withoutGapsSvg = kerfedSvg;

            var coloredSvg = loopsColorDivider.SetLoopsColorBasedOnLength(withoutGapsSvg);

            var labelsSvg = await objectsLabelsPreciseLocator.PlaceLabels(coloredSvg);

            if (options.Value.MakeTabs)
            {
                var tabsSvg = await loopsTabsGenerator.CutLoopsToMakeTabs(coloredSvg);
                mergeLabelsWithTabsSvg.Merge(labelsSvg, tabsSvg);
            }
        }
    }
}



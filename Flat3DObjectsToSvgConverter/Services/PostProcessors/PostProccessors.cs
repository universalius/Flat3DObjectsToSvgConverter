using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Models;
using Microsoft.Extensions.Options;
using SvgLib;

namespace Flat3DObjectsToSvgConverter.Services.PostProcessors
{
    public class PostProccessors
    {
        private readonly ObjectsLabelsPreciseLocator _objectsLabelsPreciseLocator;
        private readonly LoopsColorDivider _loopsColorDivider;
        private readonly LoopsTabsGenerator _loopsTabsGenerator;
        private readonly MergeLabelsWithTabsSvg _mergeLabelsWithTabsSvg;
        private readonly FeaturesSettings _features;

        public PostProccessors(ObjectsLabelsPreciseLocator objectsLabelsPreciseLocator,
            LoopsColorDivider loopsColorDivider,
            LoopsTabsGenerator loopsTabsGenerator,
            MergeLabelsWithTabsSvg mergeLabelsWithTabsSvg,
            IOptions<FeaturesSettings> options)
        {
            _loopsColorDivider = loopsColorDivider;
            _objectsLabelsPreciseLocator = objectsLabelsPreciseLocator;
            _loopsTabsGenerator = loopsTabsGenerator;
            _mergeLabelsWithTabsSvg = mergeLabelsWithTabsSvg;
            _features = options.Value;
        }

        public async Task Run(string compactedSvg)
        {
            //SvgDocument svgDocument = SvgFileHelpers.ParseSvgFile(@"D:\Виталик\Cat_Hack\Svg\Test1 01.06.2024 14-23-28\Test1_compacted.svg");
            //SvgDocument svgDocument = SvgFileHelpers.ParseSvgFile(@"D:\Виталик\Cat_Hack\Svg\Test15 21.07.2024 12-09-34\Test15_compacted.svg");
            //var compactedSvg = svgDocument.Element.OuterXml;

            var coloredSvg = _loopsColorDivider.SetLoopsColorBasedOnLength(compactedSvg);

            var labelsSvg = await _objectsLabelsPreciseLocator.PlaceLabels(coloredSvg);

            if (_features.MakeTabs)
            {
                var tabsSvg = await _loopsTabsGenerator.CutLoopsToMakeTabs(coloredSvg);
                _mergeLabelsWithTabsSvg.Merge(labelsSvg, tabsSvg);
            }
        }
    }
}



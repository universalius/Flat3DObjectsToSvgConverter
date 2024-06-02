using Flat3DObjectsToSvgConverter.Helpers;
using Flat3DObjectsToSvgConverter.Services;
using Flat3DObjectsToSvgConverter.Services.CleanLoops;
using Flat3DObjectsToSvgConverter.Services.Parse3dObjects;
using SvgLib;

namespace Flat3DObjectsToSvgConverter.Services.PostProcessors
{
    public class PostProccessors
    {
        private readonly ObjectsLabelsPreciseLocator _objectsLabelsPreciseLocator;
        private readonly LoopsColorDivider _loopsColorDivider;
        private readonly LoopsTabsGenerator _loopsTabsGenerator;
        private readonly MergeLabelsWithTabsSvg _mergeLabelsWithTabsSvg;

        public PostProccessors(ObjectsLabelsPreciseLocator objectsLabelsPreciseLocator,
            LoopsColorDivider loopsColorDivider,
            LoopsTabsGenerator loopsTabsGenerator,
            MergeLabelsWithTabsSvg mergeLabelsWithTabsSvg)
        {
            _loopsColorDivider = loopsColorDivider;
            _objectsLabelsPreciseLocator = objectsLabelsPreciseLocator;
            _loopsTabsGenerator = loopsTabsGenerator;
            _mergeLabelsWithTabsSvg = mergeLabelsWithTabsSvg;
        }

        public async Task Run(string compactedSvg)
        {
            //SvgDocument svgDocument = SvgFileHelpers.ParseSvgFile(@"D:\Виталик\Cat_Hack\Svg\Test1 01.06.2024 14-23-28\Test1_compacted.svg");
            ////SvgDocument svgDocument = SvgFileHelpers.ParseSvgFile(@"D:\Виталик\Cat_Hack\Svg\test_rays.svg");
            //var compactedSvg = svgDocument.Element.OuterXml;

            var coloredSvg = _loopsColorDivider.SetLoopsColorBasedOnLength(compactedSvg);

            var labelsSvg = await _objectsLabelsPreciseLocator.PlaceLabels(coloredSvg);

            var tabsSvg = await _loopsTabsGenerator.CutLoopsToMakeTabs(coloredSvg);

            _mergeLabelsWithTabsSvg.Merge(labelsSvg, tabsSvg);
        }
    }
}



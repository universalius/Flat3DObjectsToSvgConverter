using Microsoft.Extensions.Hosting;
using Flat3DObjectsToSvgConverter.Services;
using SvgLib;
using Flat3DObjectsToSvgConverter.Helpers;

namespace Flat3DObjectsToSvgConverter;

public class Flat3DObjectsToSvgHostedService : IHostedService
{
    private readonly ObjectsLabelsToSvgConverter _objectsLabelsToSvgConverter;
    private readonly ObjectsLabelsPreciseLocator _objectsLabelsPreciseLocator;
    private readonly SvgCompactingService _svgCompactingService;
    private readonly ObjectsToLoopsConverter _objectsToLoopsConverter;
    private readonly ObjectsToSvgConverter _objectsToSvgConverter;
    private readonly LoopsTabsGenerator _loopsTabsGenerator;
    private readonly ObjectLoopsAlligner _objectLoopsAlligner;
    private readonly MergeLabelsWithTabsSvg _mergeLabelsWithTabsSvg;

    public Flat3DObjectsToSvgHostedService(ObjectsLabelsToSvgConverter objectsLabelsToSvgConverter,
        ObjectsLabelsPreciseLocator objectsLabelsPreciseLocator,
        SvgCompactingService svgCompactingService,
        ObjectsToLoopsConverter objectsToLoopsConverter,
        ObjectsToSvgConverter objectsToSvgConverter,
        LoopsTabsGenerator loopsTabsGenerator,
        ObjectLoopsAlligner objectLoopsAlligner,
        MergeLabelsWithTabsSvg mergeLabelsWithTabsSvg)
    {
        _objectsLabelsToSvgConverter = objectsLabelsToSvgConverter;
        _svgCompactingService = svgCompactingService;
        _objectsToLoopsConverter = objectsToLoopsConverter;
        _objectsToSvgConverter = objectsToSvgConverter;
        _objectsLabelsPreciseLocator = objectsLabelsPreciseLocator;
        _loopsTabsGenerator = loopsTabsGenerator;
        _objectLoopsAlligner = objectLoopsAlligner;
        _mergeLabelsWithTabsSvg = mergeLabelsWithTabsSvg;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        var meshesObjects = await _objectsToLoopsConverter.Convert();

        _objectLoopsAlligner.MakeLoopsPerpendicularToAxis(meshesObjects);

        var svg = _objectsToSvgConverter.Convert(meshesObjects);

        //Console.ReadKey();

        var compactedSvg = await _svgCompactingService.Compact(svg);

        //SvgDocument svgDocument = SvgFileHelpers.ParseSvgFile(@"D:\Виталик\Cat_Hack\Svg\Test10 25.02.2024 15-37-51\test10_compacted.svg");
        //SvgDocument svgDocument = SvgFileHelpers.ParseSvgFile(@"D:\Виталик\Cat_Hack\Svg\test_rays.svg");
        //var compactedSvg = svgDocument.Element.OuterXml;

        //await _objectsLabelsToSvgConverter.Convert(compactedSvg);

        var labelsSvg = await _objectsLabelsPreciseLocator.PlaceLabels(compactedSvg);

        var tabsSvg = await _loopsTabsGenerator.CutLoopsToMakeTabs(compactedSvg);

        _mergeLabelsWithTabsSvg.Merge(labelsSvg, tabsSvg);

        Console.ReadKey();
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}



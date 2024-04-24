using Microsoft.Extensions.Hosting;
using Flat3DObjectsToSvgConverter.Services;
using SvgLib;
using Flat3DObjectsToSvgConverter.Helpers;

namespace Flat3DObjectsToSvgConverter;

public class Flat3DObjectsToSvgHostedService : IHostedService
{
    private readonly ObjectsLabelsToSvgConverter _objectsLabelsToSvgConverter;
    private readonly ObjectsLabelsPreciseLocator _objectsLabelsToSvgPreciseConverter;
    private readonly SvgCompactingService _svgCompactingService;
    private readonly ObjectsToLoopsConverter _objectsToLoopsConverter;
    private readonly ObjectsToSvgConverter _objectsToSvgConverter;
    private readonly LoopsTabsGenerator _cutLoopsToMakeSupportSvgConverter;

    public Flat3DObjectsToSvgHostedService(ObjectsLabelsToSvgConverter objectsLabelsToSvgConverter,
        ObjectsLabelsPreciseLocator objectsLabelsToSvgPreciseConverter,
        SvgCompactingService svgCompactingService,
        ObjectsToLoopsConverter objectsToLoopsConverter,
        ObjectsToSvgConverter objectsToSvgConverter,
        LoopsTabsGenerator cutLoopsToMakeSupportSvgConverter)
    {
        _objectsLabelsToSvgConverter = objectsLabelsToSvgConverter;
        _svgCompactingService = svgCompactingService;
        _objectsToLoopsConverter = objectsToLoopsConverter;
        _objectsToSvgConverter = objectsToSvgConverter;
        _objectsLabelsToSvgPreciseConverter = objectsLabelsToSvgPreciseConverter;
        _cutLoopsToMakeSupportSvgConverter = cutLoopsToMakeSupportSvgConverter;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        var meshesObjects = await _objectsToLoopsConverter.Convert();

        var svg = _objectsToSvgConverter.Convert(meshesObjects);

        var compactedSvg = await _svgCompactingService.Compact(svg);

        //SvgDocument svgDocument = SvgFileHelpers.ParseSvgFile(@"D:\Виталик\Cat_Hack\Svg\Test10 25.02.2024 15-37-51\test10_compacted.svg");
        //SvgDocument svgDocument = SvgFileHelpers.ParseSvgFile(@"D:\Виталик\Cat_Hack\Svg\test_rays.svg");
        //var compactedSvg = svgDocument.Element.OuterXml;

        //await _objectsLabelsToSvgConverter.Convert(compactedSvg);

        await _objectsLabelsToSvgPreciseConverter.PlaceLabels(compactedSvg);

        await _cutLoopsToMakeSupportSvgConverter.CutLoopsToMakeTabs(compactedSvg);

        Console.ReadKey();
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}



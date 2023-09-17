using Microsoft.Extensions.Hosting;
using Flat3DObjectsToSvgConverter.Services;
using SvgLib;

namespace Flat3DObjectsToSvgConverter;

public class Flat3DObjectsToSvgHostedService : IHostedService
{
    private readonly ObjectsLabelsToSvgConverter _objectsLabelsToSvgConverter;
    private readonly ObjectsLabelsPreciseLocatorAndSvgConverter _objectsLabelsToSvgPreciseConverter;
    private readonly SvgCompactingService _svgCompactingService;
    private readonly ObjectsToLoopsConverter _objectsToLoopsConverter;
    private readonly ObjectsToSvgConverter _objectsToSvgConverter;
    private readonly CutLoopsToMakeSupportSvgConverter _cutLoopsToMakeSupportSvgConverter;

    public Flat3DObjectsToSvgHostedService(ObjectsLabelsToSvgConverter objectsLabelsToSvgConverter,
        ObjectsLabelsPreciseLocatorAndSvgConverter objectsLabelsToSvgPreciseConverter,
        SvgCompactingService svgCompactingService,
        ObjectsToLoopsConverter objectsToLoopsConverter,
        ObjectsToSvgConverter objectsToSvgConverter,
        CutLoopsToMakeSupportSvgConverter cutLoopsToMakeSupportSvgConverter)
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
        //var meshesObjects = await _objectsToLoopsConverter.Convert();

        //var svg = _objectsToSvgConverter.Convert(meshesObjects);

        //var compactedSvg = await _svgCompactingService.Compact(svg);

        SvgDocument svgDocument = ObjectsLabelsToSvgConverter.ParseSvgFile(@"D:\Виталик\Cat_Hack\Svg\test5 25.08.2023 8-45-36\test5_compacted.svg");
        var compactedSvg = svgDocument.Element.OuterXml;

        //await _objectsLabelsToSvgConverter.Convert(compactedSvg);

        //await _objectsLabelsToSvgPreciseConverter.Convert(compactedSvg);

        await _cutLoopsToMakeSupportSvgConverter.Convert(compactedSvg);

        Console.ReadKey();
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}



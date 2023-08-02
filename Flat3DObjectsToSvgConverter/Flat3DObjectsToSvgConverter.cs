using Microsoft.Extensions.Hosting;
using Flat3DObjectsToSvgConverter.Services;

namespace Flat3DObjectsToSvgConverter;

public class Flat3DObjectsToSvgHostedService : IHostedService
{
    private readonly ObjectsLabelsToSvgConverter _objectsLabelsToSvgConverter;
    private readonly SvgCompactingService _svgCompactingService;
    private readonly ObjectsToLoopsConverter _objectsToLoopsConverter;
    private readonly ObjectsToSvgConverter _objectsToSvgConverter;

    public Flat3DObjectsToSvgHostedService(ObjectsLabelsToSvgConverter objectsLabelsToSvgConverter,
        SvgCompactingService svgCompactingService,
        ObjectsToLoopsConverter objectsToLoopsConverter,
        ObjectsToSvgConverter objectsToSvgConverter)
    {
        _objectsLabelsToSvgConverter = objectsLabelsToSvgConverter;
        _svgCompactingService = svgCompactingService;
        _objectsToLoopsConverter = objectsToLoopsConverter;
        _objectsToSvgConverter = objectsToSvgConverter;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        var meshesObjects = await _objectsToLoopsConverter.Convert();

        var svg = _objectsToSvgConverter.Convert(meshesObjects);

        var compactedSvg = await _svgCompactingService.Compact(svg);

        await _objectsLabelsToSvgConverter.Convert(compactedSvg);

        Console.ReadKey();
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}



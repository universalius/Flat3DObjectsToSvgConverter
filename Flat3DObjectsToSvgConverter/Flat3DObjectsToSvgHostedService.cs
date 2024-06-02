using Microsoft.Extensions.Hosting;
using Flat3DObjectsToSvgConverter.Services;
using Flat3DObjectsToSvgConverter.Services.Parse3dObjects;
using Flat3DObjectsToSvgConverter.Services.PostProcessors;

namespace Flat3DObjectsToSvgConverter;

public class Flat3DObjectsToSvgHostedService : IHostedService
{
    private readonly PostProccessors _postProccessors;
    private readonly SvgCompactingService _svgCompactingService;
    private readonly ThreeDObjectsParser _3DObjectsParser;

    public Flat3DObjectsToSvgHostedService(PostProccessors postProccessors,
        SvgCompactingService svgCompactingService,
        ThreeDObjectsParser threeDObjectsParser)
    {
        _postProccessors = postProccessors;
        _svgCompactingService = svgCompactingService;
        _3DObjectsParser = threeDObjectsParser;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        var svg = await _3DObjectsParser.Transform3DObjectsTo2DSvgLoops();

        //Console.ReadKey();

        var compactedSvg = await _svgCompactingService.Compact(svg);

        await _postProccessors.Run(compactedSvg);

        Console.ReadKey();
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}



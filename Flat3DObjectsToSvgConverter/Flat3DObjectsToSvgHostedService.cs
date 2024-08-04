using Microsoft.Extensions.Hosting;
using Flat3DObjectsToSvgConverter.Services;
using Flat3DObjectsToSvgConverter.Services.Parse3dObjects;
using Flat3DObjectsToSvgConverter.Services.PostProcessors;
using Flat3DObjectsToSvgConverter.Models;

namespace Flat3DObjectsToSvgConverter;

public class Flat3DObjectsToSvgHostedService : IHostedService
{
    private readonly PostProccessors _postProccessors;
    private readonly SvgCompactingService _svgCompactingService;
    private readonly ThreeDObjectsParser _3DObjectsParser;
    private readonly Statistics _statistics;

    public Flat3DObjectsToSvgHostedService(PostProccessors postProccessors,
        SvgCompactingService svgCompactingService,
        ThreeDObjectsParser threeDObjectsParser,
        Statistics statistics)
    {
        _postProccessors = postProccessors;
        _svgCompactingService = svgCompactingService;
        _3DObjectsParser = threeDObjectsParser;
        _statistics = statistics;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        var svg = await _3DObjectsParser.Transform3DObjectsTo2DSvgLoops();

        //Console.ReadKey();

        var compactedSvg = await _svgCompactingService.Compact(svg);

        await _postProccessors.Run(compactedSvg);

        if (_statistics.ObjectsCount != _statistics.CompactedLoopsCount)
        {
            Console.WriteLine($"NOT COMPACTED ALL PARSED OBJECTS, total parsed objects - {_statistics.ObjectsCount}, " +
                $"total compacted loops - {_statistics.CompactedLoopsCount}. Pls descrease spacing or increase document size");
            Console.WriteLine();
        }

        Console.ReadKey();
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}



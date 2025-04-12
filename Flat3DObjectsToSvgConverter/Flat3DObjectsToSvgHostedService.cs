﻿using Flat3DObjectsToSvgConverter.Features;
using Flat3DObjectsToSvgConverter.Features.Kerf;
using Flat3DObjectsToSvgConverter.Features.Parse3dObjects;
using Flat3DObjectsToSvgConverter.Features.PostProcessors;
using Flat3DObjectsToSvgConverter.Models;
using Microsoft.Extensions.Hosting;

namespace Flat3DObjectsToSvgConverter;

public class Flat3DObjectsToSvgHostedService(PostProccessors postProccessors,
        SvgCompactingService svgCompactingService,
        ThreeDObjectsParser threeDObjectsParser,
        KerfApplier kerfApplier,
        Statistics statistics) : IHostedService
{
    public async Task StartAsync(CancellationToken stoppingToken)
    {
        var svg = await threeDObjectsParser.Transform3DObjectsTo2DSvgLoops();

        ////Console.ReadKey();

        var compactedSvg = await svgCompactingService.Compact(svg);

        var kerfedSvg = kerfApplier.ApplyKerf(compactedSvg);

        await postProccessors.Run(/*"");*/ kerfedSvg);

        if (statistics.ObjectsCount != statistics.CompactedLoopsCount)
        {
            Console.WriteLine($"NOT COMPACTED ALL PARSED OBJECTS, total parsed objects - {statistics.ObjectsCount}, " +
                $"total compacted loops - {statistics.CompactedLoopsCount}. Pls descrease spacing or increase document size");
            Console.WriteLine();
        }

        Console.ReadKey();
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}



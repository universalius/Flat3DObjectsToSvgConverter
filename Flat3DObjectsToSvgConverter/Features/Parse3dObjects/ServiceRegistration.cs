using Microsoft.Extensions.DependencyInjection;
using Flat3DObjectsToSvgConverter.Features.CloseSlots;
using Flat3DObjectsToSvgConverter.Features.Kerf;
using Flat3DObjectsToSvgConverter.Features.CleanLoops;

namespace Flat3DObjectsToSvgConverter.Features.Parse3dObjects;

public static class ServiceRegistration
{
    public static IServiceCollection AddParse3dObjectsServices(this IServiceCollection services)
    {
        services
            .AddSingleton<ThreeDObjectsParser>()
            .AddSingleton<ObjectsToLoopsConverter>()
            .AddSingleton<ObjectLoopsToSvgConverter>()
            .AddSingleton<KerfApplier>()
            .AddCleanupServices()
            .AddCloseSlotsServices();

        return services;
    }
}
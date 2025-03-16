using Microsoft.Extensions.DependencyInjection;
using Flat3DObjectsToSvgConverter.Services.CleanLoops;
using Flat3DObjectsToSvgConverter.Services.CloseSlots;

namespace Flat3DObjectsToSvgConverter.Services.Parse3dObjects;

public static class ServiceRegistration
{
    public static IServiceCollection AddParse3dObjectsServices(this IServiceCollection services)
    {
        services
            .AddSingleton<ThreeDObjectsParser>()
            .AddSingleton<ObjectsToLoopsConverter>()
            .AddSingleton<ObjectLoopsToSvgConverter>()
            .AddCleanupServices()
            .AddCloseSlotsServices();

        return services;
    }
}
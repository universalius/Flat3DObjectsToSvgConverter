using Microsoft.Extensions.DependencyInjection;

namespace Flat3DObjectsToSvgConverter.Services.CleanLoops;

public static class ServiceRegistration
{
    public static IServiceCollection AddCleanupServices(this IServiceCollection services)
    {
        services
            .AddSingleton<ObjectLoopsAlligner>()
            .AddSingleton<ObjectLoopsPointsReducer>()
            .AddSingleton<ObjectLoopsTinyGapsRemover>()
            .AddSingleton<ObjectLoopsSlotsCutter>()
            .AddSingleton<ObjectLoopsGearsCutter>()
            .AddSingleton<ObjectLoopsCleaner>()
            .AddSingleton<ObjectLoopsSlotSizeReducer>();

        return services;
    }
}
using Microsoft.Extensions.DependencyInjection;

namespace Flat3DObjectsToSvgConverter.Services.CloseSlots;

public static class ServiceRegistration
{
    public static IServiceCollection AddCloseSlotsServices(this IServiceCollection services)
    {
        services
            .AddSingleton<ObjectLoopsSlotsCutter>()
            .AddSingleton<ObjectLoopsGearsCutter>()
            .AddSingleton<ObjectLoopsSlotSizeReducer>();

        return services;
    }
}
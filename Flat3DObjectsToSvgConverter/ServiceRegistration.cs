using Microsoft.Extensions.DependencyInjection;
using Flat3DObjectsToSvgConverter.Services;

namespace Flat3DObjectsToSvgConverter;

public static class ServiceRegistration
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddHostedService<Flat3DObjectsToSvgHostedService>()
            .AddSingleton<ObjectsLabelsToSvgConverter>()
            .AddTransient<ISvgCompactingService, SvgCompactingService>();

        return services;
    }
}
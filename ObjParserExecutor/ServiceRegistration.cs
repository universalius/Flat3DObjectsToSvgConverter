using Microsoft.Extensions.DependencyInjection;
using Plain3DObjectsToSvgConverter.Services;

namespace Plain3DObjectsToSvgConverter;

public static class ServiceRegistration
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddHostedService<Plain3DObjectsToSvgHostedService>()
            .AddSingleton<ObjectsLabelsToSvgConverter>();
        return services;
    }
}
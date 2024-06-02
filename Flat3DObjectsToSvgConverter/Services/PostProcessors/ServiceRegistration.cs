using Microsoft.Extensions.DependencyInjection;

namespace Flat3DObjectsToSvgConverter.Services.PostProcessors;

public static class ServiceRegistration
{
    public static IServiceCollection AddPostProcessorsServices(this IServiceCollection services)
    {
        services
            .AddSingleton<LoopsColorDivider>()
            .AddSingleton<ObjectsLabelsPreciseLocator>()
            .AddSingleton<LoopsTabsGenerator>()
            .AddSingleton<MergeLabelsWithTabsSvg>()
            .AddSingleton<PostProccessors>();

        return services;
    }
}
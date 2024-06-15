using Microsoft.Extensions.DependencyInjection;
using Flat3DObjectsToSvgConverter.Services;
using Microsoft.Extensions.Configuration;
using SvgNest.Models;
using Flat3DObjectsToSvgConverter.Models;
using Flat3DObjectsToSvgConverter.Services.Parse3dObjects;
using Flat3DObjectsToSvgConverter.Services.PostProcessors;

namespace Flat3DObjectsToSvgConverter;

public static class ServiceRegistration
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHostedService<Flat3DObjectsToSvgHostedService>()
            .AddSingleton<SvgCompactingService>()
            .AddSingleton<IOFileService>()
            .AddSingleton<Statistics>()
            .AddParse3dObjectsServices()
            .AddPostProcessorsServices();

        services.AddOptions()
            .Configure<SvgNestConfig>(configuration.GetSection("SvgNest"))
            .Configure<IOSettings>(configuration.GetSection("IO"))
            .Configure<SlotsSettings>(configuration.GetSection("Slots"))
            .Configure<FeaturesSettings>(configuration.GetSection("Features"));

        return services;
    }
}
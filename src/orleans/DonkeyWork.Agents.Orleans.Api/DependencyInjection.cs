using DonkeyWork.Agents.Orleans.Api.Endpoints;
using DonkeyWork.Agents.Orleans.Api.Options;
using DonkeyWork.Agents.Orleans.Core;
using DonkeyWork.Agents.Orleans.Core.Interceptors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Persistence.SeaweedFs.Hosting;

namespace DonkeyWork.Agents.Orleans.Api;

public static class DependencyInjection
{
    public static IHostBuilder AddOrleansApi(this IHostBuilder hostBuilder, IConfiguration configuration)
    {
        var options = configuration.GetSection(OrleansOptions.SectionName).Get<OrleansOptions>() ?? new OrleansOptions();

        hostBuilder.UseOrleans(siloBuilder =>
        {
            siloBuilder.UseLocalhostClustering();

            siloBuilder.Configure<SiloMessagingOptions>(o =>
            {
                o.ResponseTimeout = TimeSpan.FromMinutes(options.ResponseTimeoutMinutes);
            });

            siloBuilder.AddMemoryGrainStorage("PubSubStore");

            siloBuilder.AddSeaweedFsGrainStorage("SeaweedFs", o =>
            {
                o.BaseUrl = options.SeaweedFsBaseUrl;
                o.BasePath = options.SeaweedFsBasePath;
            });

            siloBuilder.AddIncomingGrainCallFilter<GrainContextInterceptor>();
        });

        return hostBuilder;
    }

    public static IServiceCollection AddOrleansServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OrleansOptions>()
            .BindConfiguration(OrleansOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<GrainContext>();

        return services;
    }

    public static IEndpointRouteBuilder MapOrleansEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapConversationWebSocket();
        return endpoints;
    }
}

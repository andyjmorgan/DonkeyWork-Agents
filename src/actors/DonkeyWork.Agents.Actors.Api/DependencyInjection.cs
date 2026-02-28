using DonkeyWork.Agents.Actors.Api.Endpoints;
using DonkeyWork.Agents.Actors.Api.Options;
using DonkeyWork.Agents.Actors.Core;
using DonkeyWork.Agents.Actors.Core.Interceptors;
using DonkeyWork.Agents.Actors.Core.Options;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Clustering.Kubernetes;
using Orleans.Configuration;
using Orleans.Hosting.Kubernetes;

namespace DonkeyWork.Agents.Actors.Api;

using Orleans.Persistence.SeaweedFs.Hosting;

public static class DependencyInjection
{
    public static IHostBuilder AddActorsApi(this IHostBuilder hostBuilder, IConfiguration configuration)
    {
        var options = configuration.GetSection(ActorsOptions.SectionName).Get<ActorsOptions>() ?? new ActorsOptions();

        // Orleans manages its own DI scopes for grain activations, so the default
        // scope validation (enabled in Development) produces false positives for
        // scoped services consumed by grain call filters.
        hostBuilder.UseDefaultServiceProvider(o => o.ValidateScopes = false);

        hostBuilder.UseOrleans((context, siloBuilder) =>
        {
            if (context.HostingEnvironment.IsProduction())
            {
                // UseKubernetesHosting reads ClusterId/ServiceId from pod labels
                // (orleans/clusterId, orleans/serviceId) and sets silo name to the pod name.
                siloBuilder.UseKubernetesHosting();
                siloBuilder.UseKubeMembership();
            }
            else
            {
                // UseLocalhostClustering uses PostConfigure<ClusterOptions> internally,
                // so ClusterId/ServiceId must be passed here rather than via a separate
                // Configure<ClusterOptions> call (which it would override).
                siloBuilder.UseLocalhostClustering(
                    serviceId: "donkeywork-agents",
                    clusterId: "donkeywork-agents");
            }

            siloBuilder.Configure<SiloMessagingOptions>(o =>
            {
                o.ResponseTimeout = TimeSpan.FromMinutes(options.ResponseTimeoutMinutes);
            });

            siloBuilder.AddMemoryGrainStorage(Contracts.StorageProviders.PubSub);

            siloBuilder.AddSeaweedFsGrainStorage(Contracts.StorageProviders.SeaweedFs, o =>
            {
                o.BaseUrl = options.SeaweedFsBaseUrl;
                o.BasePath = options.SeaweedFsBasePath;
            });

            siloBuilder.AddIncomingGrainCallFilter<GrainContextInterceptor>();
        });

        return hostBuilder;
    }

    public static IServiceCollection AddActorsServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ActorsOptions>()
            .BindConfiguration(ActorsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AnthropicOptions>()
            .BindConfiguration("Anthropic")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient();
        services.AddActorsCore();

        return services;
    }

    public static IEndpointRouteBuilder MapActorsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapConversationWebSocket();
        return endpoints;
    }
}

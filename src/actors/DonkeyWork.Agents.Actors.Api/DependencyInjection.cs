using DonkeyWork.Agents.Actors.Api.Endpoints;
using DonkeyWork.Agents.Actors.Api.EventBus;
using DonkeyWork.Agents.Actors.Api.Options;
using DonkeyWork.Agents.Actors.Contracts.Events;
using DonkeyWork.Agents.Actors.Contracts.Services;
using DonkeyWork.Agents.Actors.Core;
using DonkeyWork.Agents.Actors.Core.Interceptors;
using DonkeyWork.Agents.Actors.Core.Options;
using DonkeyWork.Agents.Actors.Core.Services;
using DonkeyWork.Agents.Actors.Core.Tools.Mcp;
using DonkeyWork.Agents.Actors.Core.Tools.Sandbox;
using DonkeyWork.Agents.Common.MessageBus.Transport;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.ObjectStore;
using Orleans.Clustering.Kubernetes;
using Orleans.Configuration;
using Orleans.Hosting.Kubernetes;
using Orleans.Streaming.Nats.Hosting;

namespace DonkeyWork.Agents.Actors.Api;

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

            siloBuilder.AddNatsStream(Contracts.StorageProviders.NatsStream, opts =>
            {
                opts.Url = configuration["Nats:Url"] ?? "nats://localhost:4222";
                opts.StreamName = "orleans-actors";
                opts.Partitions = 4;
                opts.ConsumerName = "donkeywork-silo";
                opts.MaxAge = TimeSpan.FromHours(24);
            });

            siloBuilder.AddIncomingGrainCallFilter<GrainContextInterceptor>();
        });

        return hostBuilder;
    }

    public static IServiceCollection AddActorsServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ActorsOptions>()
            .BindConfiguration(ActorsOptions.SectionName);

        services.AddOptions<AnthropicOptions>()
            .BindConfiguration("Anthropic")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SandboxOptions>()
            .BindConfiguration(SandboxOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var sandboxOptions = configuration.GetSection(SandboxOptions.SectionName).Get<SandboxOptions>();
        if (sandboxOptions is not null)
        {
            services.AddHttpClient<SandboxManagerClient>(client =>
            {
                client.BaseAddress = new Uri(sandboxOptions.ManagerBaseUrl);
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 5;
                options.Retry.Delay = TimeSpan.FromSeconds(2);
                options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(3);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(10);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);
            });

            services.AddHttpClient<McpSandboxManagerClient>(client =>
            {
                client.BaseAddress = new Uri(sandboxOptions.ManagerBaseUrl);
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 5;
                options.Retry.Delay = TimeSpan.FromSeconds(2);
                options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(3);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(10);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);
            });
        }

        services.AddHttpClient();
        services.AddSingleton<IGrainMessageStore, GrainMessageStore>();
        services.AddSingleton<IAgentExecutionRepository, AgentExecutionRepository>();
        services.AddScoped<IAgentExecutionService, AgentExecutionService>();
        services.AddActorsCore();

        RegisterAgentEventBus(services, configuration);

        return services;
    }

    private static void RegisterAgentEventBus(IServiceCollection services, IConfiguration configuration)
    {
        var natsUrl = configuration["Nats:Url"] ?? "nats://localhost:4222";

        services.AddSingleton<NatsConnection>(sp =>
        {
            var log = sp.GetRequiredService<ILogger<AgentEventPublisher>>();
            var conn = new NatsConnection(new NatsOpts { Url = natsUrl });
            ConnectionEventLogger.Wire(conn, log);
            conn.ConnectAsync().GetAwaiter().GetResult();
            return conn;
        });

        services.AddSingleton<INatsJSContext>(sp =>
            new NatsJSContext(sp.GetRequiredService<NatsConnection>()));

        services.AddSingleton<INatsObjContext>(sp =>
            new NatsObjContext(sp.GetRequiredService<INatsJSContext>()));

        services.AddSingleton<IPayloadSerializer, MessagePackPayloadSerializer>();

        services.AddSingleton<PayloadTypeRegistry>(sp =>
        {
            var registry = new PayloadTypeRegistry();
            foreach (var (name, type) in GetAllStreamEventTypes())
                registry.Add(name, type);
            return registry;
        });

        services.AddSingleton<AgentEventPublisher>();
        services.AddSingleton<IAgentEventPublisher>(sp => sp.GetRequiredService<AgentEventPublisher>());
        services.AddSingleton<AgentEventConsumerFactory>();
        services.AddHostedService<AgentEventStreamBootstrap>();
    }

    private static IEnumerable<(string Name, Type Type)> GetAllStreamEventTypes()
    {
        var baseType = typeof(StreamEventBase);
        return baseType.Assembly
            .GetTypes()
            .Where(t => t.IsSealed && baseType.IsAssignableFrom(t))
            .Select(t => (t.Name, t));
    }

    public static IEndpointRouteBuilder MapActorsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapConversationWebSocket();
        endpoints.MapAgentTestEndpoint();
        return endpoints;
    }
}

using DonkeyWork.Agents.Orchestrations.Api.Options;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Execution;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Executors;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Providers;
using DonkeyWork.Agents.Orchestrations.Core.Options;
using DonkeyWork.Agents.Orchestrations.Core.Services;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Providers;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace DonkeyWork.Agents.Orchestrations.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddOrchestrationsApi(this IServiceCollection services)
    {
        // Register options
        services.AddOptions<OrchestrationsOptions>()
            .BindConfiguration(OrchestrationsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<NatsOptions>()
            .BindConfiguration(NatsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register NATS connection and JetStream context as singletons
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<NatsOptions>>().Value;
            var connection = new NatsConnection(new NatsOpts { Url = options.Url });
            connection.ConnectAsync().GetAwaiter().GetResult();
            return connection;
        });

        services.AddSingleton<INatsJSContext>(sp =>
        {
            var connection = sp.GetRequiredService<NatsConnection>();
            var options = sp.GetRequiredService<IOptions<NatsOptions>>().Value;
            var jsContext = new NatsJSContext(connection);

            // Ensure the shared stream exists with wildcard subject
            var config = new StreamConfig(options.StreamName, [$"{options.SubjectPrefix}.>"])
            {
                MaxAge = options.MaxAge
            };

            try
            {
                jsContext.CreateStreamAsync(config).GetAwaiter().GetResult();
            }
            catch (NatsJSApiException ex) when (ex.Error.ErrCode == 10058)
            {
                // Stream already exists — update config
                try
                {
                    jsContext.UpdateStreamAsync(config).GetAwaiter().GetResult();
                }
                catch
                {
                    // Config update not critical
                }
            }

            return jsContext;
        });

        // Register services
        services.AddScoped<IOrchestrationService, OrchestrationService>();
        services.AddScoped<IOrchestrationVersionService, OrchestrationVersionService>();

        services.AddSingleton<IExecutionStreamService>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ExecutionStreamService>>();
            var jsContext = sp.GetRequiredService<INatsJSContext>();
            var options = sp.GetRequiredService<IOptions<NatsOptions>>().Value;
            return new ExecutionStreamService(logger, jsContext, options.StreamName);
        });

        services.AddScoped<IOrchestrationExecutor, OrchestrationExecutor>();

        // Register node schema services
        services.AddSingleton<INodeSchemaGenerator, NodeSchemaGenerator>();
        services.AddSingleton<INodeTypeSchemaService, NodeTypeSchemaService>();
        services.AddSingleton<IMultimodalChatSchemaService, MultimodalChatSchemaService>();

        // Register execution context as scoped (hydrated by orchestrator)
        services.AddScoped<IExecutionContext, Core.Execution.ExecutionContext>();

        // Register template renderer as scoped (uses IExecutionContext)
        services.AddScoped<ITemplateRenderer, TemplateRenderer>();

        // Register stream writer as scoped (one producer per execution)
        services.AddScoped<IExecutionStreamWriter, ExecutionStreamWriter>();

        // Register repositories
        services.AddScoped<IOrchestrationExecutionRepository, OrchestrationExecutionRepository>();

        // Register execution infrastructure
        services.AddSingleton<GraphAnalyzer>();

        // Register node method registry with provider discovery
        services.AddSingleton(sp =>
        {
            var registry = new NodeMethodRegistry();
            // Discover providers from Core assembly
            registry.DiscoverProviders(typeof(HttpNodeProvider).Assembly);
            return registry;
        });

        // Register node providers as scoped (they access scoped IExecutionContext)
        services.AddScoped<HttpNodeProvider>();
        services.AddScoped<TimingNodeProvider>();
        services.AddScoped<UtilityNodeProvider>();

        // Register generic executor for provider-based nodes
        services.AddScoped<GenericNodeExecutor>();

        // Register dedicated executors for complex nodes
        services.AddScoped<StartNodeExecutor>();
        services.AddScoped<EndNodeExecutor>();
        services.AddScoped<ModelNodeExecutor>();
        services.AddScoped<MultimodalChatNodeExecutor>();

        // Register executor registry with mappings
        services.AddScoped<INodeExecutorRegistry>(sp =>
        {
            var registry = new NodeExecutorRegistry(sp);

            // Dedicated executors for flow control and complex nodes
            registry.Register(NodeType.Start, typeof(StartNodeExecutor));
            registry.Register(NodeType.End, typeof(EndNodeExecutor));
            registry.Register(NodeType.Model, typeof(ModelNodeExecutor));
            registry.Register(NodeType.MultimodalChatModel, typeof(MultimodalChatNodeExecutor));

            // Generic executor for provider-based nodes
            registry.Register(NodeType.MessageFormatter, typeof(GenericNodeExecutor));
            registry.Register(NodeType.HttpRequest, typeof(GenericNodeExecutor));
            registry.Register(NodeType.Sleep, typeof(GenericNodeExecutor));

            return registry;
        });

        // Register background service
        services.AddHostedService<StreamCleanupBackgroundService>();

        return services;
    }
}

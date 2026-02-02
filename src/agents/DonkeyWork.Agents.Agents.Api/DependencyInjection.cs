using System.Net;
using DonkeyWork.Agents.Agents.Api.Options;
using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Agents.Core.Execution;
using DonkeyWork.Agents.Agents.Core.Execution.Executors;
using DonkeyWork.Agents.Agents.Core.Execution.Providers;
using DonkeyWork.Agents.Agents.Core.Options;
using DonkeyWork.Agents.Agents.Core.Services;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Providers;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Schema;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Stream.Client;

namespace DonkeyWork.Agents.Agents.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddAgentsApi(this IServiceCollection services)
    {
        // Register options
        services.AddOptions<AgentsOptions>()
            .BindConfiguration(AgentsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RabbitMqStreamOptions>()
            .BindConfiguration(RabbitMqStreamOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register RabbitMQ StreamSystem as singleton
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqStreamOptions>>().Value;

            var config = new StreamSystemConfig
            {
                UserName = options.Username,
                Password = options.Password,
                VirtualHost = options.VirtualHost,
                Endpoints = new List<EndPoint> { new DnsEndPoint(options.Host, options.Port) }
            };

            return StreamSystem.Create(config).GetAwaiter().GetResult();
        });

        // Register services
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IAgentVersionService, AgentVersionService>();
        services.AddSingleton<IExecutionStreamService, ExecutionStreamService>();
        services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();

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
        services.AddScoped<IAgentExecutionRepository, AgentExecutionRepository>();

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

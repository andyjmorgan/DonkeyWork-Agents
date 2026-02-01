using System.Net;
using DonkeyWork.Agents.Agents.Api.Options;
using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Agents.Core.Execution;
using DonkeyWork.Agents.Agents.Core.Execution.Executors;
using DonkeyWork.Agents.Agents.Core.Options;
using DonkeyWork.Agents.Agents.Core.Services;
using DonkeyWork.Agents.Common.Nodes.Schema;
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

        // Register execution context as scoped (hydrated by orchestrator)
        services.AddScoped<IExecutionContext, Core.Execution.ExecutionContext>();

        // Register stream writer as scoped (one producer per execution)
        services.AddScoped<IExecutionStreamWriter, ExecutionStreamWriter>();

        // Register repositories
        services.AddScoped<IAgentExecutionRepository, AgentExecutionRepository>();

        // Register execution infrastructure
        services.AddSingleton<GraphAnalyzer>();
        services.AddScoped<INodeExecutorRegistry>(sp =>
        {
            var registry = new NodeExecutorRegistry(sp);
            registry.Register("start", typeof(StartNodeExecutor));
            registry.Register("model", typeof(ModelNodeExecutor));
            registry.Register("end", typeof(EndNodeExecutor));
            registry.Register("action", typeof(ActionNodeExecutor));
            registry.Register("messageFormatter", typeof(MessageFormatterNodeExecutor));
            return registry;
        });

        // Register node executors as scoped
        services.AddScoped<StartNodeExecutor>();
        services.AddScoped<ModelNodeExecutor>();
        services.AddScoped<EndNodeExecutor>();
        services.AddScoped<ActionNodeExecutor>();
        services.AddScoped<MessageFormatterNodeExecutor>();

        // Register background service
        services.AddHostedService<StreamCleanupBackgroundService>();

        return services;
    }
}

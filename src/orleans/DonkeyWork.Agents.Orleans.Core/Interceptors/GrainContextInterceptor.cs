using DonkeyWork.Agents.Orleans.Contracts.Models;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Serilog.Context;

namespace DonkeyWork.Agents.Orleans.Core.Interceptors;

public sealed class GrainContextInterceptor : IIncomingGrainCallFilter
{
    private readonly GrainContext _grainContext;
    private readonly ILogger<GrainContextInterceptor> _logger;

    public GrainContextInterceptor(GrainContext grainContext, ILogger<GrainContextInterceptor> logger)
    {
        _grainContext = grainContext;
        _logger = logger;
    }

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        var grain = context.Grain;
        var grainKey = grain switch
        {
            Grain g => g.GetPrimaryKeyString(),
            _ => null
        };

        string? agentType = null;

        if (grainKey is not null)
        {
            _grainContext.GrainKey = grainKey;

            // DonkeyWork grain keys include userId: "prefix:userId:conversationId[:taskId]"
            if (grainKey.StartsWith(AgentKeys.ConversationPrefix))
            {
                var parts = grainKey[AgentKeys.ConversationPrefix.Length..].Split(':');
                if (parts.Length >= 1 && Guid.TryParse(parts[0], out var userId))
                    _grainContext.UserId = userId;
                if (parts.Length >= 2)
                    _grainContext.ConversationId = parts[1];
                agentType = "Conversation";
            }
            else if (grainKey.StartsWith(AgentKeys.DelegatePrefix))
            {
                var parts = grainKey[AgentKeys.DelegatePrefix.Length..].Split(':');
                if (parts.Length >= 1 && Guid.TryParse(parts[0], out var userId))
                    _grainContext.UserId = userId;
                if (parts.Length >= 2)
                    _grainContext.ConversationId = parts[1];
                agentType = "Delegate";
            }
            else if (grainKey.StartsWith(AgentKeys.DeepResearchPrefix))
            {
                var parts = grainKey[AgentKeys.DeepResearchPrefix.Length..].Split(':');
                if (parts.Length >= 1 && Guid.TryParse(parts[0], out var userId))
                    _grainContext.UserId = userId;
                if (parts.Length >= 2)
                    _grainContext.ConversationId = parts[1];
                agentType = "DeepResearch";
            }
            else if (grainKey.StartsWith(AgentKeys.ResearchPrefix))
            {
                var parts = grainKey[AgentKeys.ResearchPrefix.Length..].Split(':');
                if (parts.Length >= 1 && Guid.TryParse(parts[0], out var userId))
                    _grainContext.UserId = userId;
                if (parts.Length >= 2)
                    _grainContext.ConversationId = parts[1];
                agentType = "Research";
            }
        }

        using (LogContext.PushProperty("GrainKey", grainKey))
        using (LogContext.PushProperty("ConversationId", _grainContext.ConversationId))
        using (LogContext.PushProperty("AgentType", agentType))
        {
            try
            {
                await context.Invoke();
            }
            catch (Exception ex)
            {
                if (grainKey is not null)
                    _logger.LogError(ex, "Grain {GrainKey} failed {Method}", grainKey, context.ImplementationMethod?.Name ?? "unknown");
                throw;
            }
        }
    }
}

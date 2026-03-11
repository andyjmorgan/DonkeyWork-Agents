using System.Collections.Frozen;
using DonkeyWork.Agents.Actors.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace DonkeyWork.Agents.Actors.Core.Interceptors;

public sealed class GrainContextInterceptor : IIncomingGrainCallFilter
{
    private readonly ILogger<GrainContextInterceptor> _logger;

    private static readonly FrozenDictionary<string, string> PrefixToAgentType =
        new Dictionary<string, string>
        {
            [AgentKeys.ConversationPrefix] = "Conversation",
            [AgentKeys.DelegatePrefix] = "Delegate",
            [AgentKeys.DeepResearchPrefix] = "DeepResearch",
            [AgentKeys.ResearchPrefix] = "Research",
            [AgentKeys.CustomAgentPrefix] = "Custom",
            [AgentKeys.TestPrefix] = "Test",
        }.ToFrozenDictionary();

    public GrainContextInterceptor(ILogger<GrainContextInterceptor> logger)
    {
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

        // Resolve scoped services from the grain's activation scope so we share
        // the same instances the grain and its dependencies (DbContext etc.) use.
        GrainContext? grainContext = null;
        string? agentType = null;

        if (grain is IGrainBase grainBase)
        {
            var services = grainBase.GrainContext.ActivationServices;
            grainContext = services.GetService<GrainContext>();

            if (grainKey is not null && grainContext is not null)
            {
                grainContext.GrainKey = grainKey;

                // Determine agent type from key prefix.
                // Adding a new prefix only requires a new entry in PrefixToAgentType.
                string? matchedPrefix = null;
                foreach (var (prefix, type) in PrefixToAgentType)
                {
                    if (grainKey.StartsWith(prefix))
                    {
                        agentType = type;
                        matchedPrefix = prefix;
                        break;
                    }
                }

                // Hydrate ConversationId: prefer RequestContext, fall back to key parsing.
                // RequestContext is set by the WebSocket handler and by SwarmAgentSpawner
                // before outgoing grain calls. Key parsing handles cases where RequestContext
                // is unavailable (e.g. grain-internal async processing).
                var callerConversationId = RequestContext.Get(GrainCallContextKeys.ConversationId) as string;
                if (callerConversationId is not null)
                {
                    grainContext.ConversationId = callerConversationId;
                }
                else if (matchedPrefix is not null)
                {
                    // All keys follow {prefix}{userId}:{conversationId}[:{rest}]
                    var parts = grainKey[matchedPrefix.Length..].Split(':');
                    if (parts.Length >= 2)
                        grainContext.ConversationId = parts[1];
                }
            }

            // Hydrate IIdentityContext from the caller's RequestContext.
            var callerUserId = RequestContext.Get(GrainCallContextKeys.UserId) as string;
            if (callerUserId is not null && Guid.TryParse(callerUserId, out var callerGuid))
            {
                var identityContext = services.GetService<IIdentityContext>();
                identityContext?.SetIdentity(callerGuid);
            }
        }

        using (LogContext.PushProperty("GrainKey", grainKey))
        using (LogContext.PushProperty("ConversationId", grainContext?.ConversationId))
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

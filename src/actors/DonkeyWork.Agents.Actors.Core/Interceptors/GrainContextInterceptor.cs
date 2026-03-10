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

                if (grainKey.StartsWith(AgentKeys.ConversationPrefix))
                {
                    var parts = grainKey[AgentKeys.ConversationPrefix.Length..].Split(':');
                    if (parts.Length >= 2)
                        grainContext.ConversationId = parts[1];
                    agentType = "Conversation";
                }
                else if (grainKey.StartsWith(AgentKeys.DelegatePrefix))
                {
                    var parts = grainKey[AgentKeys.DelegatePrefix.Length..].Split(':');
                    if (parts.Length >= 2)
                        grainContext.ConversationId = parts[1];
                    agentType = "Delegate";
                }
                else if (grainKey.StartsWith(AgentKeys.DeepResearchPrefix))
                {
                    var parts = grainKey[AgentKeys.DeepResearchPrefix.Length..].Split(':');
                    if (parts.Length >= 2)
                        grainContext.ConversationId = parts[1];
                    agentType = "DeepResearch";
                }
                else if (grainKey.StartsWith(AgentKeys.ResearchPrefix))
                {
                    var parts = grainKey[AgentKeys.ResearchPrefix.Length..].Split(':');
                    if (parts.Length >= 2)
                        grainContext.ConversationId = parts[1];
                    agentType = "Research";
                }
                else if (grainKey.StartsWith(AgentKeys.TestPrefix))
                {
                    var parts = grainKey[AgentKeys.TestPrefix.Length..].Split(':');
                    if (parts.Length >= 2)
                        grainContext.ConversationId = parts[1];
                    agentType = "Test";
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

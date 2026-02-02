using DonkeyWork.Agents.Conversations.Contracts.Services;
using DonkeyWork.Agents.Conversations.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DonkeyWork.Agents.Conversations.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddConversationsApi(this IServiceCollection services)
    {
        // Register services
        services.AddScoped<IConversationService, ConversationService>();

        return services;
    }
}

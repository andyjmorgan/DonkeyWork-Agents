using DonkeyWork.Agents.Prompts.Contracts.Services;
using DonkeyWork.Agents.Prompts.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DonkeyWork.Agents.Prompts.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPromptsApi(this IServiceCollection services)
    {
        services.AddScoped<IPromptService, PromptService>();
        return services;
    }
}

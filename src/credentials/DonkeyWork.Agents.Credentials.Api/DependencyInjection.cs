using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Credentials.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DonkeyWork.Agents.Credentials.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddCredentialsApi(this IServiceCollection services)
    {
        services.AddScoped<IUserApiKeyService, UserApiKeyService>();
        services.AddScoped<IExternalApiKeyService, ExternalApiKeyService>();

        return services;
    }
}

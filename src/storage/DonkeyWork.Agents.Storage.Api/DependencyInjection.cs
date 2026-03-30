using DonkeyWork.Agents.Storage.Contracts.Services;
using DonkeyWork.Agents.Storage.Core.Options;
using DonkeyWork.Agents.Storage.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DonkeyWork.Agents.Storage.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddStorageApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<StorageOptions>()
            .BindConfiguration(StorageOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IS3ClientWrapper, S3ClientWrapper>();

        services.AddScoped<IStorageService, StorageService>();
        services.AddScoped<ISkillsService, SkillsService>();

        return services;
    }
}

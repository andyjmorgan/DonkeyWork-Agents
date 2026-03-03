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
        // Configure options with validation
        services.AddOptions<StorageOptions>()
            .BindConfiguration(StorageOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register S3 client wrapper
        services.AddSingleton<IS3ClientWrapper, S3ClientWrapper>();

        // Register services
        services.AddScoped<IStorageService, StorageService>();

        return services;
    }
}

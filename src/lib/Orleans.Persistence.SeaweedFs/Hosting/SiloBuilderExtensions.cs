using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Persistence.SeaweedFs.Configuration;
using Orleans.Persistence.SeaweedFs.Provider;
using Orleans.Storage;

namespace Orleans.Persistence.SeaweedFs.Hosting;

public static class SiloBuilderExtensions
{
    public static ISiloBuilder AddSeaweedFsGrainStorage(
        this ISiloBuilder builder,
        string name,
        Action<SeaweedFsStorageOptions> configureOptions)
    {
        var options = new SeaweedFsStorageOptions();
        configureOptions(options);

        builder.Services.AddHttpClient(name);
        builder.Services.AddKeyedSingleton<IGrainStorage>(name, (sp, key) =>
            new SeaweedFsGrainStorage(
                name,
                options,
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<ILogger<SeaweedFsGrainStorage>>()));

        return builder;
    }
}

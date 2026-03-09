using Orleans.Configuration;
using Orleans.Streaming.Nats.Configuration;
using Orleans.Streaming.Nats.Provider;

namespace Orleans.Streaming.Nats.Hosting;

public static class SiloBuilderExtensions
{
    public static ISiloBuilder AddNatsStream(
        this ISiloBuilder builder,
        string providerName,
        Action<NatsStreamOptions> configureOptions)
    {
        var options = new NatsStreamOptions();
        configureOptions(options);

        builder.AddPersistentStreams(
            providerName,
            NatsQueueAdapterFactory.Create,
            providerConfigurator =>
            {
                providerConfigurator.Configure<NatsStreamOptions>(ob =>
                    ob.Configure(configureOptions));

                providerConfigurator.Configure<HashRingStreamQueueMapperOptions>(ob =>
                    ob.Configure(mqo => mqo.TotalQueueCount = options.Partitions));
            });

        return builder;
    }
}

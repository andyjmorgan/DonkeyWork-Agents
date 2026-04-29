using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Net;

namespace DonkeyWork.Agents.Common.MessageBus.Transport;

public static class NatsClientFactory
{
    public static NatsClient Create(TransportOptions options, ILogger log)
    {
        var natsOpts = NatsOpts.Default with
        {
            Url = options.NatsUrl,
            RequestTimeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds),
            CommandTimeout = TimeSpan.FromSeconds(options.CommandTimeoutSeconds),
            ConnectTimeout = TimeSpan.FromSeconds(options.ConnectTimeoutSeconds),
            PingInterval = TimeSpan.FromSeconds(options.PingIntervalSeconds),
            MaxPingOut = options.MaxPingOut,
            ReconnectWaitMin = TimeSpan.FromMilliseconds(options.ReconnectWaitMinMs),
            ReconnectWaitMax = TimeSpan.FromMilliseconds(options.ReconnectWaitMaxMs),
        };

        var client = new NatsClient(natsOpts);
        ConnectionEventLogger.Wire(client.Connection, log);
        return client;
    }

    public static INatsJSContext CreateJetStream(NatsClient client)
        => client.CreateJetStreamContext();
}

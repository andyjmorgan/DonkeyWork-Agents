using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace DonkeyWork.Agents.Common.MessageBus.Transport;

public static class ConnectionEventLogger
{
    public static void Wire(INatsConnection conn, ILogger log)
    {
        conn.ConnectionDisconnected += (_, e) =>
        {
            log.LogWarning("disconnected from NATS ({Reason})", e.Message);
            return default;
        };
        conn.ConnectionOpened += (_, e) =>
        {
            log.LogInformation("connected to NATS ({Url})", conn.ServerInfo?.Host ?? "?");
            return default;
        };
        conn.ReconnectFailed += (_, e) =>
        {
            log.LogWarning("reconnect attempt failed ({Reason})", e.Message);
            return default;
        };
    }
}

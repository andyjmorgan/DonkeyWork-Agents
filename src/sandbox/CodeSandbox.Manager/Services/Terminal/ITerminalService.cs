using System.Net.WebSockets;

namespace CodeSandbox.Manager.Services.Terminal;

public interface ITerminalService
{
    Task HandleTerminalSessionAsync(
        string sandboxId,
        WebSocket webSocket,
        CancellationToken cancellationToken = default);

    Task ResizeTerminalAsync(
        string sandboxId,
        int cols,
        int rows,
        CancellationToken cancellationToken = default);
}

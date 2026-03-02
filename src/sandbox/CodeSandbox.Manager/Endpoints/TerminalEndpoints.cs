using CodeSandbox.Manager.Services.Container;
using CodeSandbox.Manager.Services.Terminal;

namespace CodeSandbox.Manager.Endpoints;

public static class TerminalEndpoints
{
    public static void MapTerminalEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/sandbox/{sandboxId}/terminal", HandleTerminalWebSocket)
            .WithTags("Terminal")
            .WithName("TerminalWebSocket")
            .WithSummary("Open a terminal WebSocket to a sandbox")
            .WithDescription("Establishes a WebSocket connection for interactive terminal access to the sandbox container via Kubernetes exec.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task HandleTerminalWebSocket(
        string sandboxId,
        ITerminalService terminalService,
        ISandboxService sandboxService,
        ILogger<Program> logger,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            logger.LogWarning("Non-WebSocket request to terminal endpoint for sandbox {SandboxId}", sandboxId);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("WebSocket connection required", cancellationToken);
            return;
        }

        var container = await sandboxService.GetContainerAsync(sandboxId, cancellationToken);
        if (container == null)
        {
            logger.LogWarning("Sandbox not found for terminal connection: {SandboxId}", sandboxId);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync($"Sandbox {sandboxId} not found", cancellationToken);
            return;
        }

        if (!container.IsReady)
        {
            logger.LogWarning("Sandbox not ready for terminal connection: {SandboxId}", sandboxId);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync($"Sandbox {sandboxId} is not ready", cancellationToken);
            return;
        }

        logger.LogInformation("Accepting WebSocket terminal connection for sandbox {SandboxId}", sandboxId);

        try
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await terminalService.HandleTerminalSessionAsync(sandboxId, webSocket, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Terminal WebSocket error for sandbox {SandboxId}", sandboxId);
        }
    }
}

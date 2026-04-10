using System.Text.Json;
using System.Threading.Channels;
using CodeSandbox.Contracts.Requests;
using CodeSandbox.Contracts.Responses;
using CodeSandbox.Executor.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeSandbox.Executor.Controllers;

[ApiController]
[Route("api/mcp")]
public class McpController : ControllerBase
{
    private readonly StdioBridge bridge;
    private readonly ILogger<McpController> logger;

    public McpController(StdioBridge bridge, ILogger<McpController> logger)
    {
        this.bridge = bridge;
        this.logger = logger;
    }

    [HttpPost("start")]
    public async Task Start([FromBody] McpStartRequest request, CancellationToken ct)
    {
        this.logger.LogInformation(
            "MCP Start called. Command: {Command}, Args: [{Args}], Timeout: {Timeout}s",
            request.Command,
            string.Join(", ", request.Arguments),
            request.TimeoutSeconds);

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";

        var eventChannel = Channel.CreateUnbounded<StdioBridge.StartEvent>();

        var startTask = Task.Run(async () =>
        {
            try
            {
                await this.bridge.StartAsync(
                    request.Command,
                    request.Arguments,
                    request.PreExecScripts,
                    request.TimeoutSeconds > 0 ? request.TimeoutSeconds : 30,
                    request.WorkingDirectory,
                    eventChannel.Writer,
                    ct);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "StdioBridge.StartAsync failed");
            }
            finally
            {
                eventChannel.Writer.TryComplete();
            }
        }, ct);

        await foreach (var evt in eventChannel.Reader.ReadAllAsync(ct))
        {
            var data = JsonSerializer.Serialize(new
            {
                eventType = evt.EventType,
                message = evt.Message,
                stream = evt.Stream,
                exitCode = evt.ExitCode,
                pid = evt.Pid,
                elapsedSeconds = evt.ElapsedSeconds,
            });

            await Response.WriteAsync($"data: {data}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }

        await startTask;
    }

    [HttpPost("proxy")]
    public async Task<McpProxyResponse> Proxy([FromBody] McpProxyRequest request, CancellationToken ct)
    {
        this.logger.LogInformation("MCP ProxyRequest called. Timeout: {Timeout}s", request.TimeoutSeconds);

        var timeoutSeconds = request.TimeoutSeconds > 0 ? request.TimeoutSeconds : 30;
        var response = await this.bridge.SendRequestAsync(request.Body, timeoutSeconds, ct);

        return new McpProxyResponse
        {
            Body = response ?? "",
            IsNotification = response == null,
        };
    }

    [HttpGet("notifications")]
    public async Task Notifications(CancellationToken ct)
    {
        this.logger.LogInformation("MCP StreamNotifications called");

        var reader = this.bridge.GetNotificationReader();
        if (reader is null)
        {
            Response.StatusCode = StatusCodes.Status409Conflict;
            await Response.WriteAsJsonAsync(new { error = "MCP server is not running" }, ct);
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";

        try
        {
            await foreach (var notification in reader.ReadAllAsync(ct))
            {
                await Response.WriteAsync($"data: {notification}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    [HttpPost("response")]
    public async Task<IActionResult> SendResponse([FromBody] McpSendResponseRequest request, CancellationToken ct)
    {
        this.logger.LogInformation("MCP SendResponse called");

        await this.bridge.SendResponseAsync(request.Body, ct);
        return Ok(new { success = true });
    }

    [HttpGet("status")]
    public McpStatusResponse GetStatus()
    {
        var status = this.bridge.GetStatus();

        return new McpStatusResponse
        {
            State = status.State.ToString(),
            Error = status.Error,
            StartedAt = status.StartedAt?.ToString("O"),
            LastRequestAt = status.LastRequestAt?.ToString("O"),
        };
    }

    [HttpDelete]
    public IActionResult Stop()
    {
        this.logger.LogInformation("MCP Stop called");

        this.bridge.Stop();
        return Ok(new { success = true });
    }
}

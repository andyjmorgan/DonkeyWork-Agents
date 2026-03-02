using System.Threading.Channels;
using CodeSandbox.Contracts.Grpc.McpServer;
using Grpc.Core;

namespace CodeSandbox.Executor.Services;

public class McpServerGrpcService : McpServerService.McpServerServiceBase
{
    private readonly StdioBridge _bridge;
    private readonly ILogger<McpServerGrpcService> _logger;

    public McpServerGrpcService(StdioBridge bridge, ILogger<McpServerGrpcService> logger)
    {
        _bridge = bridge;
        _logger = logger;
    }

    public override async Task Start(
        McpStartRequest request,
        IServerStreamWriter<Contracts.Grpc.McpServer.McpStartEvent> responseStream,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "gRPC Start called. Command: {Command}, Args: [{Args}], Timeout: {Timeout}s",
            request.Command,
            string.Join(", ", request.Arguments),
            request.TimeoutSeconds);

        // Create an internal channel for events from StdioBridge
        var eventChannel = Channel.CreateUnbounded<StdioBridge.StartEvent>();

        // Start the bridge in a background task that writes to the channel
        var startTask = Task.Run(async () =>
        {
            try
            {
                await _bridge.StartAsync(
                    request.Command,
                    request.Arguments.ToArray(),
                    request.PreExecScripts.ToArray(),
                    request.TimeoutSeconds > 0 ? request.TimeoutSeconds : 30,
                    request.HasWorkingDirectory ? request.WorkingDirectory : null,
                    eventChannel.Writer,
                    context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StdioBridge.StartAsync failed");
            }
            finally
            {
                eventChannel.Writer.TryComplete();
            }
        }, context.CancellationToken);

        // Stream events to the gRPC client as they arrive
        await foreach (var evt in eventChannel.Reader.ReadAllAsync(context.CancellationToken))
        {
            var grpcEvent = new Contracts.Grpc.McpServer.McpStartEvent
            {
                EventType = evt.EventType,
                Message = evt.Message
            };

            // Set error field for error events
            if (evt.EventType == "error")
            {
                grpcEvent.Error = evt.Message;
            }

            await responseStream.WriteAsync(grpcEvent, context.CancellationToken);
        }

        // Ensure the start task completes (may have thrown)
        await startTask;
    }

    public override async Task<JsonRpcResponse> ProxyRequest(
        JsonRpcRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC ProxyRequest called. Timeout: {Timeout}s", request.TimeoutSeconds);

        var timeoutSeconds = request.TimeoutSeconds > 0 ? request.TimeoutSeconds : 30;
        var response = await _bridge.SendRequestAsync(request.Body, timeoutSeconds, context.CancellationToken);

        return new JsonRpcResponse
        {
            Body = response ?? "",
            IsNotification = response == null
        };
    }

    public override async Task StreamNotifications(
        StreamNotificationsRequest request,
        IServerStreamWriter<JsonRpcNotification> responseStream,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC StreamNotifications called");

        var reader = _bridge.GetNotificationReader();
        if (reader is null)
        {
            _logger.LogWarning("No notification reader available - server not running");
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "MCP server is not running"));
        }

        try
        {
            await foreach (var notification in reader.ReadAllAsync(context.CancellationToken))
            {
                await responseStream.WriteAsync(
                    new JsonRpcNotification { Body = notification },
                    context.CancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected
        }
    }

    public override async Task<SendResponseAck> SendResponse(
        JsonRpcResponseMessage request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC SendResponse called");

        await _bridge.SendResponseAsync(request.Body, context.CancellationToken);
        return new SendResponseAck { Success = true };
    }

    public override Task<McpStatusResponse> GetStatus(
        GetStatusRequest request,
        ServerCallContext context)
    {
        var status = _bridge.GetStatus();

        return Task.FromResult(new McpStatusResponse
        {
            State = status.State.ToString(),
            Error = status.Error ?? "",
            StartedAt = status.StartedAt?.ToString("O") ?? "",
            LastRequestAt = status.LastRequestAt?.ToString("O") ?? ""
        });
    }

    public override Task<StopResponse> Stop(
        StopRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC Stop called");

        _bridge.Stop();
        return Task.FromResult(new StopResponse { Success = true });
    }
}

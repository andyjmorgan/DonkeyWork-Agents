using CodeSandbox.Contracts.Events;
using CodeSandbox.Contracts.Grpc.Executor;
using CodeSandbox.Contracts.Requests;
using Grpc.Core;

namespace CodeSandbox.Executor.Services;

public class ExecutorGrpcService : ExecutorService.ExecutorServiceBase
{
    private readonly ILogger<ExecutorGrpcService> _logger;
    private readonly ProcessTracker _processTracker;

    public ExecutorGrpcService(ILogger<ExecutorGrpcService> logger, ProcessTracker processTracker)
    {
        _logger = logger;
        _processTracker = processTracker;
    }

    public override async Task Execute(
        ExecuteRequest request,
        IServerStreamWriter<Contracts.Grpc.Executor.ExecuteEvent> responseStream,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "gRPC Execute called. Command: {Command}, Timeout: {TimeoutSeconds}s",
            request.Command.Length > 50 ? request.Command[..50] + "..." : request.Command,
            request.TimeoutSeconds);

        var process = new ManagedProcess(request.Command, _processTracker, _logger);
        var tracked = process.Start();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
        var timeoutSeconds = request.TimeoutSeconds > 0 ? request.TimeoutSeconds : 300;
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        bool timedOut = false;

        try
        {
            await foreach (var evt in tracked.ReconnectAsync(timeoutCts.Token))
            {
                await responseStream.WriteAsync(MapEvent(evt), context.CancellationToken);

                if (evt is CompletedEvent)
                {
                    _processTracker.Remove(tracked.Pid);
                }
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !context.CancellationToken.IsCancellationRequested)
        {
            timedOut = true;
        }

        if (timedOut)
        {
            _logger.LogInformation(
                "Process timed out, continuing in background. Pid: {Pid}, TimeoutSeconds: {Timeout}",
                tracked.Pid, timeoutSeconds);

            await responseStream.WriteAsync(new Contracts.Grpc.Executor.ExecuteEvent
            {
                EventType = "timeout",
                Data = "Process timed out but continues running in background",
                Pid = tracked.Pid,
                ExitCode = -1
            }, context.CancellationToken);
        }
    }

    public override async Task ReconnectProcess(
        ReconnectProcessRequest request,
        IServerStreamWriter<Contracts.Grpc.Executor.ExecuteEvent> responseStream,
        ServerCallContext context)
    {
        var tracked = _processTracker.TryGet(request.Pid);
        if (tracked == null)
        {
            _logger.LogWarning("Reconnect requested for unknown PID: {Pid}", request.Pid);
            throw new RpcException(new Status(StatusCode.NotFound, $"Process {request.Pid} not found"));
        }

        _logger.LogInformation(
            "gRPC Reconnecting to tracked process. Pid: {Pid}, Completed: {IsCompleted}, BufferedEvents: {Count}",
            request.Pid, tracked.IsCompleted, tracked.BufferedEventCount);

        await foreach (var evt in tracked.ReconnectAsync(context.CancellationToken))
        {
            await responseStream.WriteAsync(MapEvent(evt), context.CancellationToken);
        }
    }

    public override Task<ListProcessesResponse> ListProcesses(
        ListProcessesRequest request,
        ServerCallContext context)
    {
        var response = new ListProcessesResponse();

        foreach (var tracked in _processTracker.GetAll())
        {
            response.Processes.Add(new ProcessInfo
            {
                Pid = tracked.Pid,
                Command = tracked.Command,
                IsRunning = !tracked.IsCompleted,
                StartedAt = tracked.StartedAt.ToString("O")
            });
        }

        return Task.FromResult(response);
    }

    public override Task<KillProcessResponse> KillProcess(
        KillProcessRequest request,
        ServerCallContext context)
    {
        if (_processTracker.TryRemoveAndKill(request.Pid))
        {
            _logger.LogInformation("gRPC Force killed tracked process. Pid: {Pid}", request.Pid);
            return Task.FromResult(new KillProcessResponse
            {
                Success = true,
                Message = $"Process {request.Pid} killed"
            });
        }

        return Task.FromResult(new KillProcessResponse
        {
            Success = false,
            Message = $"Process {request.Pid} not found"
        });
    }

    private static Contracts.Grpc.Executor.ExecuteEvent MapEvent(ExecutionEvent evt)
    {
        return evt switch
        {
            OutputEvent output => new Contracts.Grpc.Executor.ExecuteEvent
            {
                EventType = "output",
                Data = output.Data,
                Pid = output.Pid
            },
            CompletedEvent completed => new Contracts.Grpc.Executor.ExecuteEvent
            {
                EventType = completed.TimedOut ? "timeout" : "exit",
                Data = completed.ExitCode.ToString(),
                Pid = completed.Pid,
                ExitCode = completed.ExitCode
            },
            _ => new Contracts.Grpc.Executor.ExecuteEvent
            {
                EventType = "error",
                Data = "Unknown event type",
                Pid = evt.Pid
            }
        };
    }
}

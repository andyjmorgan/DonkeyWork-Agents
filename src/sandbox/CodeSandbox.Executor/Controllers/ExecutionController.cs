using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using CodeSandbox.Contracts.Events;
using CodeSandbox.Contracts.Requests;
using CodeSandbox.Contracts.Responses;
using CodeSandbox.Executor.Services;

namespace CodeSandbox.Executor.Controllers;

[ApiController]
[Route("api")]
public class ExecutionController : ControllerBase
{
    private readonly ILogger<ExecutionController> _logger;
    private readonly ProcessTracker _processTracker;

    public ExecutionController(ILogger<ExecutionController> logger, ProcessTracker processTracker)
    {
        _logger = logger;
        _processTracker = processTracker;
    }

    /// <summary>
    /// Execute a bash command and stream results via Server-Sent Events (SSE).
    /// If the command exceeds the timeout, a CompletedEvent with TimedOut=true is sent
    /// and the process continues running in the background. Use the reconnect endpoint
    /// with the returned PID to resume streaming output.
    /// </summary>
    [HttpPost("execute")]
    public IResult Execute(
        [FromBody] ExecuteCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Execute endpoint called. Command: {Command}, Timeout: {TimeoutSeconds}s",
            request.Command.Length > 50 ? request.Command[..50] + "..." : request.Command,
            request.TimeoutSeconds
        );

        return TypedResults.ServerSentEvents(
            StreamEvents(request, cancellationToken));
    }

    /// <summary>
    /// Reconnect to a tracked process's output stream via SSE.
    /// Replays all buffered output since the original stream ended, then continues
    /// streaming live output until the process completes.
    /// </summary>
    [HttpGet("processes/{pid}/reconnect")]
    public IResult Reconnect(int pid, CancellationToken cancellationToken)
    {
        var tracked = _processTracker.TryGet(pid);
        if (tracked == null)
        {
            _logger.LogWarning("Reconnect requested for unknown PID: {Pid}", pid);
            return Results.NotFound(new { error = "Process not found", pid });
        }

        _logger.LogInformation(
            "Reconnecting to tracked process. Pid: {Pid}, Completed: {IsCompleted}, BufferedEvents: {Count}",
            pid,
            tracked.IsCompleted,
            tracked.BufferedEventCount);

        return TypedResults.ServerSentEvents(
            StreamReconnect(tracked, cancellationToken));
    }

    /// <summary>
    /// List all tracked processes (those that timed out or are still running).
    /// </summary>
    [HttpGet("processes")]
    public ActionResult<IReadOnlyList<TrackedProcessInfo>> ListProcesses()
    {
        var processes = _processTracker.GetAll()
            .Select(t => new TrackedProcessInfo
            {
                Pid = t.Pid,
                Command = t.Command,
                StartedAt = t.StartedAt,
                IsCompleted = t.IsCompleted,
                CompletedAt = t.CompletedAt,
                ExitCode = t.ExitCode,
                BufferedEventCount = t.BufferedEventCount
            })
            .ToList();

        return Ok(processes);
    }

    /// <summary>
    /// Force kill a tracked process and remove it from tracking.
    /// </summary>
    [HttpDelete("processes/{pid}")]
    public IActionResult KillProcess(int pid)
    {
        if (_processTracker.TryRemoveAndKill(pid))
        {
            _logger.LogInformation("Force killed tracked process. Pid: {Pid}", pid);
            return Ok(new { message = "Process killed", pid });
        }

        return NotFound(new { error = "Process not found", pid });
    }

    private async IAsyncEnumerable<SseItem<object>> StreamEvents(
        ExecuteCommand request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var process = new ManagedProcess(request.Command, _processTracker, _logger);
        var tracked = process.Start();

        // Create a timeout-aware cancellation that stops streaming but doesn't kill the process
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds));

        bool timedOut = false;

        // Manual enumeration: yield is not allowed in try-catch, but is allowed in try-finally.
        // The inner try-catch (no yield) handles timeout; the outer try-finally (no catch) disposes.
        var enumerator = tracked.ReconnectAsync(timeoutCts.Token).GetAsyncEnumerator();

        try
        {
            while (true)
            {
                bool moved;
                try
                {
                    moved = await enumerator.MoveNextAsync();
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    timedOut = true;
                    break;
                }

                if (!moved) break;

                var evt = enumerator.Current;

                if (evt is OutputEvent outputEvt)
                {
                    _logger.LogInformation(
                        "Streaming output. Pid: {Pid}, Stream: {Stream}, Data: {Data}",
                        outputEvt.Pid,
                        outputEvt.Stream,
                        outputEvt.Data
                    );

                    yield return new SseItem<object>(outputEvt, nameof(OutputEvent))
                    {
                        EventId = Guid.NewGuid().ToString()
                    };
                }
                else if (evt is CompletedEvent completedEvt)
                {
                    _logger.LogInformation(
                        "Process completed. Pid: {Pid}, ExitCode: {ExitCode}, TimedOut: {TimedOut}",
                        completedEvt.Pid,
                        completedEvt.ExitCode,
                        completedEvt.TimedOut
                    );

                    yield return new SseItem<object>(completedEvt, nameof(CompletedEvent))
                    {
                        EventId = Guid.NewGuid().ToString()
                    };

                    // Process completed within timeout - clean up from tracker
                    _processTracker.Remove(tracked.Pid);
                }
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }

        if (timedOut)
        {
            _logger.LogInformation(
                "Process timed out, continuing in background. Pid: {Pid}, TimeoutSeconds: {Timeout}",
                tracked.Pid,
                request.TimeoutSeconds);

            yield return new SseItem<object>(
                new CompletedEvent
                {
                    Pid = tracked.Pid,
                    ExitCode = -1,
                    TimedOut = true
                },
                nameof(CompletedEvent))
            {
                EventId = Guid.NewGuid().ToString()
            };
        }
    }

    private async IAsyncEnumerable<SseItem<object>> StreamReconnect(
        TrackedProcess tracked,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var evt in tracked.ReconnectAsync(cancellationToken))
        {
            if (evt is OutputEvent outputEvt)
            {
                _logger.LogInformation(
                    "Reconnect streaming output. Pid: {Pid}, Stream: {Stream}, Data: {Data}",
                    outputEvt.Pid,
                    outputEvt.Stream,
                    outputEvt.Data
                );

                yield return new SseItem<object>(outputEvt, nameof(OutputEvent))
                {
                    EventId = Guid.NewGuid().ToString()
                };
            }
            else if (evt is CompletedEvent completedEvt)
            {
                _logger.LogInformation(
                    "Reconnect process completed. Pid: {Pid}, ExitCode: {ExitCode}",
                    completedEvt.Pid,
                    completedEvt.ExitCode
                );

                yield return new SseItem<object>(completedEvt, nameof(CompletedEvent))
                {
                    EventId = Guid.NewGuid().ToString()
                };
            }
        }
    }
}

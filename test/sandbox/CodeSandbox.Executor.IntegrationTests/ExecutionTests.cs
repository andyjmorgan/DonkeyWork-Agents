using CodeSandbox.Contracts.Grpc.Executor;
using Grpc.Core;
using Xunit;

namespace CodeSandbox.Executor.IntegrationTests;

[Trait("Category", "Integration")]
public class ExecutionTests : IClassFixture<ServerFixture>
{
    private readonly ExecutorService.ExecutorServiceClient _client;

    public ExecutionTests(ServerFixture fixture)
    {
        _client = new ExecutorService.ExecutorServiceClient(fixture.GrpcChannel);
    }

    private async Task<List<ExecuteEvent>> ExecuteCommandAsync(string command, int timeoutSeconds = 30)
    {
        var request = new ExecuteRequest { Command = command, TimeoutSeconds = timeoutSeconds };
        using var call = _client.Execute(request, deadline: DateTime.UtcNow.AddSeconds(timeoutSeconds + 10));

        var events = new List<ExecuteEvent>();
        await foreach (var evt in call.ResponseStream.ReadAllAsync())
        {
            events.Add(evt);
        }

        return events;
    }

    private async Task<List<ExecuteEvent>> ReconnectAsync(int pid, int timeoutSeconds = 30)
    {
        var request = new ReconnectProcessRequest { Pid = pid };
        using var call = _client.ReconnectProcess(request, deadline: DateTime.UtcNow.AddSeconds(timeoutSeconds));

        var events = new List<ExecuteEvent>();
        await foreach (var evt in call.ResponseStream.ReadAllAsync())
        {
            events.Add(evt);
        }

        return events;
    }

    private async Task<ListProcessesResponse> ListProcessesAsync()
    {
        return await _client.ListProcessesAsync(new ListProcessesRequest());
    }

    private async Task<KillProcessResponse> KillProcessAsync(int pid)
    {
        return await _client.KillProcessAsync(new KillProcessRequest { Pid = pid });
    }

    [Fact]
    public async Task ExecuteAsync_SimpleEchoCommand_ReturnsOutput()
    {
        var events = await ExecuteCommandAsync("echo 'Hello, World!'");

        var outputEvents = events.Where(e => e.EventType == "output").ToList();
        var completedEvent = events.Single(e => e.EventType == "exit");

        Assert.NotEmpty(outputEvents);
        Assert.Contains(outputEvents, e => e.Data.Contains("Hello, World!"));
        Assert.True(completedEvent.Pid > 0);
        Assert.Equal(0, completedEvent.ExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_MultiLineOutput_CapturesAllLines()
    {
        var events = await ExecuteCommandAsync("echo 'Line 1'; sleep 1; echo 'Line 2'; sleep 1; echo 'Line 3'");

        var outputLines = events
            .Where(e => e.EventType == "output" && e.Stream == "stdout")
            .Select(e => e.Data)
            .ToList();
        var completedEvent = events.Single(e => e.EventType == "exit");

        Assert.Equal(0, completedEvent.ExitCode);
        Assert.Contains(outputLines, l => l.Contains("Line 1"));
        Assert.Contains(outputLines, l => l.Contains("Line 2"));
        Assert.Contains(outputLines, l => l.Contains("Line 3"));
    }

    [Fact]
    public async Task ExecuteAsync_StderrOutput_CapturesErrorStream()
    {
        var events = await ExecuteCommandAsync("echo 'This is an error' >&2");

        var errorLines = events
            .Where(e => e.EventType == "output" && e.Stream == "stderr")
            .Select(e => e.Data)
            .ToList();

        Assert.Contains(errorLines, l => l.Contains("This is an error"));
    }

    [Fact]
    public async Task ExecuteAsync_NonZeroExitCode_ReturnsCorrectExitCode()
    {
        var events = await ExecuteCommandAsync("exit 42");

        var completedEvent = events.Single(e => e.EventType == "exit");

        Assert.Equal(42, completedEvent.ExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleProcesses_RunConcurrently()
    {
        var task1 = CollectResultAsync("sleep 2; echo 'Process A done'");
        var task2 = CollectResultAsync("sleep 1; echo 'Process B done'");
        var task3 = CollectResultAsync("sleep 3; echo 'Process C done'");

        var results = await Task.WhenAll(task1, task2, task3);

        Assert.Equal(3, results.Length);
        Assert.All(results, r => Assert.Equal(0, r.ExitCode));
        Assert.Contains("Process A done", results[0].Stdout);
        Assert.Contains("Process B done", results[1].Stdout);
        Assert.Contains("Process C done", results[2].Stdout);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleStderr_CapturesAllErrors()
    {
        var events = await ExecuteCommandAsync("echo 'Error 1' >&2; sleep 1; echo 'Error 2' >&2; sleep 1; echo 'Error 3' >&2");

        var errorLines = events
            .Where(e => e.EventType == "output" && e.Stream == "stderr")
            .Select(e => e.Data)
            .ToList();

        Assert.Contains(errorLines, l => l.Contains("Error 1"));
        Assert.Contains(errorLines, l => l.Contains("Error 2"));
        Assert.Contains(errorLines, l => l.Contains("Error 3"));
    }

    [Fact]
    public async Task ExecuteAsync_MixedStdoutStderr_CapturesBothStreams()
    {
        var events = await ExecuteCommandAsync("echo 'stdout message'; echo 'stderr message' >&2");

        var stdoutLines = events
            .Where(e => e.EventType == "output" && e.Stream == "stdout")
            .Select(e => e.Data)
            .ToList();
        var stderrLines = events
            .Where(e => e.EventType == "output" && e.Stream == "stderr")
            .Select(e => e.Data)
            .ToList();

        Assert.Contains(stdoutLines, l => l.Contains("stdout message"));
        Assert.Contains(stderrLines, l => l.Contains("stderr message"));
    }

    [Fact]
    public async Task ExecuteAsync_ContainsPidInformation()
    {
        var events = await ExecuteCommandAsync("echo testmarker123");

        var outputEvent = events.FirstOrDefault(e => e.EventType == "output" && e.Data.Contains("testmarker123"));
        var completedEvent = events.Single(e => e.EventType == "exit");

        Assert.NotNull(outputEvent);
        Assert.Equal(completedEvent.Pid, outputEvent!.Pid);
        Assert.Equal("stdout", outputEvent.Stream);
    }

    [Fact]
    public async Task ExecuteAsync_CompletedEvent_ContainsCorrectExitCode()
    {
        var events = await ExecuteCommandAsync("exit 7");

        var completedEvent = events.Single(e => e.EventType == "exit");

        Assert.True(completedEvent.Pid > 0);
        Assert.Equal(7, completedEvent.ExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_VeryLongOutput_CapturesEverything()
    {
        var events = await ExecuteCommandAsync("for i in $(seq 1 50); do echo \"Line $i\"; sleep 0.01; done");

        var outputLines = events
            .Where(e => e.EventType == "output" && e.Stream == "stdout")
            .Select(e => e.Data)
            .ToList();
        var completedEvent = events.Single(e => e.EventType == "exit");

        Assert.Equal(0, completedEvent.ExitCode);
        Assert.Contains(outputLines, l => l.Contains("Line 1"));
        Assert.Contains(outputLines, l => l.Contains("Line 25"));
        Assert.Contains(outputLines, l => l.Contains("Line 50"));
    }

    [Fact]
    public async Task ExecuteAsync_EmptyCommand_HandlesGracefully()
    {
        var events = await ExecuteCommandAsync("");

        var completedEvent = events.Single(e => e.EventType == "exit");

        Assert.Equal(0, completedEvent.ExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_CommandWithEnvironmentVariables_UsesVariables()
    {
        var events = await ExecuteCommandAsync("TEST_VAR=hello; echo $TEST_VAR");

        var outputLines = events
            .Where(e => e.EventType == "output" && e.Stream == "stdout")
            .Select(e => e.Data)
            .ToList();

        Assert.Contains(outputLines, l => l.Contains("hello"));
    }

    [Fact]
    public async Task ExecuteAsync_PipedCommands_ExecutesCorrectly()
    {
        var events = await ExecuteCommandAsync("echo hello world | grep world");

        var outputLines = events
            .Where(e => e.EventType == "output" && e.Stream == "stdout")
            .Select(e => e.Data)
            .ToList();

        Assert.Contains(outputLines, l => l.Contains("hello world"));
    }

    [Fact]
    public async Task ExecuteAsync_BackgroundProcess_CompletesSuccessfully()
    {
        var events = await ExecuteCommandAsync("(sleep 1 &); echo 'done'");

        var outputLines = events
            .Where(e => e.EventType == "output" && e.Stream == "stdout")
            .Select(e => e.Data)
            .ToList();
        var completedEvent = events.Single(e => e.EventType == "exit");

        Assert.Equal(0, completedEvent.ExitCode);
        Assert.Contains(outputLines, l => l.Contains("done"));
    }

    [Fact]
    public async Task ExecuteAsync_Timeout_ReturnsTimedOutWithPid()
    {
        // Command runs for 10s but timeout is 3s
        var events = await ExecuteCommandAsync(
            "echo 'before timeout'; sleep 10; echo 'after timeout'",
            timeoutSeconds: 3);

        var outputEvents = events.Where(e => e.EventType == "output").ToList();
        var timeoutEvent = events.Single(e => e.EventType == "timeout");

        // Should have received output before timeout
        Assert.Contains(outputEvents, e => e.Data.Contains("before timeout"));

        // Should report timeout with a valid PID
        Assert.True(timeoutEvent.Pid > 0);
        Assert.Equal(-1, timeoutEvent.ExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_Timeout_ProcessAppearsInTrackedList()
    {
        // Start a long-running command with a short timeout
        var events = await ExecuteCommandAsync(
            "echo 'tracked process'; sleep 30",
            timeoutSeconds: 2);

        var timeoutEvent = events.Single(e => e.EventType == "timeout");

        // The process should appear in the tracked processes list
        var trackedProcesses = await ListProcessesAsync();
        var tracked = trackedProcesses.Processes.FirstOrDefault(p => p.Pid == timeoutEvent.Pid);

        Assert.NotNull(tracked);
        Assert.Contains("tracked process", tracked!.Command);
        Assert.True(tracked.IsRunning);

        // Clean up - kill the process
        await KillProcessAsync(timeoutEvent.Pid);
    }

    [Fact]
    public async Task ExecuteAsync_Timeout_ReconnectGetsRemainingOutput()
    {
        // Command: outputs lines every second for 8 seconds, timeout at 3 seconds
        var events = await ExecuteCommandAsync(
            "for i in $(seq 1 8); do echo \"tick $i\"; sleep 1; done",
            timeoutSeconds: 3);

        var timeoutEvent = events.Single(e => e.EventType == "timeout");
        var pid = timeoutEvent.Pid;

        // Wait a moment for more output to be buffered
        await Task.Delay(2000);

        // Reconnect to get remaining output
        var reconnectEvents = await ReconnectAsync(pid, timeoutSeconds: 15);

        var reconnectOutput = reconnectEvents
            .Where(e => e.EventType == "output")
            .Select(e => e.Data)
            .ToList();
        var reconnectCompleted = reconnectEvents.SingleOrDefault(e => e.EventType == "exit");

        // Should have output lines that came after the timeout
        Assert.NotEmpty(reconnectOutput);

        // The process should eventually complete
        Assert.NotNull(reconnectCompleted);
        Assert.Equal(0, reconnectCompleted!.ExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_Timeout_ReconnectAfterCompletion_ReplaysBuffer()
    {
        // Command: runs for 5 seconds with output, timeout at 2 seconds
        var events = await ExecuteCommandAsync(
            "echo 'line A'; sleep 1; echo 'line B'; sleep 1; echo 'line C'; sleep 1; echo 'line D'",
            timeoutSeconds: 2);

        var timeoutEvent = events.Single(e => e.EventType == "timeout");
        var pid = timeoutEvent.Pid;

        // Wait for the process to fully complete
        await Task.Delay(6000);

        // Reconnect after the process has already completed
        var reconnectEvents = await ReconnectAsync(pid, timeoutSeconds: 10);

        var reconnectOutput = reconnectEvents
            .Where(e => e.EventType == "output")
            .Select(e => e.Data)
            .ToList();
        var reconnectCompleted = reconnectEvents.SingleOrDefault(e => e.EventType == "exit");

        // Should have received the remaining output from the buffer
        Assert.NotEmpty(reconnectOutput);

        // Should have the real completion event
        Assert.NotNull(reconnectCompleted);
        Assert.Equal(0, reconnectCompleted!.ExitCode);
    }

    [Fact]
    public async Task KillProcess_ForcesTrackedProcessToComplete()
    {
        // Start a very long-running command
        var events = await ExecuteCommandAsync(
            "echo 'will be killed'; sleep 300",
            timeoutSeconds: 2);

        var timeoutEvent = events.Single(e => e.EventType == "timeout");
        var pid = timeoutEvent.Pid;

        // Verify it's tracked
        var tracked = await ListProcessesAsync();
        Assert.Contains(tracked.Processes, p => p.Pid == pid);

        // Kill it
        var killResponse = await KillProcessAsync(pid);
        Assert.True(killResponse.Success);

        // Should no longer be in the tracked list
        await Task.Delay(1000);
        tracked = await ListProcessesAsync();
        Assert.DoesNotContain(tracked.Processes, p => p.Pid == pid);
    }

    [Fact]
    public async Task KillProcess_UnknownPid_ReturnsFalse()
    {
        var response = await KillProcessAsync(99999);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task ReconnectProcess_UnknownPid_ThrowsNotFound()
    {
        var ex = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await ReconnectAsync(99999);
        });

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    private async Task<(int Pid, int ExitCode, string Stdout, string Stderr)> CollectResultAsync(string command)
    {
        var events = await ExecuteCommandAsync(command);

        var stdout = string.Join('\n', events
            .Where(e => e.EventType == "output" && e.Stream == "stdout")
            .Select(e => e.Data));
        var stderr = string.Join('\n', events
            .Where(e => e.EventType == "output" && e.Stream == "stderr")
            .Select(e => e.Data));
        var completedEvent = events.SingleOrDefault(e => e.EventType == "exit");

        return (completedEvent?.Pid ?? 0, completedEvent?.ExitCode ?? -1, stdout, stderr);
    }
}

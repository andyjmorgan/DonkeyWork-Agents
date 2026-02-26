using System.Net.Http.Json;
using System.Text.Json;
using CodeSandbox.Contracts.Events;
using CodeSandbox.Contracts.Requests;
using CodeSandbox.Contracts.Responses;
using Xunit;

namespace CodeSandbox.Executor.IntegrationTests;

public class ExecutionTests : IClassFixture<ServerFixture>
{
    private readonly ServerFixture _fixture;
    private readonly JsonSerializerOptions _jsonOptions;

    public ExecutionTests(ServerFixture fixture)
    {
        _fixture = fixture;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private async Task<List<ExecutionEvent>> ExecuteCommandAsync(string command, int timeoutSeconds = 30)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds + 10);

        var request = new ExecuteCommand { Command = command, TimeoutSeconds = timeoutSeconds };
        var response = await httpClient.PostAsJsonAsync($"{_fixture.ServerUrl}/api/execute", request);
        response.EnsureSuccessStatusCode();

        var events = new List<ExecutionEvent>();
        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (line.StartsWith("data: "))
            {
                var json = line["data: ".Length..];
                var evt = JsonSerializer.Deserialize<ExecutionEvent>(json, _jsonOptions);
                if (evt != null)
                {
                    events.Add(evt);
                    if (evt is CompletedEvent)
                    {
                        break;
                    }
                }
            }
        }

        return events;
    }

    private async Task<List<ExecutionEvent>> ReconnectAsync(int pid, int timeoutSeconds = 30)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        var response = await httpClient.GetAsync(
            $"{_fixture.ServerUrl}/api/processes/{pid}/reconnect",
            HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var events = new List<ExecutionEvent>();
        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (line.StartsWith("data: "))
            {
                var json = line["data: ".Length..];
                var evt = JsonSerializer.Deserialize<ExecutionEvent>(json, _jsonOptions);
                if (evt != null)
                {
                    events.Add(evt);
                    if (evt is CompletedEvent)
                    {
                        break;
                    }
                }
            }
        }

        return events;
    }

    private async Task<List<TrackedProcessInfo>> ListProcessesAsync()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"{_fixture.ServerUrl}/api/processes");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<TrackedProcessInfo>>(_jsonOptions)
               ?? [];
    }

    private async Task<bool> KillProcessAsync(int pid)
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.DeleteAsync($"{_fixture.ServerUrl}/api/processes/{pid}");
        return response.IsSuccessStatusCode;
    }

    [Fact]
    public async Task ExecuteAsync_SimpleEchoCommand_ReturnsOutput()
    {
        var events = await ExecuteCommandAsync("echo 'Hello, World!'");

        var outputEvents = events.OfType<OutputEvent>().ToList();
        var completedEvent = events.OfType<CompletedEvent>().Single();

        Assert.NotEmpty(outputEvents);
        Assert.Contains(outputEvents, e => e.Data.Contains("Hello, World!"));
        Assert.True(completedEvent.Pid > 0);
        Assert.Equal(0, completedEvent.ExitCode);
        Assert.False(completedEvent.TimedOut);
    }

    [Fact]
    public async Task ExecuteAsync_MultiLineOutput_CapturesAllLines()
    {
        var events = await ExecuteCommandAsync("echo 'Line 1'; sleep 1; echo 'Line 2'; sleep 1; echo 'Line 3'");

        var outputLines = events.OfType<OutputEvent>()
            .Where(e => e.Stream == OutputStreamType.Stdout)
            .Select(e => e.Data)
            .ToList();
        var completedEvent = events.OfType<CompletedEvent>().Single();

        Assert.Equal(0, completedEvent.ExitCode);
        Assert.Contains(outputLines, l => l.Contains("Line 1"));
        Assert.Contains(outputLines, l => l.Contains("Line 2"));
        Assert.Contains(outputLines, l => l.Contains("Line 3"));
    }

    [Fact]
    public async Task ExecuteAsync_StderrOutput_CapturesErrorStream()
    {
        var events = await ExecuteCommandAsync("echo 'This is an error' >&2");

        var errorLines = events.OfType<OutputEvent>()
            .Where(e => e.Stream == OutputStreamType.Stderr)
            .Select(e => e.Data)
            .ToList();

        Assert.Contains(errorLines, l => l.Contains("This is an error"));
    }

    [Fact]
    public async Task ExecuteAsync_NonZeroExitCode_ReturnsCorrectExitCode()
    {
        var events = await ExecuteCommandAsync("exit 42");

        var completedEvent = events.OfType<CompletedEvent>().Single();

        Assert.Equal(42, completedEvent.ExitCode);
        Assert.False(completedEvent.TimedOut);
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

        var errorLines = events.OfType<OutputEvent>()
            .Where(e => e.Stream == OutputStreamType.Stderr)
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

        var stdoutLines = events.OfType<OutputEvent>()
            .Where(e => e.Stream == OutputStreamType.Stdout)
            .Select(e => e.Data)
            .ToList();
        var stderrLines = events.OfType<OutputEvent>()
            .Where(e => e.Stream == OutputStreamType.Stderr)
            .Select(e => e.Data)
            .ToList();

        Assert.Contains(stdoutLines, l => l.Contains("stdout message"));
        Assert.Contains(stderrLines, l => l.Contains("stderr message"));
    }

    [Fact]
    public async Task ExecuteAsync_ContainsPidInformation()
    {
        var events = await ExecuteCommandAsync("echo testmarker123");

        var outputEvent = events.OfType<OutputEvent>().FirstOrDefault(e => e.Data.Contains("testmarker123"));
        var completedEvent = events.OfType<CompletedEvent>().Single();

        Assert.NotNull(outputEvent);
        Assert.NotNull(completedEvent);
        Assert.Equal(completedEvent.Pid, outputEvent!.Pid);
        Assert.Equal(OutputStreamType.Stdout, outputEvent.Stream);
    }

    [Fact]
    public async Task ExecuteAsync_CompletedEvent_ContainsCorrectExitCode()
    {
        var events = await ExecuteCommandAsync("exit 7");

        var completedEvent = events.OfType<CompletedEvent>().Single();

        Assert.True(completedEvent.Pid > 0);
        Assert.Equal(7, completedEvent.ExitCode);
        Assert.False(completedEvent.TimedOut);
    }

    [Fact]
    public async Task ExecuteAsync_VeryLongOutput_CapturesEverything()
    {
        var events = await ExecuteCommandAsync("for i in $(seq 1 50); do echo \"Line $i\"; sleep 0.01; done");

        var outputLines = events.OfType<OutputEvent>()
            .Where(e => e.Stream == OutputStreamType.Stdout)
            .Select(e => e.Data)
            .ToList();
        var completedEvent = events.OfType<CompletedEvent>().Single();

        Assert.Equal(0, completedEvent.ExitCode);
        Assert.Contains(outputLines, l => l.Contains("Line 1"));
        Assert.Contains(outputLines, l => l.Contains("Line 25"));
        Assert.Contains(outputLines, l => l.Contains("Line 50"));
    }

    [Fact]
    public async Task ExecuteAsync_EmptyCommand_HandlesGracefully()
    {
        var events = await ExecuteCommandAsync("");

        var completedEvent = events.OfType<CompletedEvent>().Single();

        Assert.Equal(0, completedEvent.ExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_CommandWithEnvironmentVariables_UsesVariables()
    {
        var events = await ExecuteCommandAsync("TEST_VAR=hello; echo $TEST_VAR");

        var outputLines = events.OfType<OutputEvent>()
            .Where(e => e.Stream == OutputStreamType.Stdout)
            .Select(e => e.Data)
            .ToList();

        Assert.Contains(outputLines, l => l.Contains("hello"));
    }

    [Fact]
    public async Task ExecuteAsync_PipedCommands_ExecutesCorrectly()
    {
        var events = await ExecuteCommandAsync("echo hello world | grep world");

        var outputLines = events.OfType<OutputEvent>()
            .Where(e => e.Stream == OutputStreamType.Stdout)
            .Select(e => e.Data)
            .ToList();

        Assert.Contains(outputLines, l => l.Contains("hello world"));
    }

    [Fact]
    public async Task ExecuteAsync_BackgroundProcess_CompletesSuccessfully()
    {
        var events = await ExecuteCommandAsync("(sleep 1 &); echo 'done'");

        var outputLines = events.OfType<OutputEvent>()
            .Where(e => e.Stream == OutputStreamType.Stdout)
            .Select(e => e.Data)
            .ToList();
        var completedEvent = events.OfType<CompletedEvent>().Single();

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

        var outputEvents = events.OfType<OutputEvent>().ToList();
        var completedEvent = events.OfType<CompletedEvent>().Single();

        // Should have received output before timeout
        Assert.Contains(outputEvents, e => e.Data.Contains("before timeout"));

        // Should report timeout with a valid PID for reconnect
        Assert.True(completedEvent.TimedOut);
        Assert.True(completedEvent.Pid > 0);
        Assert.Equal(-1, completedEvent.ExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_Timeout_ProcessAppearsInTrackedList()
    {
        // Start a long-running command with a short timeout
        var events = await ExecuteCommandAsync(
            "echo 'tracked process'; sleep 30",
            timeoutSeconds: 2);

        var completedEvent = events.OfType<CompletedEvent>().Single();
        Assert.True(completedEvent.TimedOut);

        // The process should appear in the tracked processes list
        var trackedProcesses = await ListProcessesAsync();
        var tracked = trackedProcesses.FirstOrDefault(p => p.Pid == completedEvent.Pid);

        Assert.NotNull(tracked);
        Assert.Contains("tracked process", tracked!.Command);
        Assert.False(tracked.IsCompleted);

        // Clean up - kill the process
        await KillProcessAsync(completedEvent.Pid);
    }

    [Fact]
    public async Task ExecuteAsync_Timeout_ReconnectGetsRemainingOutput()
    {
        // Command: outputs lines every second for 8 seconds, timeout at 3 seconds
        var events = await ExecuteCommandAsync(
            "for i in $(seq 1 8); do echo \"tick $i\"; sleep 1; done",
            timeoutSeconds: 3);

        var completedEvent = events.OfType<CompletedEvent>().Single();
        Assert.True(completedEvent.TimedOut);

        var pid = completedEvent.Pid;

        // Wait a moment for more output to be buffered
        await Task.Delay(2000);

        // Reconnect to get remaining output
        var reconnectEvents = await ReconnectAsync(pid, timeoutSeconds: 15);

        var reconnectOutput = reconnectEvents.OfType<OutputEvent>()
            .Select(e => e.Data)
            .ToList();
        var reconnectCompleted = reconnectEvents.OfType<CompletedEvent>().SingleOrDefault();

        // Should have output lines that came after the timeout
        Assert.NotEmpty(reconnectOutput);

        // The process should eventually complete
        Assert.NotNull(reconnectCompleted);
        Assert.False(reconnectCompleted!.TimedOut);
        Assert.Equal(0, reconnectCompleted.ExitCode);
    }

    [Fact]
    public async Task ExecuteAsync_Timeout_ReconnectAfterCompletion_ReplaysBuffer()
    {
        // Command: runs for 5 seconds with output, timeout at 2 seconds
        var events = await ExecuteCommandAsync(
            "echo 'line A'; sleep 1; echo 'line B'; sleep 1; echo 'line C'; sleep 1; echo 'line D'",
            timeoutSeconds: 2);

        var completedEvent = events.OfType<CompletedEvent>().Single();
        Assert.True(completedEvent.TimedOut);

        var pid = completedEvent.Pid;

        // Wait for the process to fully complete
        await Task.Delay(6000);

        // Reconnect after the process has already completed
        var reconnectEvents = await ReconnectAsync(pid, timeoutSeconds: 10);

        var reconnectOutput = reconnectEvents.OfType<OutputEvent>()
            .Select(e => e.Data)
            .ToList();
        var reconnectCompleted = reconnectEvents.OfType<CompletedEvent>().SingleOrDefault();

        // Should have received the remaining output from the buffer
        Assert.NotEmpty(reconnectOutput);

        // Should have the real completion event
        Assert.NotNull(reconnectCompleted);
        Assert.Equal(0, reconnectCompleted!.ExitCode);
        Assert.False(reconnectCompleted.TimedOut);
    }

    [Fact]
    public async Task KillProcess_ForcesTrackedProcessToComplete()
    {
        // Start a very long-running command
        var events = await ExecuteCommandAsync(
            "echo 'will be killed'; sleep 300",
            timeoutSeconds: 2);

        var completedEvent = events.OfType<CompletedEvent>().Single();
        Assert.True(completedEvent.TimedOut);

        var pid = completedEvent.Pid;

        // Verify it's tracked
        var tracked = await ListProcessesAsync();
        Assert.Contains(tracked, p => p.Pid == pid);

        // Kill it
        var killed = await KillProcessAsync(pid);
        Assert.True(killed);

        // Should no longer be in the tracked list
        await Task.Delay(1000);
        tracked = await ListProcessesAsync();
        Assert.DoesNotContain(tracked, p => p.Pid == pid);
    }

    [Fact]
    public async Task KillProcess_UnknownPid_ReturnsNotFound()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.DeleteAsync($"{_fixture.ServerUrl}/api/processes/99999");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReconnectProcess_UnknownPid_ReturnsNotFound()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"{_fixture.ServerUrl}/api/processes/99999/reconnect");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<(int Pid, int ExitCode, string Stdout, string Stderr)> CollectResultAsync(string command)
    {
        var events = await ExecuteCommandAsync(command);

        var stdout = string.Join('\n', events.OfType<OutputEvent>()
            .Where(e => e.Stream == OutputStreamType.Stdout)
            .Select(e => e.Data));
        var stderr = string.Join('\n', events.OfType<OutputEvent>()
            .Where(e => e.Stream == OutputStreamType.Stderr)
            .Select(e => e.Data));
        var completedEvent = events.OfType<CompletedEvent>().SingleOrDefault();

        return (completedEvent?.Pid ?? 0, completedEvent?.ExitCode ?? -1, stdout, stderr);
    }
}

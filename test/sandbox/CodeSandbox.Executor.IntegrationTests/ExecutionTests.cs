using System.Net.Http.Json;
using CodeSandbox.Contracts.Requests.Tools;
using CodeSandbox.Contracts.Responses;
using Xunit;

namespace CodeSandbox.Executor.IntegrationTests;

[Trait("Category", "Integration")]
public class ExecutionTests : IClassFixture<ServerFixture>
{
    private readonly HttpClient _client;

    public ExecutionTests(ServerFixture fixture)
    {
        _client = fixture.HttpClient;
    }

    private async Task<ToolResponse> ExecuteCommandAsync(string command, int timeoutSeconds = 30)
    {
        var request = new BashRequest { Command = command, TimeoutSeconds = timeoutSeconds };
        var response = await _client.PostAsJsonAsync("/api/tools/bash", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ToolResponse>())!;
    }

    private async Task<ToolResponse> ResumeAsync(int pid, int timeoutSeconds = 30)
    {
        var request = new ResumeRequest { Pid = pid, TimeoutSeconds = timeoutSeconds };
        var response = await _client.PostAsJsonAsync("/api/tools/resume", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ToolResponse>())!;
    }

    [Fact]
    public async Task Bash_SimpleEchoCommand_ReturnsOutput()
    {
        var result = await ExecuteCommandAsync("echo 'Hello, World!'");

        Assert.False(result.IsError);
        Assert.Contains("Hello, World!", result.Output);
    }

    [Fact]
    public async Task Bash_MultiLineOutput_CapturesAllLines()
    {
        var result = await ExecuteCommandAsync("echo 'Line 1'; echo 'Line 2'; echo 'Line 3'");

        Assert.False(result.IsError);
        Assert.Contains("Line 1", result.Output);
        Assert.Contains("Line 2", result.Output);
        Assert.Contains("Line 3", result.Output);
    }

    [Fact]
    public async Task Bash_StderrOutput_CapturesErrorStream()
    {
        var result = await ExecuteCommandAsync("echo 'This is an error' >&2");

        Assert.Contains("This is an error", result.Output);
    }

    [Fact]
    public async Task Bash_NonZeroExitCode_ReturnsExitCodeInOutput()
    {
        var result = await ExecuteCommandAsync("exit 42");

        Assert.Contains("[Exit code: 42]", result.Output);
    }

    [Fact]
    public async Task Bash_MultipleProcesses_RunConcurrently()
    {
        var task1 = ExecuteCommandAsync("sleep 2; echo 'Process A done'");
        var task2 = ExecuteCommandAsync("sleep 1; echo 'Process B done'");
        var task3 = ExecuteCommandAsync("sleep 3; echo 'Process C done'");

        var results = await Task.WhenAll(task1, task2, task3);

        Assert.All(results, r => Assert.False(r.IsError));
        Assert.Contains("Process A done", results[0].Output);
        Assert.Contains("Process B done", results[1].Output);
        Assert.Contains("Process C done", results[2].Output);
    }

    [Fact]
    public async Task Bash_MixedStdoutStderr_CapturesBothStreams()
    {
        var result = await ExecuteCommandAsync("echo 'stdout message'; echo 'stderr message' >&2");

        Assert.Contains("stdout message", result.Output);
        Assert.Contains("stderr message", result.Output);
    }

    [Fact]
    public async Task Bash_PipedCommands_ExecutesCorrectly()
    {
        var result = await ExecuteCommandAsync("echo hello world | grep world");

        Assert.False(result.IsError);
        Assert.Contains("hello world", result.Output);
    }

    [Fact]
    public async Task Bash_VeryLongOutput_CapturesEverything()
    {
        var result = await ExecuteCommandAsync("for i in $(seq 1 50); do echo \"Line $i\"; done");

        Assert.False(result.IsError);
        Assert.Contains("Line 1", result.Output);
        Assert.Contains("Line 25", result.Output);
        Assert.Contains("Line 50", result.Output);
    }

    [Fact]
    public async Task Bash_CommandWithEnvironmentVariables_UsesVariables()
    {
        var result = await ExecuteCommandAsync("TEST_VAR=hello; echo $TEST_VAR");

        Assert.False(result.IsError);
        Assert.Contains("hello", result.Output);
    }

    [Fact]
    public async Task Bash_Timeout_ReturnsTimedOutWithPid()
    {
        var result = await ExecuteCommandAsync(
            "echo 'before timeout'; sleep 10; echo 'after timeout'",
            timeoutSeconds: 3);

        Assert.Contains("before timeout", result.Output);
        Assert.Contains("Operation timed out", result.Output);
        Assert.Contains("pid=", result.Output);
    }

    [Fact]
    public async Task Bash_Timeout_ResumeGetsRemainingOutput()
    {
        var result = await ExecuteCommandAsync(
            "for i in $(seq 1 8); do echo \"tick $i\"; sleep 1; done",
            timeoutSeconds: 3);

        Assert.Contains("Operation timed out", result.Output);

        var pidMatch = System.Text.RegularExpressions.Regex.Match(result.Output, @"pid=(\d+)");
        Assert.True(pidMatch.Success, "Expected PID in timeout message");
        var pid = int.Parse(pidMatch.Groups[1].Value);

        await Task.Delay(2000);

        var resumed = await ResumeAsync(pid, timeoutSeconds: 15);

        Assert.NotEmpty(resumed.Output);
    }

    [Fact]
    public async Task Resume_UnknownPid_ReturnsError()
    {
        var result = await ResumeAsync(99999);

        Assert.True(result.IsError);
        Assert.Contains("No tracked process found", result.Output);
    }

    [Fact]
    public async Task Bash_EmptyCommand_ReturnsError()
    {
        var result = await ExecuteCommandAsync("");

        Assert.True(result.IsError);
        Assert.Contains("command must not be empty", result.Output);
    }
}

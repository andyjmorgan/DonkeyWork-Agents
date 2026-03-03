using System.Text;
using CodeSandbox.Manager.Contracts.Grpc;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Tools.Sandbox;

/// <summary>
/// gRPC client for communicating with the CodeSandbox Manager's sandbox endpoints.
/// Handles pod lifecycle (find/create) and command execution.
/// </summary>
public sealed class SandboxManagerClient
{
    private readonly SandboxManagerService.SandboxManagerServiceClient _client;
    private readonly ILogger<SandboxManagerClient> _logger;

    public SandboxManagerClient(GrpcChannel channel, ILogger<SandboxManagerClient> logger)
    {
        _client = new SandboxManagerService.SandboxManagerServiceClient(channel);
        _logger = logger;
    }

    /// <summary>
    /// Find an existing sandbox for the given user and conversation.
    /// Returns the pod name if found, null otherwise.
    /// </summary>
    public async Task<string?> FindSandboxAsync(string userId, string conversationId, CancellationToken ct)
    {
        try
        {
            var response = await _client.FindSandboxAsync(
                new FindSandboxRequest { UserId = userId, ConversationId = conversationId },
                cancellationToken: ct);

            return response.Name;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// Create a new sandbox for the given user and conversation.
    /// Streams gRPC creation events and returns the pod name when ready.
    /// </summary>
    public async Task<string> CreateSandboxAsync(
        string userId,
        string conversationId,
        Action<string>? onProgress,
        CancellationToken ct)
    {
        var request = new CreateSandboxRequest
        {
            UserId = userId,
            ConversationId = conversationId,
        };

        using var call = _client.CreateSandbox(request, cancellationToken: ct);

        string? podName = null;
        string? failReason = null;

        await foreach (var evt in call.ResponseStream.ReadAllAsync(ct))
        {
            switch (evt.EventType)
            {
                case "ContainerCreatedEvent":
                    onProgress?.Invoke("Sandbox pod created, waiting for ready...");
                    break;

                case "ContainerWaitingEvent":
                    onProgress?.Invoke(evt.Message is { Length: > 0 } ? evt.Message : "Waiting for sandbox...");
                    break;

                case "ContainerReadyEvent":
                    podName = evt.PodName;
                    _logger.LogInformation("Sandbox ready: {PodName}", podName);
                    break;

                case "ContainerFailedEvent":
                    failReason = evt.Reason is { Length: > 0 } ? evt.Reason : "Unknown failure";
                    _logger.LogWarning("Sandbox creation failed: {Reason}", failReason);
                    break;
            }
        }

        if (podName is not null)
            return podName;

        throw new InvalidOperationException($"Sandbox creation failed: {failReason ?? "unknown reason"}");
    }

    /// <summary>
    /// Execute a command in the given sandbox.
    /// Reads the gRPC stream and returns collected stdout, stderr, exit code, and timeout status.
    /// </summary>
    public async Task<CommandResult> ExecuteCommandAsync(
        string sandboxId,
        string command,
        int timeoutSeconds,
        CancellationToken ct)
    {
        var request = new ExecuteCommandRequest
        {
            SandboxId = sandboxId,
            Command = command,
            TimeoutSeconds = timeoutSeconds,
        };

        using var call = _client.ExecuteCommand(request, cancellationToken: ct);

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var exitCode = -1;
        var timedOut = false;
        var pid = 0;

        await foreach (var evt in call.ResponseStream.ReadAllAsync(ct))
        {
            switch (evt.EventType)
            {
                case "output":
                    if (evt.HasStream && evt.Stream == "stderr")
                        stderr.Append(evt.Data);
                    else
                        stdout.Append(evt.Data);
                    if (evt.Pid > 0)
                        pid = evt.Pid;
                    break;

                case "exit":
                    if (evt.HasExitCode)
                        exitCode = evt.ExitCode;
                    if (evt.Pid > 0)
                        pid = evt.Pid;
                    break;

                case "timeout":
                    timedOut = true;
                    if (evt.HasExitCode)
                        exitCode = evt.ExitCode;
                    if (evt.Pid > 0)
                        pid = evt.Pid;
                    break;
            }
        }

        return new CommandResult(stdout.ToString(), stderr.ToString(), exitCode, timedOut, pid);
    }

    /// <summary>
    /// Delete a sandbox by pod name.
    /// </summary>
    public async Task<bool> DeleteSandboxAsync(string sandboxId, CancellationToken ct)
    {
        try
        {
            var response = await _client.DeleteSandboxAsync(
                new DeleteSandboxRequest { PodName = sandboxId },
                cancellationToken: ct);

            return response.Success;
        }
        catch (RpcException)
        {
            return false;
        }
    }

}

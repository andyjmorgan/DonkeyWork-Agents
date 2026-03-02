using CodeSandbox.Manager.Contracts.Grpc;
using DonkeyWork.Agents.Mcp.Contracts.Models;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Tools.Mcp;

/// <summary>
/// gRPC client for communicating with the CodeSandbox Manager's MCP server endpoints.
/// Handles pod lifecycle (find/create/start) and JSON-RPC proxying.
/// </summary>
public sealed class McpSandboxManagerClient
{
    private readonly McpManagerService.McpManagerServiceClient _client;
    private readonly ILogger<McpSandboxManagerClient> _logger;

    public McpSandboxManagerClient(GrpcChannel channel, ILogger<McpSandboxManagerClient> logger)
    {
        _client = new McpManagerService.McpManagerServiceClient(channel);
        _logger = logger;
    }

    /// <summary>
    /// Finds an existing MCP server pod or creates one, ensuring the MCP process is started.
    /// Returns the pod name when the server is ready.
    /// </summary>
    public async Task<string> EnsureMcpServerAsync(
        string userId,
        Guid mcpServerConfigId,
        McpStdioConnectionConfigV1 config,
        Action<string>? onProgress,
        CancellationToken ct)
    {
        // 1. Try to find an existing pod
        var existingPodName = await FindMcpServerAsync(userId, mcpServerConfigId.ToString(), ct);

        if (existingPodName is not null)
        {
            _logger.LogInformation("Found existing MCP server pod {PodName} for config {ConfigId}", existingPodName, mcpServerConfigId);

            // Check if the MCP process is running
            var status = await GetMcpStatusAsync(existingPodName, ct);
            if (status is "Ready" or "Initializing")
                return existingPodName;

            // Process not running — start it
            onProgress?.Invoke($"Starting MCP process in existing pod {existingPodName}...");
            await StartMcpProcessAsync(existingPodName, config, onProgress, ct);
            return existingPodName;
        }

        // 2. No existing pod — create one
        onProgress?.Invoke($"Creating MCP server pod for '{config.Name}'...");
        var podName = await CreateMcpServerAsync(userId, mcpServerConfigId, config, onProgress, ct);
        return podName;
    }

    /// <summary>
    /// Proxies a JSON-RPC request to the MCP server via the Manager.
    /// </summary>
    public async Task<string?> ProxyMcpRequestAsync(
        string podName,
        string jsonRpcBody,
        int timeoutSeconds,
        CancellationToken ct)
    {
        var request = new ProxyMcpRequestMessage
        {
            PodName = podName,
            Body = jsonRpcBody,
            TimeoutSeconds = timeoutSeconds,
        };

        var response = await _client.ProxyMcpRequestAsync(request, cancellationToken: ct);
        return response.Body is { Length: > 0 } ? response.Body : null;
    }

    /// <summary>
    /// Gets the MCP process status string from the Manager.
    /// </summary>
    public async Task<string> GetMcpStatusAsync(string podName, CancellationToken ct)
    {
        var response = await _client.GetMcpStatusAsync(
            new GetMcpStatusRequest { PodName = podName },
            cancellationToken: ct);

        return response.State is { Length: > 0 } ? response.State : "Unknown";
    }

    private async Task<string?> FindMcpServerAsync(string userId, string mcpServerConfigId, CancellationToken ct)
    {
        try
        {
            var response = await _client.FindMcpServerAsync(
                new FindMcpServerRequest { UserId = userId, McpServerConfigId = mcpServerConfigId },
                cancellationToken: ct);

            return response.Name;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task<string> CreateMcpServerAsync(
        string userId,
        Guid mcpServerConfigId,
        McpStdioConnectionConfigV1 config,
        Action<string>? onProgress,
        CancellationToken ct)
    {
        var request = new CreateMcpServerRequest
        {
            UserId = userId,
            McpServerConfigId = mcpServerConfigId.ToString(),
            Command = config.Command,
            TimeoutSeconds = 120,
        };

        if (config.Arguments is { Count: > 0 })
            request.Arguments.AddRange(config.Arguments);

        if (config.PreExecScripts is { Count: > 0 })
            request.PreExecScripts.AddRange(config.PreExecScripts);

        if (config.EnvironmentVariables is { Count: > 0 })
            request.EnvironmentVariables.Add(config.EnvironmentVariables);

        if (config.WorkingDirectory is not null)
            request.WorkingDirectory = config.WorkingDirectory;

        using var call = _client.CreateMcpServer(request, cancellationToken: ct);

        string? podName = null;
        string? failReason = null;

        await foreach (var evt in call.ResponseStream.ReadAllAsync(ct))
        {
            switch (evt.EventType)
            {
                case "ContainerCreatedEvent":
                    onProgress?.Invoke("MCP server pod created, waiting for ready...");
                    break;

                case "ContainerWaitingEvent":
                    onProgress?.Invoke(evt.Message is { Length: > 0 } ? evt.Message : "Waiting for MCP server...");
                    break;

                case "ContainerReadyEvent":
                    podName = evt.PodName;
                    _logger.LogInformation("MCP server pod ready: {PodName}", podName);
                    break;

                case "McpServerStartedEvent":
                    podName ??= evt.PodName;
                    _logger.LogInformation("MCP server started in pod: {PodName}", podName);
                    break;

                case "ContainerFailedEvent":
                case "McpServerStartFailedEvent":
                    failReason = evt.Reason is { Length: > 0 } ? evt.Reason : "Unknown failure";
                    _logger.LogWarning("MCP server creation failed: {Reason}", failReason);
                    break;
            }
        }

        if (podName is not null)
            return podName;

        throw new InvalidOperationException($"MCP server creation failed: {failReason ?? "unknown reason"}");
    }

    private async Task StartMcpProcessAsync(
        string podName,
        McpStdioConnectionConfigV1 config,
        Action<string>? onProgress,
        CancellationToken ct)
    {
        var request = new StartMcpProcessRequest
        {
            PodName = podName,
            Command = config.Command,
            TimeoutSeconds = 60,
        };

        if (config.Arguments is { Count: > 0 })
            request.Arguments.AddRange(config.Arguments);

        if (config.PreExecScripts is { Count: > 0 })
            request.PreExecScripts.AddRange(config.PreExecScripts);

        if (config.WorkingDirectory is not null)
            request.WorkingDirectory = config.WorkingDirectory;

        using var call = _client.StartMcpProcess(request, cancellationToken: ct);

        string? failReason = null;

        await foreach (var evt in call.ResponseStream.ReadAllAsync(ct))
        {
            switch (evt.EventType)
            {
                case "ready":
                    _logger.LogInformation("MCP process started in pod {PodName}", podName);
                    return;

                case "error":
                    failReason = evt.Error is { Length: > 0 } ? evt.Error : evt.Message;
                    break;

                default:
                    if (evt.Message is { Length: > 0 })
                        onProgress?.Invoke(evt.Message);
                    break;
            }
        }

        if (failReason is not null)
            throw new InvalidOperationException($"MCP process start failed: {failReason}");
    }

}

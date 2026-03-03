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
        _logger.LogInformation("Ensuring MCP server for UserId={UserId}, ConfigId={ConfigId}, Command={Command}",
            userId, mcpServerConfigId, config.Command);

        // 1. Try to find an existing pod
        var existingPodName = await FindMcpServerAsync(userId, mcpServerConfigId.ToString(), ct);

        if (existingPodName is not null)
        {
            _logger.LogInformation("Found existing MCP server pod {PodName} for ConfigId={ConfigId}", existingPodName, mcpServerConfigId);

            // Check if the MCP process is running
            var status = await GetMcpStatusAsync(existingPodName, ct);
            _logger.LogInformation("MCP server {PodName} process status: {Status}", existingPodName, status);

            if (status is "Ready" or "Initializing")
                return existingPodName;

            // Process not running — start it
            _logger.LogInformation("MCP process not running in {PodName}, starting...", existingPodName);
            onProgress?.Invoke($"Starting MCP process in existing pod {existingPodName}...");
            await StartMcpProcessAsync(existingPodName, config, onProgress, ct);
            return existingPodName;
        }

        // 2. No existing pod — create one
        _logger.LogInformation("No existing MCP server pod found, creating new for ConfigId={ConfigId}", mcpServerConfigId);
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
        _logger.LogDebug("Proxying MCP request to {PodName} (timeout={Timeout}s, bodyLen={BodyLen})",
            podName, timeoutSeconds, jsonRpcBody.Length);

        var request = new ProxyMcpRequestMessage
        {
            PodName = podName,
            Body = jsonRpcBody,
            TimeoutSeconds = timeoutSeconds,
        };

        var response = await _client.ProxyMcpRequestAsync(request, cancellationToken: ct);
        var hasBody = response.Body is { Length: > 0 };

        _logger.LogDebug("MCP proxy response from {PodName}: hasBody={HasBody}, bodyLen={BodyLen}",
            podName, hasBody, response.Body?.Length ?? 0);

        return hasBody ? response.Body : null;
    }

    /// <summary>
    /// Gets the MCP process status string from the Manager.
    /// </summary>
    public async Task<string> GetMcpStatusAsync(string podName, CancellationToken ct)
    {
        _logger.LogDebug("Getting MCP status for {PodName}", podName);

        var response = await _client.GetMcpStatusAsync(
            new GetMcpStatusRequest { PodName = podName },
            cancellationToken: ct);

        var state = response.State is { Length: > 0 } ? response.State : "Unknown";
        _logger.LogDebug("MCP status for {PodName}: {State}", podName, state);
        return state;
    }

    private async Task<string?> FindMcpServerAsync(string userId, string mcpServerConfigId, CancellationToken ct)
    {
        _logger.LogDebug("Finding MCP server for UserId={UserId}, ConfigId={ConfigId}", userId, mcpServerConfigId);

        try
        {
            var response = await _client.FindMcpServerAsync(
                new FindMcpServerRequest { UserId = userId, McpServerConfigId = mcpServerConfigId },
                cancellationToken: ct);

            _logger.LogDebug("Found MCP server {PodName} for ConfigId={ConfigId}", response.Name, mcpServerConfigId);
            return response.Name;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            _logger.LogDebug("No existing MCP server found for ConfigId={ConfigId}", mcpServerConfigId);
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

        _logger.LogInformation("Creating MCP server pod: Command={Command}, ConfigId={ConfigId}",
            config.Command, mcpServerConfigId);

        using var call = _client.CreateMcpServer(request, cancellationToken: ct);

        string? podName = null;
        string? failReason = null;

        await foreach (var evt in call.ResponseStream.ReadAllAsync(ct))
        {
            _logger.LogDebug("MCP server creation event: Type={EventType}, PodName={PodName}, Message={Message}",
                evt.EventType, evt.PodName, evt.Message);

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
                    _logger.LogInformation("MCP server pod ready: {PodName} (elapsed: {Elapsed}s)", podName, evt.ElapsedSeconds);
                    break;

                case "McpServerStartedEvent":
                    podName ??= evt.PodName;
                    _logger.LogInformation("MCP server process started in pod: {PodName} (elapsed: {Elapsed}s)", podName, evt.ElapsedSeconds);
                    break;

                case "ContainerFailedEvent":
                case "McpServerStartFailedEvent":
                    failReason = evt.Reason is { Length: > 0 } ? evt.Reason : "Unknown failure";
                    _logger.LogWarning("MCP server creation failed for ConfigId={ConfigId}: {Reason}", mcpServerConfigId, failReason);
                    break;
            }
        }

        if (podName is not null)
        {
            _logger.LogInformation("MCP server ready: {PodName} for ConfigId={ConfigId}", podName, mcpServerConfigId);
            return podName;
        }

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

        _logger.LogInformation("Starting MCP process in pod {PodName}: Command={Command}", podName, config.Command);

        using var call = _client.StartMcpProcess(request, cancellationToken: ct);

        string? failReason = null;

        await foreach (var evt in call.ResponseStream.ReadAllAsync(ct))
        {
            _logger.LogDebug("MCP process start event in {PodName}: Type={EventType}, Message={Message}",
                podName, evt.EventType, evt.Message);

            switch (evt.EventType)
            {
                case "ready":
                    _logger.LogInformation("MCP process started successfully in pod {PodName}", podName);
                    return;

                case "error":
                    failReason = evt.Error is { Length: > 0 } ? evt.Error : evt.Message;
                    _logger.LogWarning("MCP process start error in pod {PodName}: {Error}", podName, failReason);
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

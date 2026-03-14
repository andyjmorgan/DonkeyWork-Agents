using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using DonkeyWork.Agents.Actors.Core.Providers;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Mcp.Contracts.Models;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace DonkeyWork.Agents.Actors.Core.Tools.Mcp;

/// <summary>
/// Per-grain MCP tool manager that connects to external MCP servers,
/// discovers their tools, and provides execution capabilities.
/// Supports both HTTP MCP servers (via MCP SDK) and stdio MCP servers (via sandbox manager proxy).
/// </summary>
internal sealed class McpToolProvider : IAsyncDisposable
{
    private static readonly TimeSpan PerServerTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StdioServerTimeout = TimeSpan.FromSeconds(120);
    private static int _jsonRpcIdCounter;

    private readonly List<McpClient> _clients = [];
    private readonly Dictionary<string, McpClientTool> _toolsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, StdioToolInfo> _stdioTools = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _toolServerNames = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<InternalToolDefinition>? _cachedDefinitions;

    /// <summary>
    /// Connects to each MCP server in parallel, discovers tools, and caches them.
    /// </summary>
    public async Task InitializeAsync(
        IReadOnlyList<McpConnectionConfigV1> httpConfigs,
        IReadOnlyList<McpStdioConnectionConfigV1> stdioConfigs,
        McpSandboxManagerClient? sandboxManagerClient,
        string userId,
        ILogger logger,
        Action<string, bool, long, int, string?>? onServerStatus,
        CancellationToken ct)
    {
        // Initialize HTTP MCP servers in parallel
        if (httpConfigs.Count > 0)
        {
            var results = new ConcurrentBag<(McpClient Client, string ServerName, IList<McpClientTool> Tools)>();
            var tasks = httpConfigs.Select(config => ConnectHttpServerAsync(config, logger, onServerStatus, results, ct));
            await Task.WhenAll(tasks);

            foreach (var (client, serverName, tools) in results)
            {
                _clients.Add(client);
                foreach (var tool in tools)
                {
                    if (_toolsByName.ContainsKey(tool.Name) || _stdioTools.ContainsKey(tool.Name))
                    {
                        logger.LogWarning(
                            "Duplicate MCP tool name '{ToolName}' from server '{ServerName}', " +
                            "already registered from '{ExistingServer}'. Skipping.",
                            tool.Name, serverName, _toolServerNames[tool.Name]);
                        continue;
                    }

                    _toolsByName[tool.Name] = tool;
                    _toolServerNames[tool.Name] = serverName;
                }
            }
        }

        // Initialize stdio MCP servers sequentially (each may create a pod)
        if (stdioConfigs.Count > 0 && sandboxManagerClient is not null)
        {
            foreach (var config in stdioConfigs)
            {
                var sw = Stopwatch.StartNew();
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(StdioServerTimeout);

                try
                {
                    var podName = await sandboxManagerClient.EnsureMcpServerAsync(
                        userId, config.Id, config, null, timeoutCts.Token);

                    // Discover tools via JSON-RPC tools/list
                    var toolsListRequest = BuildToolsListJsonRpc();
                    var response = await sandboxManagerClient.ProxyMcpRequestAsync(
                        podName, toolsListRequest, 30, timeoutCts.Token);

                    var tools = ParseToolsFromResponse(response);
                    sw.Stop();

                    foreach (var tool in tools)
                    {
                        if (_toolsByName.ContainsKey(tool.Name) || _stdioTools.ContainsKey(tool.Name))
                        {
                            logger.LogWarning(
                                "Duplicate MCP tool name '{ToolName}' from stdio server '{ServerName}', " +
                                "already registered from '{ExistingServer}'. Skipping.",
                                tool.Name, config.Name, _toolServerNames[tool.Name]);
                            continue;
                        }

                        _stdioTools[tool.Name] = new StdioToolInfo(tool, podName, sandboxManagerClient);
                        _toolServerNames[tool.Name] = config.Name;
                    }

                    logger.LogInformation(
                        "Connected to stdio MCP server '{ServerName}' via pod {PodName}, discovered {ToolCount} tools in {ElapsedMs}ms",
                        config.Name, podName, tools.Count, sw.ElapsedMilliseconds);

                    onServerStatus?.Invoke(config.Name, true, sw.ElapsedMilliseconds, tools.Count, null);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    sw.Stop();
                    logger.LogError(
                        "Timed out connecting to stdio MCP server '{ServerName}' after {ElapsedMs}ms",
                        config.Name, sw.ElapsedMilliseconds);

                    onServerStatus?.Invoke(config.Name, false, sw.ElapsedMilliseconds, 0, "Connection timed out");
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    logger.LogError(ex,
                        "Failed to connect to stdio MCP server '{ServerName}' after {ElapsedMs}ms, skipping",
                        config.Name, sw.ElapsedMilliseconds);

                    onServerStatus?.Invoke(config.Name, false, sw.ElapsedMilliseconds, 0, ex.Message);
                }
            }
        }
    }

    /// <summary>
    /// Returns tool definitions for all discovered MCP tools (both HTTP and stdio).
    /// Uses cached definitions with all tools deferred by default.
    /// </summary>
    public IReadOnlyList<InternalToolDefinition> GetToolDefinitions()
    {
        if (_cachedDefinitions is not null)
            return _cachedDefinitions;

        _cachedDefinitions = GetToolDefinitions(defaultDefer: true, perToolDefer: null, excludedTools: null);
        return _cachedDefinitions;
    }

    /// <summary>
    /// Returns tool definitions with configurable deferred loading per tool.
    /// </summary>
    public IReadOnlyList<InternalToolDefinition> GetToolDefinitions(
        bool defaultDefer,
        Dictionary<string, bool>? perToolDefer,
        HashSet<string>? excludedTools)
    {
        var definitions = new List<InternalToolDefinition>(_toolsByName.Count + _stdioTools.Count);

        foreach (var (name, tool) in _toolsByName)
        {
            if (excludedTools?.Contains(name) == true)
                continue;

            var defer = perToolDefer?.TryGetValue(name, out var d) == true ? d : defaultDefer;
            definitions.Add(new InternalToolDefinition
            {
                Name = tool.Name,
                DisplayName = tool.ProtocolTool.Title ?? tool.Name,
                Description = tool.ProtocolTool.Description,
                InputSchema = tool.ProtocolTool.InputSchema,
                DeferLoading = defer,
            });
        }

        foreach (var (name, stdioTool) in _stdioTools)
        {
            if (excludedTools?.Contains(name) == true)
                continue;

            var def = new InternalToolDefinition
            {
                Name = stdioTool.Definition.Name,
                DisplayName = stdioTool.Definition.DisplayName,
                Description = stdioTool.Definition.Description,
                InputSchema = stdioTool.Definition.InputSchema,
                DeferLoading = perToolDefer?.TryGetValue(name, out var d) == true ? d : defaultDefer,
            };
            definitions.Add(def);
        }

        return definitions;
    }

    /// <summary>
    /// Returns a summary of connected servers (name → tool count) for re-emitting status on observer reconnect.
    /// </summary>
    public IReadOnlyList<(string ServerName, int ToolCount)> GetConnectedServerSummaries()
    {
        var servers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var (_, serverName) in _toolServerNames)
        {
            servers.TryGetValue(serverName, out var count);
            servers[serverName] = count + 1;
        }

        return servers.Select(kv => (kv.Key, kv.Value)).ToList();
    }

    /// <summary>
    /// Checks whether a tool with the given name exists in this provider.
    /// </summary>
    public bool HasTool(string toolName) =>
        _toolsByName.ContainsKey(toolName) || _stdioTools.ContainsKey(toolName);

    /// <summary>
    /// Gets the display name for a tool, or null if not found.
    /// </summary>
    public string? GetDisplayName(string toolName)
    {
        if (_toolsByName.TryGetValue(toolName, out var tool))
            return tool.ProtocolTool.Title ?? tool.Name;

        if (_stdioTools.TryGetValue(toolName, out var stdioTool))
            return stdioTool.Definition.DisplayName ?? stdioTool.Definition.Name;

        return null;
    }

    /// <summary>
    /// Executes an MCP tool by name. Routes to HTTP or stdio path as appropriate.
    /// </summary>
    public async Task<ToolResult> ExecuteAsync(string toolName, JsonElement arguments, CancellationToken ct)
    {
        // HTTP tool path
        if (_toolsByName.TryGetValue(toolName, out var tool))
        {
            return await ExecuteHttpToolAsync(tool, arguments, ct);
        }

        // Stdio tool path
        if (_stdioTools.TryGetValue(toolName, out var stdioTool))
        {
            return await ExecuteStdioToolAsync(stdioTool, toolName, arguments, ct);
        }

        return ToolResult.Error($"MCP tool '{toolName}' not found.");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients)
        {
            try
            {
                await client.DisposeAsync();
            }
            catch
            {
                // Best-effort cleanup
            }
        }

        _clients.Clear();
        _toolsByName.Clear();
        _stdioTools.Clear();
        _toolServerNames.Clear();
        _cachedDefinitions = null;
    }

    #region HTTP Tool Execution

    private static async Task<ToolResult> ExecuteHttpToolAsync(McpClientTool tool, JsonElement arguments, CancellationToken ct)
    {
        var args = new Dictionary<string, object?>();
        if (arguments.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in arguments.EnumerateObject())
            {
                args[property.Name] = property.Value;
            }
        }

        var result = await tool.CallAsync(args, cancellationToken: ct);

        var textParts = new List<string>();
        foreach (var content in result.Content)
        {
            if (content is TextContentBlock textContent)
            {
                textParts.Add(textContent.Text);
            }
        }

        var responseText = textParts.Count > 0
            ? string.Join("\n", textParts)
            : "Tool returned no text content.";

        return result.IsError == true
            ? ToolResult.Error(responseText)
            : ToolResult.Success(responseText);
    }

    #endregion

    #region Stdio Tool Execution

    private static async Task<ToolResult> ExecuteStdioToolAsync(
        StdioToolInfo stdioTool,
        string toolName,
        JsonElement arguments,
        CancellationToken ct)
    {
        var jsonRpcRequest = BuildToolCallJsonRpc(toolName, arguments);
        var responseBody = await stdioTool.Client.ProxyMcpRequestAsync(
            stdioTool.PodName, jsonRpcRequest, 60, ct);

        if (responseBody is null)
            return ToolResult.Error("MCP server returned no response.");

        return ParseToolCallResponse(responseBody);
    }

    private static string BuildToolsListJsonRpc()
    {
        var id = Interlocked.Increment(ref _jsonRpcIdCounter);
        var request = new
        {
            jsonrpc = "2.0",
            id,
            method = "tools/list",
            @params = new { },
        };
        return JsonSerializer.Serialize(request);
    }

    private static string BuildToolCallJsonRpc(string toolName, JsonElement arguments)
    {
        var id = Interlocked.Increment(ref _jsonRpcIdCounter);
        var request = new
        {
            jsonrpc = "2.0",
            id,
            method = "tools/call",
            @params = new
            {
                name = toolName,
                arguments,
            },
        };
        return JsonSerializer.Serialize(request);
    }

    private static List<InternalToolDefinition> ParseToolsFromResponse(string? responseBody)
    {
        var tools = new List<InternalToolDefinition>();

        if (string.IsNullOrEmpty(responseBody))
            return tools;

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (!root.TryGetProperty("result", out var result))
            return tools;

        if (!result.TryGetProperty("tools", out var toolsArray) || toolsArray.ValueKind != JsonValueKind.Array)
            return tools;

        foreach (var toolElement in toolsArray.EnumerateArray())
        {
            var name = toolElement.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
            if (name is null)
                continue;

            var description = toolElement.TryGetProperty("description", out var descProp) ? descProp.GetString() : null;
            var title = toolElement.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : null;

            object? inputSchema = null;
            if (toolElement.TryGetProperty("inputSchema", out var schemaProp))
            {
                inputSchema = JsonSerializer.Deserialize<JsonElement>(schemaProp.GetRawText());
            }

            tools.Add(new InternalToolDefinition
            {
                Name = name,
                DisplayName = title ?? name,
                Description = description,
                InputSchema = inputSchema,
                DeferLoading = true,
            });
        }

        return tools;
    }

    private static ToolResult ParseToolCallResponse(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        // Check for JSON-RPC error
        if (root.TryGetProperty("error", out var errorProp))
        {
            var errorMessage = errorProp.TryGetProperty("message", out var msgProp)
                ? msgProp.GetString() ?? "Unknown error"
                : "Unknown JSON-RPC error";
            return ToolResult.Error(errorMessage);
        }

        if (!root.TryGetProperty("result", out var result))
            return ToolResult.Error("MCP server returned no result.");

        var isError = result.TryGetProperty("isError", out var isErrorProp) && isErrorProp.GetBoolean();

        var textParts = new List<string>();
        if (result.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var content in contentArray.EnumerateArray())
            {
                var type = content.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
                if (type == "text" && content.TryGetProperty("text", out var textProp))
                {
                    textParts.Add(textProp.GetString() ?? "");
                }
            }
        }

        var responseText = textParts.Count > 0
            ? string.Join("\n", textParts)
            : "Tool returned no text content.";

        return isError ? ToolResult.Error(responseText) : ToolResult.Success(responseText);
    }

    #endregion

    #region HTTP Server Connection

    private static async Task ConnectHttpServerAsync(
        McpConnectionConfigV1 config,
        ILogger logger,
        Action<string, bool, long, int, string?>? onServerStatus,
        ConcurrentBag<(McpClient, string, IList<McpClientTool>)> results,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(PerServerTimeout);

        try
        {
            var client = await ConnectAsync(config, logger, timeoutCts.Token);
            var tools = await client.ListToolsAsync(cancellationToken: timeoutCts.Token);
            sw.Stop();

            results.Add((client, config.Name, tools));

            logger.LogInformation(
                "Connected to MCP server '{ServerName}' at {Endpoint}, discovered {ToolCount} tools in {ElapsedMs}ms",
                config.Name, config.Endpoint, tools.Count, sw.ElapsedMilliseconds);

            onServerStatus?.Invoke(config.Name, true, sw.ElapsedMilliseconds, tools.Count, null);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            sw.Stop();
            logger.LogError(
                "Timed out connecting to MCP server '{ServerName}' at {Endpoint} after {ElapsedMs}ms",
                config.Name, config.Endpoint, sw.ElapsedMilliseconds);

            onServerStatus?.Invoke(config.Name, false, sw.ElapsedMilliseconds, 0, "Connection timed out");
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex,
                "Failed to connect to MCP server '{ServerName}' at {Endpoint} after {ElapsedMs}ms, skipping",
                config.Name, config.Endpoint, sw.ElapsedMilliseconds);

            onServerStatus?.Invoke(config.Name, false, sw.ElapsedMilliseconds, 0, ex.Message);
        }
    }

    private static async Task<McpClient> ConnectAsync(
        McpConnectionConfigV1 config,
        ILogger logger,
        CancellationToken ct)
    {
        var transportMode = config.TransportMode switch
        {
            McpHttpTransportMode.Sse => HttpTransportMode.Sse,
            McpHttpTransportMode.StreamableHttp => HttpTransportMode.StreamableHttp,
            _ => HttpTransportMode.AutoDetect,
        };

        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = new Uri(config.Endpoint),
            TransportMode = transportMode,
            Name = config.Name,
        };

        if (config.Headers.Count > 0)
        {
            transportOptions.AdditionalHeaders = new Dictionary<string, string>(config.Headers);
        }

        var transport = new HttpClientTransport(transportOptions);

        var clientOptions = new McpClientOptions
        {
            ClientInfo = new Implementation
            {
                Name = "DonkeyWork-Agents",
                Version = "1.0.0",
            },
        };

        return await McpClient.CreateAsync(transport, clientOptions, cancellationToken: ct);
    }

    #endregion

    /// <summary>
    /// Holds the tool definition and routing info for a stdio MCP tool.
    /// </summary>
    private sealed record StdioToolInfo(
        InternalToolDefinition Definition,
        string PodName,
        McpSandboxManagerClient Client);
}

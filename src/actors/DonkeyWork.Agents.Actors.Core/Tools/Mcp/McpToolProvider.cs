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
/// </summary>
internal sealed class McpToolProvider : IAsyncDisposable
{
    private static readonly TimeSpan PerServerTimeout = TimeSpan.FromSeconds(30);

    private readonly List<McpClient> _clients = [];
    private readonly Dictionary<string, McpClientTool> _toolsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _toolServerNames = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<InternalToolDefinition>? _cachedDefinitions;

    /// <summary>
    /// Connects to each MCP server in parallel, discovers tools, and caches them.
    /// </summary>
    public async Task InitializeAsync(
        IReadOnlyList<McpConnectionConfigV1> configs,
        ILogger logger,
        Action<string, bool, long, int, string?>? onServerStatus,
        CancellationToken ct)
    {
        var results = new ConcurrentBag<(McpClient Client, string ServerName, IList<McpClientTool> Tools)>();

        var tasks = configs.Select(config => ConnectServerAsync(config, logger, onServerStatus, results, ct));
        await Task.WhenAll(tasks);

        // Merge results into shared dictionaries (single-threaded after WhenAll)
        foreach (var (client, serverName, tools) in results)
        {
            _clients.Add(client);
            foreach (var tool in tools)
            {
                if (_toolsByName.ContainsKey(tool.Name))
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

    /// <summary>
    /// Returns tool definitions for all discovered MCP tools.
    /// </summary>
    public IReadOnlyList<InternalToolDefinition> GetToolDefinitions()
    {
        if (_cachedDefinitions is not null)
            return _cachedDefinitions;

        var definitions = new List<InternalToolDefinition>(_toolsByName.Count);
        foreach (var (name, tool) in _toolsByName)
        {
            definitions.Add(new InternalToolDefinition
            {
                Name = tool.Name,
                DisplayName = tool.ProtocolTool.Title ?? tool.Name,
                Description = tool.ProtocolTool.Description,
                InputSchema = tool.ProtocolTool.InputSchema,
            });
        }

        _cachedDefinitions = definitions;
        return _cachedDefinitions;
    }

    /// <summary>
    /// Checks whether a tool with the given name exists in this provider.
    /// </summary>
    public bool HasTool(string toolName) => _toolsByName.ContainsKey(toolName);

    /// <summary>
    /// Gets the display name for a tool, or null if not found.
    /// </summary>
    public string? GetDisplayName(string toolName) =>
        _toolsByName.TryGetValue(toolName, out var tool)
            ? tool.ProtocolTool.Title ?? tool.Name
            : null;

    /// <summary>
    /// Executes an MCP tool by name.
    /// </summary>
    public async Task<ToolResult> ExecuteAsync(string toolName, JsonElement arguments, CancellationToken ct)
    {
        if (!_toolsByName.TryGetValue(toolName, out var tool))
        {
            return ToolResult.Error($"MCP tool '{toolName}' not found.");
        }

        // Convert JsonElement arguments to dictionary
        var args = new Dictionary<string, object?>();
        if (arguments.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in arguments.EnumerateObject())
            {
                args[property.Name] = property.Value;
            }
        }

        var result = await tool.CallAsync(args, cancellationToken: ct);

        // Concatenate all text content blocks
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
        _toolServerNames.Clear();
        _cachedDefinitions = null;
    }

    private static async Task ConnectServerAsync(
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
}

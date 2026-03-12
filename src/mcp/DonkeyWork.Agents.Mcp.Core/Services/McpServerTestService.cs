using System.Diagnostics;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Mcp.Contracts.Models;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace DonkeyWork.Agents.Mcp.Core.Services;

/// <summary>
/// Tests MCP server connections by connecting and discovering tools.
/// </summary>
public class McpServerTestService : IMcpServerTestService
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(30);

    private readonly IMcpServerConfigurationService _configService;
    private readonly ILogger<McpServerTestService> _logger;

    public McpServerTestService(
        IMcpServerConfigurationService configService,
        ILogger<McpServerTestService> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public async Task<TestMcpServerResponseV1> TestConnectionAsync(Guid serverId, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        var config = await _configService.GetConnectionConfigByIdAsync(serverId, cancellationToken);
        if (config is null)
        {
            sw.Stop();
            return new TestMcpServerResponseV1
            {
                Success = false,
                ElapsedMs = sw.ElapsedMilliseconds,
                Error = "Server not found, not an HTTP server, or has no HTTP configuration.",
            };
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TestTimeout);

        try
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

            await using var client = await McpClient.CreateAsync(transport, clientOptions, cancellationToken: timeoutCts.Token);
            var tools = await client.ListToolsAsync(cancellationToken: timeoutCts.Token);
            sw.Stop();

            _logger.LogInformation(
                "Test connection to MCP server '{ServerName}' at {Endpoint} succeeded, discovered {ToolCount} tools in {ElapsedMs}ms",
                config.Name, config.Endpoint, tools.Count, sw.ElapsedMilliseconds);

            return new TestMcpServerResponseV1
            {
                Success = true,
                ServerName = client.ServerInfo?.Name,
                ServerVersion = client.ServerInfo?.Version,
                ElapsedMs = sw.ElapsedMilliseconds,
                Tools = tools.Select(t => new McpToolInfoV1
                {
                    Name = t.Name,
                    Description = t.Description,
                    InputSchema = t.JsonSchema,
                }).ToList(),
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            _logger.LogError(
                "Test connection to MCP server '{ServerName}' at {Endpoint} timed out after {ElapsedMs}ms",
                config.Name, config.Endpoint, sw.ElapsedMilliseconds);

            return new TestMcpServerResponseV1
            {
                Success = false,
                ElapsedMs = sw.ElapsedMilliseconds,
                Error = "Connection timed out after 30 seconds.",
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "Test connection to MCP server '{ServerName}' at {Endpoint} failed after {ElapsedMs}ms",
                config.Name, config.Endpoint, sw.ElapsedMilliseconds);

            return new TestMcpServerResponseV1
            {
                Success = false,
                ElapsedMs = sw.ElapsedMilliseconds,
                Error = ex.Message,
            };
        }
    }
}

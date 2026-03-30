using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DonkeyWork.Agents.A2a.Contracts.Helpers;
using DonkeyWork.Agents.A2a.Contracts.Models;
using DonkeyWork.Agents.A2a.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

namespace DonkeyWork.Agents.Mcp.Core.Services;

public class A2aMcpToolService : IA2aMcpToolService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(30);

    private readonly IA2aServerConfigurationService _configService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IIdentityContext _identityContext;
    private readonly ILogger<A2aMcpToolService> _logger;

    public A2aMcpToolService(
        IA2aServerConfigurationService configService,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IIdentityContext identityContext,
        ILogger<A2aMcpToolService> logger)
    {
        _configService = configService;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _identityContext = identityContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Tool>> DiscoverToolsAsync(CancellationToken cancellationToken = default)
    {
        var cacheEntry = await GetOrCreateCacheAsync(cancellationToken);
        return cacheEntry.Tools;
    }

    public bool CanHandle(string toolName)
    {
        var cacheKey = GetConfigMapCacheKey();
        return _cache.TryGetValue<Dictionary<string, A2aConnectionConfigV1>>(cacheKey, out var configMap)
               && configMap!.ContainsKey(toolName);
    }

    public async Task<CallToolResult> ExecuteAsync(
        string toolName,
        JsonElement? arguments,
        CancellationToken cancellationToken = default)
    {
        var cacheEntry = await GetOrCreateCacheAsync(cancellationToken);

        if (!cacheEntry.ConfigMap.TryGetValue(toolName, out var config))
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = $"A2A tool '{toolName}' not found." }],
            };
        }

        var message = ExtractStringProperty(arguments, "message");
        if (string.IsNullOrEmpty(message))
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "The 'message' argument is required." }],
            };
        }

        var contextId = ExtractStringProperty(arguments, "contextId");
        var requestBody = A2aProtocolHelper.BuildMessageSendRequest(message, contextId);

        var address = config.Address.TrimEnd('/');
        if (!address.EndsWith("/a2a", StringComparison.OrdinalIgnoreCase))
            address += "/a2a";

        using var client = _httpClientFactory.CreateClient();
        foreach (var header in config.Headers)
            client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);

        using var content = new StringContent(requestBody, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
        using var response = await client.PostAsync(address, content, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "A2A agent {ToolName} returned HTTP {StatusCode}",
                toolName,
                (int)response.StatusCode);

            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = $"A2A agent returned HTTP {(int)response.StatusCode}: {responseBody}" }],
            };
        }

        var (isError, parsedContent) = A2aProtocolHelper.ParseMessageResponse(responseBody);

        return new CallToolResult
        {
            IsError = isError,
            Content = [new TextContentBlock { Text = parsedContent }],
        };
    }

    private async Task<A2aMcpToolCache> GetOrCreateCacheAsync(CancellationToken cancellationToken)
    {
        var toolsCacheKey = GetToolsCacheKey();
        var configMapCacheKey = GetConfigMapCacheKey();

        if (_cache.TryGetValue<IReadOnlyList<Tool>>(toolsCacheKey, out var cachedTools)
            && _cache.TryGetValue<Dictionary<string, A2aConnectionConfigV1>>(configMapCacheKey, out var cachedMap))
        {
            return new A2aMcpToolCache(cachedTools!, cachedMap!);
        }

        var configs = await _configService.GetMcpPublishedConnectionConfigsAsync(cancellationToken);

        var fetchTasks = configs.Select(c => FetchAgentCardAsync(c, cancellationToken)).ToList();
        var results = await Task.WhenAll(fetchTasks);

        var tools = new List<Tool>();
        var configMap = new Dictionary<string, A2aConnectionConfigV1>(StringComparer.Ordinal);

        foreach (var (config, card) in results)
        {
            if (config is null)
                continue;

            var toolName = A2aProtocolHelper.SanitizeToolName(card?.Name ?? config.Name);
            var description = A2aProtocolHelper.BuildToolDescription(card, config.Description);

            var inputSchema = BuildInputSchema();

            var tool = new Tool
            {
                Name = toolName,
                Description = description,
                InputSchema = inputSchema,
            };

            tools.Add(tool);
            configMap.TryAdd(toolName, config);
        }

        _cache.Set(toolsCacheKey, (IReadOnlyList<Tool>)tools, CacheDuration);
        _cache.Set(configMapCacheKey, configMap, CacheDuration);

        _logger.LogInformation("Discovered {Count} A2A tools for user {UserId}", tools.Count, GetUserIdForLogging());

        return new A2aMcpToolCache(tools, configMap);
    }

    private async Task<(A2aConnectionConfigV1? Config, A2aAgentCardV1? Card)> FetchAgentCardAsync(
        A2aConnectionConfigV1 config,
        CancellationToken cancellationToken)
    {
        try
        {
            var address = config.Address.TrimEnd('/');
            if (address.EndsWith("/a2a", StringComparison.OrdinalIgnoreCase))
                address = address[..^4];

            var cardUrl = $"{address}/.well-known/agent-card.json";

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = FetchTimeout;

            foreach (var header in config.Headers)
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);

            var response = await client.GetAsync(cardUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to fetch agent card from {Url}: HTTP {StatusCode}",
                    cardUrl,
                    (int)response.StatusCode);
                return (config, null);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var card = JsonSerializer.Deserialize<A2aAgentCardV1>(json);
            return (config, card);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch agent card for A2A server {Name}", config.Name);
            return (config, null);
        }
    }

    private static JsonElement BuildInputSchema()
    {
        var schema = new
        {
            type = "object",
            properties = new
            {
                message = new
                {
                    type = "string",
                    description = "The message to send to the A2A agent.",
                },
                contextId = new
                {
                    type = "string",
                    description = "Optional context ID for multi-turn conversations.",
                },
            },
            required = new[] { "message" },
        };

        return JsonSerializer.SerializeToElement(schema);
    }

    private static string? ExtractStringProperty(JsonElement? element, string propertyName)
    {
        if (element is not { ValueKind: JsonValueKind.Object } obj)
            return null;

        return obj.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }

    private string GetToolsCacheKey() => $"a2a_mcp_tools:{GetUserIdForLogging()}";

    private string GetConfigMapCacheKey() => $"a2a_mcp_config_map:{GetUserIdForLogging()}";

    private string GetUserIdForLogging()
    {
        return _identityContext.IsAuthenticated
            ? _identityContext.UserId.ToString()
            : "anonymous";
    }

    private sealed record A2aMcpToolCache(IReadOnlyList<Tool> Tools, Dictionary<string, A2aConnectionConfigV1> ConfigMap);
}

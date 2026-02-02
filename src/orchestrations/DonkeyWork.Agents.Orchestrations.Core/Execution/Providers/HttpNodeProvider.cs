using System.Text;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Providers;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Providers;

/// <summary>
/// Provider for HTTP-related node executions.
/// </summary>
[NodeProvider]
public class HttpNodeProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly ILogger<HttpNodeProvider> _logger;

    public HttpNodeProvider(
        IHttpClientFactory httpClientFactory,
        ITemplateRenderer templateRenderer,
        ILogger<HttpNodeProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _templateRenderer = templateRenderer;
        _logger = logger;
    }

    [NodeMethod(NodeType.HttpRequest)]
    public async Task<HttpRequestNodeOutput> ExecuteHttpRequestAsync(
        HttpRequestNodeConfiguration config,
        CancellationToken cancellationToken)
    {
        // Resolve URL with template renderer
        var resolvedUrl = await _templateRenderer.RenderAsync(config.Url, cancellationToken);

        _logger.LogDebug("HTTP Request: {Method} {Url}", config.Method, resolvedUrl);

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);

        var httpMethod = config.Method switch
        {
            Contracts.Nodes.Enums.HttpMethod.Get => System.Net.Http.HttpMethod.Get,
            Contracts.Nodes.Enums.HttpMethod.Post => System.Net.Http.HttpMethod.Post,
            Contracts.Nodes.Enums.HttpMethod.Put => System.Net.Http.HttpMethod.Put,
            Contracts.Nodes.Enums.HttpMethod.Patch => System.Net.Http.HttpMethod.Patch,
            Contracts.Nodes.Enums.HttpMethod.Delete => System.Net.Http.HttpMethod.Delete,
            Contracts.Nodes.Enums.HttpMethod.Head => System.Net.Http.HttpMethod.Head,
            Contracts.Nodes.Enums.HttpMethod.Options => System.Net.Http.HttpMethod.Options,
            _ => throw new InvalidOperationException($"Unsupported HTTP method: {config.Method}")
        };

        var request = new HttpRequestMessage(httpMethod, resolvedUrl);

        // Add headers
        if (config.Headers?.Items != null)
        {
            foreach (var header in config.Headers.Items)
            {
                var resolvedValue = await _templateRenderer.RenderAsync(header.Value, cancellationToken);
                request.Headers.TryAddWithoutValidation(header.Key, resolvedValue);
            }
        }

        // Add body if present
        if (!string.IsNullOrEmpty(config.Body))
        {
            var resolvedBody = await _templateRenderer.RenderAsync(config.Body, cancellationToken);
            var contentType = "application/json";
            if (request.Headers.TryGetValues("Content-Type", out var contentTypes))
            {
                contentType = contentTypes.First();
                request.Headers.Remove("Content-Type");
            }
            request.Content = new StringContent(resolvedBody, Encoding.UTF8, contentType);
        }

        var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        var responseHeaders = response.Headers
            .Concat(response.Content.Headers)
            .ToDictionary(h => h.Key, h => string.Join(", ", h.Value));

        _logger.LogDebug(
            "HTTP Response: {StatusCode} - Body length: {Length}",
            (int)response.StatusCode,
            responseBody.Length);

        return new HttpRequestNodeOutput
        {
            StatusCode = (int)response.StatusCode,
            Body = responseBody,
            Headers = responseHeaders
        };
    }
}

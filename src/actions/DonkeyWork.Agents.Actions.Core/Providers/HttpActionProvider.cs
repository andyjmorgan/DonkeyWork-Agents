using System.Diagnostics;
using DonkeyWork.Agents.Actions.Contracts.Attributes;
using DonkeyWork.Agents.Actions.Contracts.Services;
using DonkeyWork.Agents.Actions.Contracts.Types;

namespace DonkeyWork.Agents.Actions.Core.Providers;

/// <summary>
/// Provider for HTTP request actions
/// </summary>
[ActionProvider]
public class HttpActionProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IParameterResolver _parameterResolver;

    public HttpActionProvider(
        IHttpClientFactory httpClientFactory,
        IParameterResolver parameterResolver)
    {
        _httpClientFactory = httpClientFactory;
        _parameterResolver = parameterResolver;
    }

    [ActionMethod("http_request")]
    public async Task<HttpRequestOutput> ExecuteAsync(
        HttpRequestParameters parameters,
        object? context = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Create HTTP client
            var client = _httpClientFactory.CreateClient();
            var timeout = _parameterResolver.Resolve(parameters.TimeoutSeconds, context);
            client.Timeout = TimeSpan.FromSeconds(timeout);

            // Create request
            var httpMethod = new System.Net.Http.HttpMethod(parameters.Method.ToString());
            var request = new HttpRequestMessage(httpMethod, parameters.Url);

            // Add headers
            if (parameters.Headers != null && !parameters.Headers.IsEmpty)
            {
                if (parameters.Headers.UseVariable && !string.IsNullOrEmpty(parameters.Headers.Variable))
                {
                    // Resolve the variable to a dictionary
                    var resolvedHeaders = _parameterResolver.ResolveHeaders(parameters.Headers.Variable, context);
                    foreach (var header in resolvedHeaders)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
                else
                {
                    // Use manual items, resolving any variables in keys/values
                    foreach (var item in parameters.Headers.Items)
                    {
                        var key = _parameterResolver.ResolveString(item.Key, context);
                        var value = _parameterResolver.ResolveString(item.Value, context);
                        if (!string.IsNullOrEmpty(key))
                        {
                            request.Headers.TryAddWithoutValidation(key, value);
                        }
                    }
                }
            }

            // Add body for POST/PUT/PATCH
            var bodyContent = parameters.Body.HasValue
                ? _parameterResolver.ResolveString(parameters.Body.Value.RawValue, context)
                : null;

            if (!string.IsNullOrEmpty(bodyContent) &&
                (parameters.Method == HttpMethod.POST ||
                 parameters.Method == HttpMethod.PUT ||
                 parameters.Method == HttpMethod.PATCH))
            {
                request.Content = new StringContent(
                    bodyContent,
                    System.Text.Encoding.UTF8,
                    "application/json");
            }

            // Execute request
            var response = await client.SendAsync(request, cancellationToken);

            // Read response
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            stopwatch.Stop();

            // Extract headers
            var responseHeaders = new Dictionary<string, string>();
            foreach (var header in response.Headers)
            {
                responseHeaders[header.Key] = string.Join(", ", header.Value);
            }
            foreach (var header in response.Content.Headers)
            {
                responseHeaders[header.Key] = string.Join(", ", header.Value);
            }

            return new HttpRequestOutput
            {
                StatusCode = (int)response.StatusCode,
                Body = responseBody,
                Headers = responseHeaders,
                IsSuccess = response.IsSuccessStatusCode,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return new HttpRequestOutput
            {
                StatusCode = 0,
                Body = $"Error: {ex.Message}",
                Headers = new Dictionary<string, string>(),
                IsSuccess = false,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
    }
}

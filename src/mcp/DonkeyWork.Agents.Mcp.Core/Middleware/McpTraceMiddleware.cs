using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using DonkeyWork.Agents.Mcp.Core.Services;
using DonkeyWork.Agents.Persistence.Entities.Mcp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Mcp.Core.Middleware;

public class McpTraceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<McpTraceMiddleware> _logger;
    private const int MaxBodySize = 1_048_576; // 1 MB

    public McpTraceMiddleware(RequestDelegate next, ILogger<McpTraceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsJsonRpcRequest(context.Request))
        {
            await _next(context);
            return;
        }

        var startedAt = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        string requestBody;
        string? method = null;
        string? jsonRpcId = null;

        try
        {
            context.Request.EnableBuffering();
            requestBody = await ReadBodyAsync(context.Request.Body);
            context.Request.Body.Position = 0;

            (method, jsonRpcId) = ExtractJsonRpcFields(requestBody);

            if (method is null && jsonRpcId is null && !requestBody.Contains("jsonrpc"))
            {
                await _next(context);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read MCP request body for tracing");
            await _next(context);
            return;
        }

        var originalBodyStream = context.Response.Body;
        using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            string? responseBody = null;
            try
            {
                responseBuffer.Position = 0;
                var rawResponse = await ReadBodyAsync(responseBuffer);
                responseBody = ExtractSsePayload(rawResponse);

                responseBuffer.Position = 0;
                await responseBuffer.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to capture MCP response body for tracing");
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }

            var completedAt = DateTimeOffset.UtcNow;
            var httpStatusCode = context.Response.StatusCode;
            var isSuccess = httpStatusCode >= 200 && httpStatusCode < 400;
            var errorMessage = !isSuccess ? ExtractErrorMessage(responseBody) : null;

            var userId = ExtractUserId(context.User);
            var clientIp = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.Request.Headers.UserAgent.ToString();

            var trace = new McpTraceEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Method = method ?? "unknown",
                JsonRpcId = jsonRpcId,
                RequestBody = requestBody,
                ResponseBody = responseBody,
                HttpStatusCode = httpStatusCode,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                ClientIpAddress = clientIp,
                UserAgent = string.IsNullOrEmpty(userAgent) ? null : userAgent,
                StartedAt = startedAt,
                CompletedAt = completedAt,
                DurationMs = (int)stopwatch.ElapsedMilliseconds,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            try
            {
                var repository = context.RequestServices.GetRequiredService<IMcpTraceRepository>();
                await repository.CreateAsync(trace, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist MCP trace for method {Method}", method);
            }
        }
    }

    private static bool IsJsonRpcRequest(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method))
            return false;

        var contentType = request.ContentType;
        return contentType is not null && contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> ReadBodyAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        var buffer = new char[MaxBodySize];
        var charsRead = await reader.ReadAsync(buffer, 0, MaxBodySize);
        return new string(buffer, 0, charsRead);
    }

    public static (string? Method, string? Id) ExtractJsonRpcFields(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return (null, null);

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            string? method = null;
            string? id = null;

            if (root.TryGetProperty("method", out var methodProp))
                method = methodProp.GetString();

            if (root.TryGetProperty("id", out var idProp))
                id = idProp.ValueKind == JsonValueKind.Number
                    ? idProp.GetRawText()
                    : idProp.GetString();

            return (method, id);
        }
        catch
        {
            return (null, null);
        }
    }

    public static string ExtractSsePayload(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return raw;

        if (!raw.Contains("data:"))
            return raw;

        var lines = raw.Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith("data:", StringComparison.Ordinal))
                return line.Substring(5).TrimStart();
        }

        return raw;
    }

    private static string? ExtractErrorMessage(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("error", out var errorProp))
            {
                if (errorProp.TryGetProperty("message", out var messageProp))
                    return messageProp.GetString();

                return errorProp.GetRawText();
            }
        }
        catch
        {
            // Not valid JSON or no error field
        }

        return null;
    }

    private static Guid? ExtractUserId(ClaimsPrincipal? user)
    {
        var sub = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user?.FindFirst("sub")?.Value;

        return Guid.TryParse(sub, out var userId) ? userId : null;
    }
}

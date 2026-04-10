using System.Net.Http.Json;
using CodeSandbox.Contracts.Requests.Tools;
using CodeSandbox.Contracts.Responses;
using CodeSandbox.Manager.Services.Container;

namespace CodeSandbox.Manager.Services.Executor;

internal class ExecutorClient : IExecutorClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISandboxService _sandboxService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ExecutorClient> _logger;

    public ExecutorClient(
        IHttpClientFactory httpClientFactory,
        ISandboxService sandboxService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ExecutorClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _sandboxService = sandboxService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Task<ToolResponse> BashAsync(BashRequest request, CancellationToken ct) =>
        PostToolAsync("bash", request, ct);

    public Task<ToolResponse> ReadAsync(ReadRequest request, CancellationToken ct) =>
        PostToolAsync("read", request, ct);

    public Task<ToolResponse> EditAsync(EditRequest request, CancellationToken ct) =>
        PostToolAsync("edit", request, ct);

    public Task<ToolResponse> WriteAsync(WriteRequest request, CancellationToken ct) =>
        PostToolAsync("write", request, ct);

    public Task<ToolResponse> MultiEditAsync(MultiEditRequest request, CancellationToken ct) =>
        PostToolAsync("multiedit", request, ct);

    public Task<ToolResponse> GlobAsync(GlobRequest request, CancellationToken ct) =>
        PostToolAsync("glob", request, ct);

    public Task<ToolResponse> GrepAsync(GrepRequest request, CancellationToken ct) =>
        PostToolAsync("grep", request, ct);

    public Task<ToolResponse> ResumeAsync(ResumeRequest request, CancellationToken ct) =>
        PostToolAsync("resume", request, ct);

    private async Task<ToolResponse> PostToolAsync<TRequest>(string toolName, TRequest request, CancellationToken ct)
    {
        var sandboxId = _httpContextAccessor.HttpContext?.Request.Headers["x-sandbox-id"].ToString();
        if (string.IsNullOrEmpty(sandboxId))
        {
            return new ToolResponse { Output = "Error: x-sandbox-id header is required", IsError = true };
        }

        string podIp;
        try
        {
            podIp = await _sandboxService.GetPodIpAsync(sandboxId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve pod IP for sandbox {SandboxId}", sandboxId);
            return new ToolResponse { Output = $"Error: failed to resolve sandbox: {ex.Message}", IsError = true };
        }

        var httpClient = _httpClientFactory.CreateClient("executor");
        var url = $"http://{podIp}:8666/api/tools/{toolName}";

        _logger.LogDebug("Forwarding {Tool} to {Url} for sandbox {SandboxId}", toolName, url, sandboxId);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync(url, request, ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error forwarding {Tool} to sandbox {SandboxId}", toolName, sandboxId);
            return new ToolResponse { Output = $"Error: failed to reach sandbox executor: {ex.Message}", IsError = true };
        }

        _ = _sandboxService.UpdateLastActivityAsync(sandboxId, CancellationToken.None);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "Executor returned {StatusCode} for {Tool} on sandbox {SandboxId}: {Body}",
                response.StatusCode, toolName, sandboxId, body);
            return new ToolResponse { Output = $"Error: executor returned {(int)response.StatusCode}: {body}", IsError = true };
        }

        var toolResponse = await response.Content.ReadFromJsonAsync<ToolResponse>(ct);
        return toolResponse ?? new ToolResponse { Output = "Error: empty response from executor", IsError = true };
    }
}

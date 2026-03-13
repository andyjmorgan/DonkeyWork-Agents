using System.Text.Json;
using CodeSandbox.Manager.Models;
using CodeSandbox.Manager.Models.Api;
using CodeSandbox.Manager.Services.Mcp;

namespace CodeSandbox.Manager.Endpoints;

public static class McpEndpoints
{
    private static readonly JsonSerializerOptions SseJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static void MapMcpEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mcp")
            .WithTags("MCP");

        group.MapPost("/find", HandleFindMcpServer)
            .WithName("FindMcpServer")
            .WithSummary("Find an existing MCP server by userId and configId")
            .Produces<McpServerInfo>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", HandleCreateMcpServer)
            .WithName("CreateMcpServer")
            .WithSummary("Create a new MCP server and stream container events via SSE")
            .Produces(StatusCodes.Status200OK, contentType: "text/event-stream");

        group.MapPost("/{podName}/start", HandleStartMcpProcess)
            .WithName("StartMcpProcess")
            .WithSummary("Start the MCP process in a pod and stream events via SSE")
            .Produces(StatusCodes.Status200OK, contentType: "text/event-stream");

        group.MapPost("/{podName}/proxy", HandleProxyMcpRequest)
            .WithName("ProxyMcpRequest")
            .WithSummary("Proxy a JSON-RPC request to the MCP server")
            .Produces<McpProxyResponse>();

        group.MapGet("/{podName}/status", HandleGetMcpStatus)
            .WithName("GetMcpStatus")
            .WithSummary("Get the current status of the MCP process")
            .Produces<McpStatusResponse>();

        group.MapDelete("/{podName}", HandleDeleteMcpServer)
            .WithName("DeleteMcpServer")
            .WithSummary("Delete an MCP server")
            .Produces<DeleteSandboxResponse>();
    }

    private static async Task<IResult> HandleFindMcpServer(
        FindMcpServerApiRequest request,
        IMcpContainerService mcpService,
        CancellationToken ct)
    {
        var result = await mcpService.FindMcpServerAsync(request.UserId, request.ConfigId, ct);
        if (result is null)
            return Results.NotFound();

        return Results.Ok(result);
    }

    private static async Task HandleCreateMcpServer(
        CreateMcpServerApiRequest request,
        IMcpContainerService mcpService,
        ILogger<Program> logger,
        HttpContext context,
        CancellationToken ct)
    {
        logger.LogInformation("REST CreateMcpServer: UserId={UserId}, ConfigId={ConfigId}",
            request.UserId, request.ConfigId);

        var serviceRequest = new CreateMcpServerRequest
        {
            UserId = request.UserId,
            McpServerConfigId = request.ConfigId,
            Command = request.Command,
            Arguments = request.Args ?? [],
            PreExecScripts = request.PreExecScripts ?? [],
            TimeoutSeconds = request.Timeout,
            EnvironmentVariables = request.EnvVars,
            WorkingDirectory = request.WorkDir,
        };

        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";

        await foreach (var evt in mcpService.CreateMcpServerAsync(serviceRequest, ct))
        {
            var json = JsonSerializer.Serialize<object>(evt, SseJsonOptions);
            await context.Response.WriteAsync($"data: {json}\n\n", ct);
            await context.Response.Body.FlushAsync(ct);
        }
    }

    private static async Task HandleStartMcpProcess(
        string podName,
        StartMcpProcessApiRequest request,
        IMcpContainerService mcpService,
        ILogger<Program> logger,
        HttpContext context,
        CancellationToken ct)
    {
        logger.LogInformation("REST StartMcpProcess: PodName={PodName}, Command={Command}",
            podName, request.Command);

        var serviceRequest = new McpStartRequest
        {
            Command = request.Command,
            Arguments = request.Args ?? [],
            PreExecScripts = request.PreExecScripts ?? [],
            TimeoutSeconds = request.Timeout,
            WorkingDirectory = request.WorkDir,
        };

        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";

        await foreach (var evt in mcpService.StartMcpProcessAsync(podName, serviceRequest, ct))
        {
            var json = JsonSerializer.Serialize(evt, SseJsonOptions);
            await context.Response.WriteAsync($"data: {json}\n\n", ct);
            await context.Response.Body.FlushAsync(ct);
        }
    }

    private static async Task<IResult> HandleProxyMcpRequest(
        string podName,
        ProxyMcpApiRequest request,
        IMcpContainerService mcpService,
        CancellationToken ct)
    {
        var result = await mcpService.ProxyMcpRequestAsync(podName, request.Body, request.TimeoutSeconds, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleGetMcpStatus(
        string podName,
        IMcpContainerService mcpService,
        CancellationToken ct)
    {
        var result = await mcpService.GetMcpStatusAsync(podName, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleDeleteMcpServer(
        string podName,
        IMcpContainerService mcpService,
        CancellationToken ct)
    {
        var result = await mcpService.DeleteMcpServerAsync(podName, ct);
        return Results.Ok(result);
    }
}

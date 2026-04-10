using System.Text.Json;
using CodeSandbox.Manager.Models;
using CodeSandbox.Manager.Models.Api;
using CodeSandbox.Manager.Services.Container;

namespace CodeSandbox.Manager.Endpoints;

public static class SandboxEndpoints
{
    private static readonly JsonSerializerOptions SseJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static void MapSandboxEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sandbox")
            .WithTags("Sandbox");

        group.MapPost("/find", HandleFindSandbox)
            .WithName("FindSandbox")
            .WithSummary("Find an existing sandbox by userId and conversationId")
            .Produces<SandboxInfo>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", HandleCreateSandbox)
            .WithName("CreateSandbox")
            .WithSummary("Create a new sandbox and stream container events via SSE")
            .Produces(StatusCodes.Status200OK, contentType: "text/event-stream");

        group.MapPost("/{podName}/execute", HandleExecuteCommand)
            .WithName("ExecuteCommand")
            .WithSummary("Execute a command in a sandbox")
            .Produces<CodeSandbox.Contracts.Responses.ToolResponse>();

        group.MapDelete("/{podName}", HandleDeleteSandbox)
            .WithName("DeleteSandbox")
            .WithSummary("Delete a sandbox")
            .Produces<DeleteSandboxResponse>();
    }

    private static async Task<IResult> HandleFindSandbox(
        FindSandboxApiRequest request,
        ISandboxService sandboxService,
        CancellationToken ct)
    {
        var result = await sandboxService.FindSandboxAsync(request.UserId, request.ConversationId, ct);
        if (result is null)
            return Results.NotFound();

        return Results.Ok(result);
    }

    private static async Task HandleCreateSandbox(
        CreateSandboxApiRequest request,
        ISandboxService sandboxService,
        ILogger<Program> logger,
        HttpContext context,
        CancellationToken ct)
    {
        logger.LogInformation("REST CreateSandbox: UserId={UserId}, ConversationId={ConversationId}",
            request.UserId, request.ConversationId);

        var serviceRequest = new CreateSandboxRequest
        {
            UserId = request.UserId,
            ConversationId = request.ConversationId,
            EnvironmentVariables = request.EnvVars,
            DynamicCredentialDomains = request.DynamicCredentialDomains ?? new List<string>(),
        };

        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";

        await foreach (var evt in sandboxService.CreateSandboxAsync(serviceRequest, ct))
        {
            var json = JsonSerializer.Serialize<object>(evt, SseJsonOptions);
            await context.Response.WriteAsync($"data: {json}\n\n", ct);
            await context.Response.Body.FlushAsync(ct);
        }
    }

    private static async Task<IResult> HandleExecuteCommand(
        string podName,
        ExecuteCommandApiRequest request,
        ISandboxService sandboxService,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        logger.LogInformation("REST ExecuteCommand: PodName={PodName}, Command={Command}",
            podName, request.Command);

        var executionRequest = new ExecutionRequest
        {
            Command = request.Command,
            TimeoutSeconds = request.TimeoutSeconds,
        };

        var result = await sandboxService.ExecuteCommandAsync(podName, executionRequest, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleDeleteSandbox(
        string podName,
        ISandboxService sandboxService,
        CancellationToken ct)
    {
        var result = await sandboxService.DeleteContainerAsync(podName, ct);
        return Results.Ok(result);
    }
}

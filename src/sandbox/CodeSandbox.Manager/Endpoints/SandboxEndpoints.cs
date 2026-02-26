using CodeSandbox.Manager.Models;
using CodeSandbox.Manager.Services.Container;
using CodeSandbox.Manager.Services.Terminal;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CodeSandbox.Manager.Endpoints;

public static class SandboxEndpoints
{
    public static void MapSandboxEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sandbox")
            .WithTags("Sandboxes");

        group.MapPost("/", CreateSandbox)
            .WithName("CreateSandbox")
            .WithSummary("Create a new sandbox")
            .WithDescription("Creates a new sandbox container assigned to a userId and conversationId. Always waits for ready and streams SSE creation events.")
            .Produces<SandboxInfo>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/", ListSandboxes)
            .WithName("ListSandboxes")
            .WithSummary("List all sandboxes")
            .WithDescription("Retrieves a list of all sandbox containers.")
            .Produces<List<SandboxInfo>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/{podName}", GetSandbox)
            .WithName("GetSandbox")
            .WithSummary("Get a specific sandbox")
            .WithDescription("Retrieves detailed information about a specific sandbox by its pod name")
            .Produces<SandboxInfo>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapDelete("/{podName}", DeleteSandbox)
            .WithName("DeleteSandbox")
            .WithSummary("Delete a sandbox")
            .WithDescription("Deletes a sandbox container")
            .Produces<DeleteSandboxResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapDelete("/", DeleteAllSandboxes)
            .WithName("DeleteAllSandboxes")
            .WithSummary("Delete all sandboxes")
            .WithDescription("Deletes all sandbox containers.")
            .Produces<DeleteAllSandboxesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/{sandboxId}/execute", ExecuteCommand)
            .WithName("ExecuteCommand")
            .WithSummary("Execute a command in a sandbox")
            .WithDescription("Forwards a command execution request to the CodeExecution API running inside the specified sandbox. Returns SSE stream with output and completion events.")
            .Produces<ExecutionRequest>(StatusCodes.Status200OK, "text/event-stream")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/{sandboxId}/terminal", HandleTerminalWebSocket)
            .WithName("TerminalWebSocket")
            .WithSummary("Open a terminal WebSocket to a sandbox")
            .WithDescription("Establishes a WebSocket connection for interactive terminal access to the sandbox container via Kubernetes exec.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task CreateSandbox(
        CreateSandboxRequest request,
        ISandboxService sandboxService,
        ILogger<Program> logger,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Create sandbox endpoint called. UserId: {UserId}, ConversationId: {ConversationId}",
            request.UserId, request.ConversationId);

        // Set headers for SSE
        context.Response.Headers["Content-Type"] = "text/event-stream";
        context.Response.Headers["Cache-Control"] = "no-cache";
        context.Response.Headers["Connection"] = "keep-alive";

        try
        {
            await foreach (var evt in sandboxService.CreateSandboxAsync(request, cancellationToken))
            {
                logger.LogInformation(
                    "Streaming event. Type: {EventType}, PodName: {PodName}",
                    evt.EventType, evt.PodName);

                var json = System.Text.Json.JsonSerializer.Serialize(evt, evt.GetType(), new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });

                var sseMessage = $"data: {json}\n\n";
                await context.Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes(sseMessage), cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid request to create sandbox");

            var errorEvent = new ContainerFailedEvent
            {
                PodName = "(none)",
                Reason = $"Validation error: {ex.Message}"
            };

            var json = System.Text.Json.JsonSerializer.Serialize(errorEvent, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            var sseMessage = $"data: {json}\n\n";
            await context.Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes(sseMessage), cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create sandbox at API layer");

            var errorEvent = new ContainerFailedEvent
            {
                PodName = "(none)",
                Reason = $"Unexpected error: {ex.Message}"
            };

            var json = System.Text.Json.JsonSerializer.Serialize(errorEvent, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            var sseMessage = $"data: {json}\n\n";
            await context.Response.Body.WriteAsync(System.Text.Encoding.UTF8.GetBytes(sseMessage), cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }
    }

    private static async Task<Results<Ok<List<SandboxInfo>>, ProblemHttpResult>> ListSandboxes(
        ISandboxService sandboxService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var containers = await sandboxService.ListContainersAsync(cancellationToken);
            return TypedResults.Ok(containers);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list sandboxes at API layer");
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to list sandboxes");
        }
    }

    private static async Task<Results<Ok<SandboxInfo>, NotFound<object>, ProblemHttpResult>> GetSandbox(
        string podName,
        ISandboxService sandboxService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var container = await sandboxService.GetContainerAsync(podName, cancellationToken);

            if (container == null)
            {
                return TypedResults.NotFound((object)new { error = $"Sandbox {podName} not found" });
            }

            return TypedResults.Ok(container);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get sandbox at API layer: {PodName}", podName);
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to get sandbox");
        }
    }

    private static async Task<Results<Ok<DeleteSandboxResponse>, NotFound<DeleteSandboxResponse>, ProblemHttpResult>> DeleteSandbox(
        string podName,
        ISandboxService sandboxService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await sandboxService.DeleteContainerAsync(podName, cancellationToken);

            if (!response.Success)
            {
                return TypedResults.NotFound(response);
            }

            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete sandbox at API layer: {PodName}", podName);
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to delete sandbox");
        }
    }

    private static async Task<Results<Ok<DeleteAllSandboxesResponse>, ProblemHttpResult>> DeleteAllSandboxes(
        ISandboxService sandboxService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await sandboxService.DeleteAllContainersAsync(cancellationToken);
            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete all sandboxes at API layer");
            return TypedResults.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to delete all sandboxes");
        }
    }

    private static async Task ExecuteCommand(
        string sandboxId,
        ExecutionRequest request,
        ISandboxService sandboxService,
        ILogger<Program> logger,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";

            await sandboxService.ExecuteCommandAsync(sandboxId, request, context.Response.Body, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while executing command in sandbox {SandboxId}", sandboxId);

            var errorJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                error = ex.Message,
                sandboxId
            });
            await context.Response.WriteAsync($"data: {errorJson}\n\n", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute command in sandbox {SandboxId}", sandboxId);

            var errorJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                error = "Failed to execute command",
                message = ex.Message,
                sandboxId
            });
            await context.Response.WriteAsync($"data: {errorJson}\n\n", cancellationToken);
        }
    }

    private static async Task HandleTerminalWebSocket(
        string sandboxId,
        ITerminalService terminalService,
        ISandboxService sandboxService,
        ILogger<Program> logger,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            logger.LogWarning("Non-WebSocket request to terminal endpoint for sandbox {SandboxId}", sandboxId);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("WebSocket connection required", cancellationToken);
            return;
        }

        var container = await sandboxService.GetContainerAsync(sandboxId, cancellationToken);
        if (container == null)
        {
            logger.LogWarning("Sandbox not found for terminal connection: {SandboxId}", sandboxId);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync($"Sandbox {sandboxId} not found", cancellationToken);
            return;
        }

        if (!container.IsReady)
        {
            logger.LogWarning("Sandbox not ready for terminal connection: {SandboxId}", sandboxId);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync($"Sandbox {sandboxId} is not ready", cancellationToken);
            return;
        }

        logger.LogInformation("Accepting WebSocket terminal connection for sandbox {SandboxId}", sandboxId);

        try
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await terminalService.HandleTerminalSessionAsync(sandboxId, webSocket, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Terminal WebSocket error for sandbox {SandboxId}", sandboxId);
        }
    }
}

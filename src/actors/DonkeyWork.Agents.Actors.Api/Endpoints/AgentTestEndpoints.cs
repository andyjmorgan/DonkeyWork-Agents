using System.Text.Json;
using System.Text.Json.Serialization;
using DonkeyWork.Agents.Actors.Api.Observers;
using DonkeyWork.Agents.Actors.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Contracts;
using DonkeyWork.Agents.Actors.Contracts.Events;
using DonkeyWork.Agents.Actors.Contracts.Grains;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DonkeyWork.Agents.Actors.Api.Endpoints;

public static class AgentTestEndpoints
{
    private static readonly JsonSerializerOptions ContractJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        AllowOutOfOrderMetadataProperties = true,
    };

    public static IEndpointRouteBuilder MapAgentTestEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/agent-test", HandleTestAsync)
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task HandleTestAsync(
        HttpContext context,
        IGrainFactory grainFactory,
        IIdentityContext identityContext)
    {
        if (!identityContext.IsAuthenticated)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Read and parse request body
        AgentTestRequest? request;
        try
        {
            request = await JsonSerializer.DeserializeAsync<AgentTestRequest>(
                context.Request.Body, ContractJsonOptions, context.RequestAborted);
        }
        catch (JsonException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid request body");
            return;
        }

        if (request?.Contract.ValueKind == JsonValueKind.Undefined || string.IsNullOrWhiteSpace(request?.Input))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Contract and input are required");
            return;
        }

        // Deserialize the contract
        AgentContract contract;
        try
        {
            contract = JsonSerializer.Deserialize<AgentContract>(
                request.Contract.GetRawText(), ContractJsonOptions)!;
        }
        catch (JsonException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync($"Invalid contract: {ex.Message}");
            return;
        }

        // Create a temporary test grain
        var testId = Guid.NewGuid();
        var grainKey = AgentKeys.Test(identityContext.UserId, testId);

        RequestContext.Set(GrainCallContextKeys.UserId, identityContext.UserId.ToString());

        var grain = grainFactory.GetGrain<IAgentGrain>(grainKey);
        using var observer = new SseObserver();
        var observerRef = grainFactory.CreateObjectReference<IAgentResponseObserver>(observer);

        // Set SSE response headers
        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";

        // Fire the grain execution (don't await — read events from observer)
        var runTask = Task.Run(async () =>
        {
            try
            {
                await grain.RunAsync(contract, request.Input, observerRef);
            }
            catch (Exception ex)
            {
                observer.OnEvent(new StreamErrorEvent(grainKey, ex.Message));
            }
            finally
            {
                observer.Complete();
            }
        }, context.RequestAborted);

        // Stream events as SSE
        try
        {
            await foreach (var evt in observer.ReadAllAsync(context.RequestAborted))
            {
                var json = SerializeEvent(evt);
                await context.Response.WriteAsync($"data: {json}\n\n", context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);

                if (evt is StreamCompleteEvent or StreamErrorEvent)
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected
        }

        // Wait for the grain to finish
        try
        {
            await runTask;
        }
        catch
        {
            // Already handled via observer
        }
    }

    private static string SerializeEvent(StreamEventBase evt)
    {
        var bytes = evt switch
        {
            StreamMessageEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamMessageEvent),
            StreamThinkingEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamThinkingEvent),
            StreamToolUseEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamToolUseEvent),
            StreamToolResultEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamToolResultEvent),
            StreamToolCompleteEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamToolCompleteEvent),
            StreamWebSearchEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamWebSearchEvent),
            StreamWebSearchCompleteEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamWebSearchCompleteEvent),
            StreamCitationEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamCitationEvent),
            StreamUsageEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamUsageEvent),
            StreamProgressEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamProgressEvent),
            StreamAgentSpawnEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamAgentSpawnEvent),
            StreamAgentCompleteEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamAgentCompleteEvent),
            StreamAgentResultDataEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamAgentResultDataEvent),
            StreamCompleteEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamCompleteEvent),
            StreamErrorEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamErrorEvent),
            StreamTurnStartEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamTurnStartEvent),
            StreamTurnEndEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamTurnEndEvent),
            StreamQueueStatusEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamQueueStatusEvent),
            StreamCancelledEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamCancelledEvent),
            StreamMcpServerStatusEvent e => JsonSerializer.SerializeToUtf8Bytes(e, AgentJsonContext.Default.StreamMcpServerStatusEvent),
            _ => JsonSerializer.SerializeToUtf8Bytes(new { agentKey = evt.AgentKey, eventType = evt.EventType }),
        };

        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}

public record AgentTestRequest(JsonElement Contract, string Input);

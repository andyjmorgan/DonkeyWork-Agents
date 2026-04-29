using System.Text.Json;
using System.Text.Json.Serialization;
using DonkeyWork.Agents.Actors.Api.EventBus;
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
        IIdentityContext identityContext,
        AgentEventConsumerFactory consumerFactory)
    {
        if (!identityContext.IsAuthenticated)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

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

        var testId = Guid.NewGuid();
        var conversationId = testId.ToString();
        var grainKey = AgentKeys.Test(identityContext.UserId, testId);

        RequestContext.Set(GrainCallContextKeys.UserId, identityContext.UserId.ToString());
        RequestContext.Set(GrainCallContextKeys.ConversationId, conversationId);

        var grain = grainFactory.GetGrain<IAgentGrain>(grainKey);

        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);

        var consumer = consumerFactory.CreateConsumer();
        var liveOpts = AgentEventConsumerFactory.LiveTailOptions(conversationId);

        var runTask = Task.Run(async () =>
        {
            try
            {
                await grain.RunAsync(contract, request.Input);
            }
            catch (Exception)
            {
                // Errors are emitted via JetStream by the grain
            }
            finally
            {
                cts.CancelAfter(TimeSpan.FromSeconds(5));
            }
        }, context.RequestAborted);

        try
        {
            await foreach (var delivered in consumer.GetMessagesAsync<StreamEventBase>(liveOpts, cts.Token))
            {
                var json = ConversationWebSocketEndpoints.ToJsonElement(delivered.Payload);
                await context.Response.WriteAsync($"data: {json}\n\n", context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);

                if (delivered.Payload is StreamCompleteEvent or StreamErrorEvent)
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected or run completed
        }

        try
        {
            await runTask;
        }
        catch
        {
            // Already handled via JetStream
        }
    }
}

public record AgentTestRequest(JsonElement Contract, string Input);

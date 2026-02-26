using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orleans.Contracts.Events;

namespace DonkeyWork.Agents.Orleans.Api.Observers;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(StreamMessageEvent))]
[JsonSerializable(typeof(StreamThinkingEvent))]
[JsonSerializable(typeof(StreamToolUseEvent))]
[JsonSerializable(typeof(StreamToolResultEvent))]
[JsonSerializable(typeof(StreamToolCompleteEvent))]
[JsonSerializable(typeof(StreamWebSearchEvent))]
[JsonSerializable(typeof(StreamWebSearchCompleteEvent))]
[JsonSerializable(typeof(WebSearchResultEntry))]
[JsonSerializable(typeof(StreamCitationEvent))]
[JsonSerializable(typeof(StreamUsageEvent))]
[JsonSerializable(typeof(StreamProgressEvent))]
[JsonSerializable(typeof(StreamAgentSpawnEvent))]
[JsonSerializable(typeof(StreamAgentCompleteEvent))]
[JsonSerializable(typeof(StreamAgentResultDataEvent))]
[JsonSerializable(typeof(StreamAgentCitation))]
[JsonSerializable(typeof(StreamCompleteEvent))]
[JsonSerializable(typeof(StreamErrorEvent))]
[JsonSerializable(typeof(StreamTurnStartEvent))]
[JsonSerializable(typeof(StreamTurnEndEvent))]
[JsonSerializable(typeof(StreamQueueStatusEvent))]
[JsonSerializable(typeof(StreamCancelledEvent))]
internal partial class AgentJsonContext : JsonSerializerContext;

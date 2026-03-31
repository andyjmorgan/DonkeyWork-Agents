using System.Text.Json.Serialization;
using DonkeyWork.Agents.Actors.Contracts.Events;

namespace DonkeyWork.Agents.Actors.Api.Observers;

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
[JsonSerializable(typeof(StreamRetryEvent))]
[JsonSerializable(typeof(StreamTurnStartEvent))]
[JsonSerializable(typeof(StreamTurnEndEvent))]
[JsonSerializable(typeof(StreamQueueStatusEvent))]
[JsonSerializable(typeof(StreamCancelledEvent))]
[JsonSerializable(typeof(StreamMcpServerStatusEvent))]
[JsonSerializable(typeof(StreamSandboxStatusEvent))]
internal partial class AgentJsonContext : JsonSerializerContext;

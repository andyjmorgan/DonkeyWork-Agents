using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ExecutionStartedEvent), nameof(ExecutionStartedEvent))]
[JsonDerivedType(typeof(NodeStartedEvent), nameof(NodeStartedEvent))]
[JsonDerivedType(typeof(TokenDeltaEvent), nameof(TokenDeltaEvent))]
[JsonDerivedType(typeof(ContentPartStartedEvent), nameof(ContentPartStartedEvent))]
[JsonDerivedType(typeof(ContentPartEndedEvent), nameof(ContentPartEndedEvent))]
[JsonDerivedType(typeof(NodeCompletedEvent), nameof(NodeCompletedEvent))]
[JsonDerivedType(typeof(ExecutionCompletedEvent), nameof(ExecutionCompletedEvent))]
[JsonDerivedType(typeof(ExecutionFailedEvent), nameof(ExecutionFailedEvent))]
public abstract class ExecutionEvent
{
    public Guid ExecutionId { get; set; }
    public DateTime Timestamp { get; set; }
}

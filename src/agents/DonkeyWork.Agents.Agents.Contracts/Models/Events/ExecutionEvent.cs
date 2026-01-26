using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Agents.Contracts.Models.Events;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ExecutionStartedEvent), "execution_started")]
[JsonDerivedType(typeof(NodeStartedEvent), "node_started")]
[JsonDerivedType(typeof(TokenDeltaEvent), "token_delta")]
[JsonDerivedType(typeof(NodeCompletedEvent), "node_completed")]
[JsonDerivedType(typeof(ExecutionCompletedEvent), "execution_completed")]
[JsonDerivedType(typeof(ExecutionFailedEvent), "execution_failed")]
public abstract class ExecutionEvent
{
    public Guid ExecutionId { get; set; }
    public DateTime Timestamp { get; set; }
}

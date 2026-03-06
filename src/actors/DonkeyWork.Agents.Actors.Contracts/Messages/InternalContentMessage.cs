namespace DonkeyWork.Agents.Actors.Contracts.Messages;

[GenerateSerializer]
public class InternalContentMessage : InternalMessage
{
    [Id(0)] public required string Content { get; set; }
    [Id(1)] public MessageOrigin Origin { get; set; }
    [Id(2)] public Guid? AgentId { get; set; }
    [Id(3)] public string? AgentName { get; set; }
}

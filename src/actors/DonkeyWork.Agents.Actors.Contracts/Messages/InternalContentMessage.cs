namespace DonkeyWork.Agents.Actors.Contracts.Messages;

[GenerateSerializer]
public class InternalContentMessage : InternalMessage
{
    [Id(0)] public required string Content { get; set; }
}

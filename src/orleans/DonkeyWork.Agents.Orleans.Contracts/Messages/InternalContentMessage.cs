namespace DonkeyWork.Agents.Orleans.Contracts.Messages;

[GenerateSerializer]
public class InternalContentMessage : InternalMessage
{
    [Id(0)] public required string Content { get; set; }
}

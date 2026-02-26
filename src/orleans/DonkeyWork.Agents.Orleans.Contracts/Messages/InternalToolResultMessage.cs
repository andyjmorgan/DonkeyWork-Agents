namespace DonkeyWork.Agents.Orleans.Contracts.Messages;

[GenerateSerializer]
public class InternalToolResultMessage : InternalMessage
{
    [Id(0)] public required string ToolUseId { get; set; }
    [Id(1)] public required string Content { get; set; }
    [Id(2)] public bool IsError { get; set; }
}

using System.Text.Json;

namespace DonkeyWork.Agents.Actors.Contracts.Messages;

[GenerateSerializer]
public sealed record ToolUseRecord(
    [property: Id(0)] string Id,
    [property: Id(1)] string Name,
    [property: Id(2)] JsonElement Input);

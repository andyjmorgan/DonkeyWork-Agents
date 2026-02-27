using System.Text.Json;
using Orleans.Serialization.Cloning;

namespace DonkeyWork.Agents.Actors.Contracts.Messages;

[RegisterCopier]
public sealed class JsonElementCopier : IDeepCopier<JsonElement>
{
    public JsonElement DeepCopy(JsonElement input, CopyContext context) => input.Clone();
}

[GenerateSerializer]
public class InternalAssistantMessage : InternalMessage
{
    [Id(0)] public string? TextContent { get; set; }
    [Id(1)] public List<ToolUseRecord> ToolUses { get; set; } = [];
    [Id(2)] public List<InternalContentBlock> ContentBlocks { get; set; } = [];
}

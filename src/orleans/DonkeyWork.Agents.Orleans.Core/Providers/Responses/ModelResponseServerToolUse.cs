using System.Text.Json;

namespace DonkeyWork.Agents.Orleans.Core.Providers.Responses;

internal class ModelResponseServerToolUse : ModelResponseBase
{
    public required int BlockIndex { get; set; }
    public required string ToolUseId { get; set; }
    public required string ToolName { get; set; }
    public JsonElement? Input { get; set; }
}

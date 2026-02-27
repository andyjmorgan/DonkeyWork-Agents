using System.Text.Json;

namespace DonkeyWork.Agents.Actors.Core.Providers.Responses;

internal class ModelResponseServerToolUse : ModelResponseBase
{
    public required int BlockIndex { get; set; }
    public required string ToolUseId { get; set; }
    public required string ToolName { get; set; }
    public JsonElement? Input { get; set; }
}

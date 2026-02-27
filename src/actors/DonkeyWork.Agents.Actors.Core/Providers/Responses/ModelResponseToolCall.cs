using System.Text.Json;

namespace DonkeyWork.Agents.Actors.Core.Providers.Responses;

internal class ModelResponseToolCall : ModelResponseBase
{
    public required int BlockIndex { get; set; }
    public required string ToolName { get; set; }
    public required string ToolUseId { get; set; }
    public required JsonElement Input { get; set; }
}

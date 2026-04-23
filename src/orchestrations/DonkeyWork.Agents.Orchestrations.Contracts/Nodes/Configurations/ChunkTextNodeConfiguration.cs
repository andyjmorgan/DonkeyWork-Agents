using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;

/// <summary>
/// Configuration for the ChunkText node — splits long markdown into ordered chunks
/// that respect heading, paragraph, and list boundaries so each chunk fits within
/// a downstream provider's input limit.
/// </summary>
[Node(
    DisplayName = "Chunk Text",
    Description = "Split long markdown text into ordered chunks respecting block boundaries",
    Category = "Audio",
    Icon = "scissors",
    Color = "pink")]
public sealed class ChunkTextNodeConfiguration : NodeConfiguration
{
    /// <inheritdoc />
    public override NodeType NodeType => NodeType.ChunkText;

    /// <summary>
    /// The markdown text to chunk. Supports Scriban template variables.
    /// </summary>
    [JsonPropertyName("inputText")]
    [ConfigurableField(Label = "Input Text", ControlType = ControlType.TextArea, Order = 10, Required = true,
        Description = "Markdown text to split into chunks. Use {{Steps.node_name.Property}} for variables.")]
    [Tab("Content", Order = 1, Icon = "file-text")]
    [SupportVariables]
    public required string InputText { get; init; }

    /// <summary>
    /// Target character budget per chunk. Blocks are packed greedily up to this size.
    /// </summary>
    [JsonPropertyName("targetCharCount")]
    [ConfigurableField(Label = "Target Chars", ControlType = ControlType.Slider, Order = 10,
        Description = "Target characters per chunk (chunker packs blocks up to this size).")]
    [Tab("Advanced", Order = 2, Icon = "sliders")]
    [Slider(Min = 500, Max = 7500, Step = 250, Default = 3000)]
    public int TargetCharCount { get; init; } = 3000;

    /// <summary>
    /// Hard ceiling per chunk. Used when a single block exceeds the target and
    /// sentence/item-level splitting is triggered.
    /// </summary>
    [JsonPropertyName("maxCharCount")]
    [ConfigurableField(Label = "Max Chars", ControlType = ControlType.Slider, Order = 20,
        Description = "Hard ceiling per chunk. OpenAI safe ~3800, Gemini safe ~7800.")]
    [Tab("Advanced", Order = 2)]
    [Slider(Min = 500, Max = 8000, Step = 250, Default = 3800)]
    public int MaxCharCount { get; init; } = 3800;
}

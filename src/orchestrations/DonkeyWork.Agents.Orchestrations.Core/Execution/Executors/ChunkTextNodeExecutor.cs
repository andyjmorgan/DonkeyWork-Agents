using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Executors;

public class ChunkTextNodeExecutor : NodeExecutor<ChunkTextNodeConfiguration, ChunkTextNodeOutput>
{
    private readonly IMarkdownChunker _chunker;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly ILogger<ChunkTextNodeExecutor> _logger;

    public ChunkTextNodeExecutor(
        IExecutionStreamWriter streamWriter,
        IExecutionContext context,
        IMarkdownChunker chunker,
        ITemplateRenderer templateRenderer,
        ILogger<ChunkTextNodeExecutor> logger)
        : base(streamWriter, context)
    {
        _chunker = chunker;
        _templateRenderer = templateRenderer;
        _logger = logger;
    }

    protected override async Task<ChunkTextNodeOutput> ExecuteInternalAsync(
        ChunkTextNodeConfiguration config,
        CancellationToken cancellationToken)
    {
        var rendered = await _templateRenderer.RenderAsync(config.InputText, cancellationToken);

        if (string.IsNullOrWhiteSpace(rendered))
        {
            throw new InvalidOperationException("Input text is empty after template rendering");
        }

        if (config.MaxCharCount < config.TargetCharCount)
        {
            throw new InvalidOperationException(
                $"MaxCharCount ({config.MaxCharCount}) must be >= TargetCharCount ({config.TargetCharCount})");
        }

        var chunks = _chunker.Chunk(rendered, new ChunkerOptions
        {
            TargetCharCount = config.TargetCharCount,
            MaxCharCount = config.MaxCharCount,
        });

        _logger.LogInformation(
            "ChunkText produced {ChunkCount} chunks (total input {InputLength} chars, target {Target})",
            chunks.Count, rendered.Length, config.TargetCharCount);

        return new ChunkTextNodeOutput
        {
            Chunks = chunks,
        };
    }
}

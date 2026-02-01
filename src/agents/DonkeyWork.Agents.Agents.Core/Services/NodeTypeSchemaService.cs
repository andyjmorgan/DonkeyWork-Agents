using DonkeyWork.Agents.Agents.Contracts.Models;
using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Common.Nodes.Enums;
using DonkeyWork.Agents.Common.Nodes.Schema;

namespace DonkeyWork.Agents.Agents.Core.Services;

/// <summary>
/// Provides node type information and configuration schemas.
/// </summary>
public class NodeTypeSchemaService : INodeTypeSchemaService
{
    private readonly INodeSchemaGenerator _schemaGenerator;
    private readonly Lazy<IReadOnlyList<NodeTypeInfo>> _nodeTypes;

    public NodeTypeSchemaService(INodeSchemaGenerator schemaGenerator)
    {
        _schemaGenerator = schemaGenerator;
        _nodeTypes = new Lazy<IReadOnlyList<NodeTypeInfo>>(GenerateNodeTypes);
    }

    public IReadOnlyList<NodeTypeInfo> GetNodeTypes()
    {
        return _nodeTypes.Value;
    }

    private IReadOnlyList<NodeTypeInfo> GenerateNodeTypes()
    {
        var nodeTypeDefinitions = new[]
        {
            new
            {
                Type = NodeType.Start,
                DisplayName = "Start",
                Description = "Entry point - validates input against schema",
                Category = "Flow",
                Icon = "play",
                Color = "green"
            },
            new
            {
                Type = NodeType.End,
                DisplayName = "End",
                Description = "Output and completion",
                Category = "Flow",
                Icon = "flag",
                Color = "orange"
            },
            new
            {
                Type = NodeType.Model,
                DisplayName = "Model",
                Description = "Call an LLM with configured prompts",
                Category = "AI",
                Icon = "brain",
                Color = "blue"
            },
            new
            {
                Type = NodeType.MessageFormatter,
                DisplayName = "Message Formatter",
                Description = "Format messages using Scriban templates",
                Category = "Utility",
                Icon = "file-text",
                Color = "cyan"
            },
            new
            {
                Type = NodeType.HttpRequest,
                DisplayName = "HTTP Request",
                Description = "Make HTTP requests to external APIs",
                Category = "Integration",
                Icon = "globe",
                Color = "purple"
            },
            new
            {
                Type = NodeType.Sleep,
                DisplayName = "Sleep",
                Description = "Pause execution for a specified duration",
                Category = "Utility",
                Icon = "clock",
                Color = "cyan"
            }
        };

        return nodeTypeDefinitions.Select(def => new NodeTypeInfo
        {
            Type = def.Type,
            DisplayName = def.DisplayName,
            Description = def.Description,
            Category = def.Category,
            Icon = def.Icon,
            Color = def.Color,
            ConfigSchema = _schemaGenerator.GenerateSchema(def.Type)
        }).ToList();
    }
}

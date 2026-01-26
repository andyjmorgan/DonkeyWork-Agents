using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Models.NodeConfigurations;
using DonkeyWork.Agents.Agents.Core.Execution.Outputs;
using Microsoft.Extensions.Logging;
using NJsonSchema;

namespace DonkeyWork.Agents.Agents.Core.Execution.Executors;

/// <summary>
/// Executor for Start nodes.
/// Validates input against the agent's InputSchema.
/// </summary>
public class StartNodeExecutor : NodeExecutor<StartNodeConfiguration, StartNodeOutput>
{
    private readonly ILogger<StartNodeExecutor> _logger;

    public StartNodeExecutor(ILogger<StartNodeExecutor> logger)
    {
        _logger = logger;
    }

    protected override async Task<StartNodeOutput> ExecuteInternalAsync(
        StartNodeConfiguration config,
        ExecutionContext context,
        CancellationToken cancellationToken)
    {
        // Parse the schema from context
        var schema = await JsonSchema.FromJsonAsync(context.InputSchema, cancellationToken);

        // Serialize the input to JSON for validation
        // If it's a JsonElement, use its raw text; otherwise serialize it
        string inputJson;
        if (context.Input is JsonElement jsonElement)
        {
            inputJson = jsonElement.GetRawText();
        }
        else
        {
            inputJson = JsonSerializer.Serialize(context.Input);
        }

        // Validate the input
        var errors = schema.Validate(inputJson);

        if (errors.Count > 0)
        {
            var errorMessages = string.Join("; ", errors.Select(e => $"{e.Path}: {e.Kind}"));
            throw new InvalidOperationException($"Input validation failed: {errorMessages}. Schema: {context.InputSchema}. Input: {inputJson}");
        }

        // Return the validated input
        return new StartNodeOutput
        {
            Input = context.Input
        };
    }
}

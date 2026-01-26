using System.Text.Json;
using DonkeyWork.Agents.Actions.Contracts.Services;
using DonkeyWork.Agents.Agents.Contracts.Models.NodeConfigurations;
using DonkeyWork.Agents.Agents.Core.Execution.Outputs;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Agents.Core.Execution.Executors;

/// <summary>
/// Executor for Action nodes.
/// Executes action providers with parameter resolution and expression evaluation.
/// </summary>
public class ActionNodeExecutor : NodeExecutor<ActionNodeConfiguration, ActionNodeOutput>
{
    private readonly IActionExecutor _actionExecutor;
    private readonly ILogger<ActionNodeExecutor> _logger;

    public ActionNodeExecutor(
        IActionExecutor actionExecutor,
        ILogger<ActionNodeExecutor> logger)
    {
        _actionExecutor = actionExecutor;
        _logger = logger;
    }

    protected override async Task<ActionNodeOutput> ExecuteInternalAsync(
        ActionNodeConfiguration config,
        ExecutionContext context,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Executing action node '{NodeName}' with action type '{ActionType}'",
            config.Name,
            config.ActionType);

        // Check if action is registered
        if (!_actionExecutor.IsActionRegistered(config.ActionType))
        {
            throw new InvalidOperationException(
                $"Action type '{config.ActionType}' is not registered. " +
                $"Available actions: {string.Join(", ", _actionExecutor.GetRegisteredActions())}");
        }

        try
        {
            // Convert ExecutionContext to Scriban context for expression resolution
            var scribanContext = context.ToScribanContext();

            // Deserialize parameters to object (the executor will handle conversion to the correct type)
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(config.Parameters.GetRawText());

            if (parameters == null)
            {
                throw new InvalidOperationException("Failed to deserialize action parameters");
            }

            // Execute the action
            var result = await _actionExecutor.ExecuteAsync(
                config.ActionType,
                parameters,
                scribanContext,
                cancellationToken);

            _logger.LogDebug(
                "Action node '{NodeName}' completed successfully",
                config.Name);

            // Return the action output
            return new ActionNodeOutput
            {
                ActionType = config.ActionType,
                Result = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to execute action node '{NodeName}' with action type '{ActionType}'",
                config.Name,
                config.ActionType);

            throw new InvalidOperationException(
                $"Action node '{config.Name}' failed: {ex.Message}",
                ex);
        }
    }
}

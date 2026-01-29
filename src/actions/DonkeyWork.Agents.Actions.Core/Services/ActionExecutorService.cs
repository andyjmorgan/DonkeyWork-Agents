using System.Text.Json;
using DonkeyWork.Agents.Actions.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actions.Core.Services;

/// <summary>
/// Scoped service for executing action providers.
/// </summary>
public class ActionExecutorService : IActionExecutor
{
    private readonly IActionRegistry _registry;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ActionExecutorService> _logger;

    public ActionExecutorService(
        IActionRegistry registry,
        IServiceProvider serviceProvider,
        ILogger<ActionExecutorService> logger)
    {
        _registry = registry;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<object> ExecuteAsync(
        string actionType,
        object parameters,
        object? context = null,
        CancellationToken cancellationToken = default)
    {
        var actionInfo = _registry.GetAction(actionType);
        if (actionInfo == null)
        {
            _logger.LogError("Action type '{ActionType}' not found in registry", actionType);
            throw new InvalidOperationException($"Action type '{actionType}' is not registered");
        }

        try
        {
            _logger.LogDebug("Executing action '{ActionType}'", actionType);

            // Resolve provider from scoped service provider
            var provider = _serviceProvider.GetRequiredService(actionInfo.ProviderType);

            // Convert parameters to the expected type if needed
            var typedParameters = ConvertParameters(parameters, actionInfo.ParameterType);

            // Build method parameters
            var methodParams = new List<object?> { typedParameters };

            if (actionInfo.HasContextParameter)
            {
                methodParams.Add(context);
            }

            if (actionInfo.HasCancellationTokenParameter)
            {
                methodParams.Add(cancellationToken);
            }

            // Invoke the method
            var result = actionInfo.Method.Invoke(provider, methodParams.ToArray());

            // Handle async methods
            if (result is Task task)
            {
                await task;

                var resultProperty = task.GetType().GetProperty("Result");
                if (resultProperty != null)
                {
                    result = resultProperty.GetValue(task);
                }
                else
                {
                    result = null;
                }
            }

            _logger.LogDebug("Action '{ActionType}' executed successfully", actionType);

            return result ?? new { success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing action '{ActionType}'", actionType);
            throw new InvalidOperationException($"Failed to execute action '{actionType}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public bool IsActionRegistered(string actionType)
    {
        return _registry.IsActionRegistered(actionType);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetRegisteredActions()
    {
        return _registry.GetRegisteredActions();
    }

    /// <summary>
    /// Converts parameters to the expected type if needed.
    /// Handles Dictionary -> strongly-typed parameter conversion.
    /// </summary>
    private static object ConvertParameters(object parameters, Type expectedType)
    {
        // If already the correct type, return as-is
        if (expectedType.IsInstanceOfType(parameters))
        {
            return parameters;
        }

        // If it's a dictionary, serialize and deserialize to the expected type
        if (parameters is IDictionary<string, object> dict)
        {
            var json = JsonSerializer.Serialize(dict);
            return JsonSerializer.Deserialize(json, expectedType)
                ?? throw new InvalidOperationException($"Failed to convert parameters to {expectedType.Name}");
        }

        // Try JSON round-trip for other types
        var serialized = JsonSerializer.Serialize(parameters);
        return JsonSerializer.Deserialize(serialized, expectedType)
            ?? throw new InvalidOperationException($"Failed to convert parameters to {expectedType.Name}");
    }
}

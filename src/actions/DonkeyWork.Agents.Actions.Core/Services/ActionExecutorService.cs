using System.Reflection;
using DonkeyWork.Agents.Actions.Contracts.Attributes;
using DonkeyWork.Agents.Actions.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actions.Core.Services;

/// <summary>
/// Service for discovering and executing action providers.
/// </summary>
public class ActionExecutorService : IActionExecutor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ActionExecutorService> _logger;
    private readonly Dictionary<string, ActionMethodInfo> _actionRegistry;

    public ActionExecutorService(
        IServiceProvider serviceProvider,
        ILogger<ActionExecutorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _actionRegistry = new Dictionary<string, ActionMethodInfo>();

        DiscoverActionProviders();
    }

    /// <inheritdoc />
    public async Task<object> ExecuteAsync(
        string actionType,
        object parameters,
        object? context = null,
        CancellationToken cancellationToken = default)
    {
        if (!_actionRegistry.TryGetValue(actionType, out var actionInfo))
        {
            _logger.LogError("Action type '{ActionType}' not found in registry", actionType);
            throw new InvalidOperationException($"Action type '{actionType}' is not registered");
        }

        try
        {
            _logger.LogDebug("Executing action '{ActionType}'", actionType);

            // Resolve provider from DI
            var provider = _serviceProvider.GetRequiredService(actionInfo.ProviderType);

            // Build method parameters
            var methodParams = new List<object?> { parameters };

            // Add context parameter if method expects it
            if (actionInfo.HasContextParameter)
            {
                methodParams.Add(context);
            }

            // Add cancellation token if method expects it
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

                // Extract result from Task<T>
                var resultProperty = task.GetType().GetProperty("Result");
                if (resultProperty != null)
                {
                    result = resultProperty.GetValue(task);
                }
                else
                {
                    result = null; // Task (non-generic) returns void
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
        return _actionRegistry.ContainsKey(actionType);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetRegisteredActions()
    {
        return _actionRegistry.Keys;
    }

    /// <summary>
    /// Discover action providers in the Actions.Core assembly.
    /// </summary>
    private void DiscoverActionProviders()
    {
        var assembly = typeof(ActionExecutorService).Assembly;

        _logger.LogInformation("Discovering action providers in assembly '{AssemblyName}'", assembly.FullName);

        var providerTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<ActionProviderAttribute>() != null && !t.IsAbstract)
            .ToList();

        _logger.LogInformation("Found {Count} action provider(s)", providerTypes.Count);

        foreach (var providerType in providerTypes)
        {
            DiscoverActionsInProvider(providerType);
        }

        _logger.LogInformation("Action discovery complete. Registered {Count} action(s): {Actions}",
            _actionRegistry.Count,
            string.Join(", ", _actionRegistry.Keys));
    }

    /// <summary>
    /// Discover action methods in a provider type.
    /// </summary>
    private void DiscoverActionsInProvider(Type providerType)
    {
        var methods = providerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<ActionMethodAttribute>() != null)
            .ToList();

        foreach (var method in methods)
        {
            var attribute = method.GetCustomAttribute<ActionMethodAttribute>()!;
            var actionType = attribute.ActionType;

            if (_actionRegistry.ContainsKey(actionType))
            {
                _logger.LogWarning(
                    "Duplicate action type '{ActionType}' found in provider '{Provider}'. Skipping.",
                    actionType,
                    providerType.Name);
                continue;
            }

            var parameters = method.GetParameters();

            // Validate method signature
            if (parameters.Length == 0)
            {
                _logger.LogWarning(
                    "Action method '{Method}' in provider '{Provider}' has no parameters. Skipping.",
                    method.Name,
                    providerType.Name);
                continue;
            }

            var actionInfo = new ActionMethodInfo
            {
                ActionType = actionType,
                ProviderType = providerType,
                Method = method,
                HasContextParameter = parameters.Length > 1 && parameters[1].ParameterType == typeof(object),
                HasCancellationTokenParameter = parameters.Any(p => p.ParameterType == typeof(CancellationToken))
            };

            _actionRegistry[actionType] = actionInfo;

            _logger.LogDebug(
                "Registered action '{ActionType}' from provider '{Provider}.{Method}'",
                actionType,
                providerType.Name,
                method.Name);
        }
    }

    /// <summary>
    /// Information about an action method.
    /// </summary>
    private class ActionMethodInfo
    {
        public required string ActionType { get; init; }
        public required Type ProviderType { get; init; }
        public required MethodInfo Method { get; init; }
        public required bool HasContextParameter { get; init; }
        public required bool HasCancellationTokenParameter { get; init; }
    }
}

using System.Reflection;
using DonkeyWork.Agents.Actions.Contracts.Attributes;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actions.Core.Services;

/// <summary>
/// Information about an action method.
/// </summary>
public class ActionMethodInfo
{
    public required string ActionType { get; init; }
    public required Type ProviderType { get; init; }
    public required MethodInfo Method { get; init; }
    public required Type ParameterType { get; init; }
    public required bool HasContextParameter { get; init; }
    public required bool HasCancellationTokenParameter { get; init; }
}

/// <summary>
/// Registry for discovered action providers. Singleton service.
/// </summary>
public interface IActionRegistry
{
    ActionMethodInfo? GetAction(string actionType);
    bool IsActionRegistered(string actionType);
    IEnumerable<string> GetRegisteredActions();
}

/// <summary>
/// Singleton registry that discovers and caches action providers at startup.
/// </summary>
public class ActionRegistry : IActionRegistry
{
    private readonly Dictionary<string, ActionMethodInfo> _actionRegistry;
    private readonly ILogger<ActionRegistry> _logger;

    public ActionRegistry(ILogger<ActionRegistry> logger)
    {
        _logger = logger;
        _actionRegistry = new Dictionary<string, ActionMethodInfo>();
        DiscoverActionProviders();
    }

    public ActionMethodInfo? GetAction(string actionType)
    {
        return _actionRegistry.TryGetValue(actionType, out var info) ? info : null;
    }

    public bool IsActionRegistered(string actionType)
    {
        return _actionRegistry.ContainsKey(actionType);
    }

    public IEnumerable<string> GetRegisteredActions()
    {
        return _actionRegistry.Keys;
    }

    private void DiscoverActionProviders()
    {
        var assembly = typeof(ActionRegistry).Assembly;

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
                ParameterType = parameters[0].ParameterType,
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
}

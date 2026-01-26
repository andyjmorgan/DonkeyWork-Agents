namespace DonkeyWork.Agents.Actions.Contracts.Services;

/// <summary>
/// Service for executing action providers by action type.
/// </summary>
public interface IActionExecutor
{
    /// <summary>
    /// Execute an action by its type identifier with the provided parameters.
    /// </summary>
    /// <param name="actionType">The action type identifier (e.g., "http_request")</param>
    /// <param name="parameters">The parameters object for the action</param>
    /// <param name="context">Execution context for expression resolution</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the action execution</returns>
    Task<object> ExecuteAsync(
        string actionType,
        object parameters,
        object? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an action type is registered and available.
    /// </summary>
    /// <param name="actionType">The action type to check</param>
    /// <returns>True if the action is registered, false otherwise</returns>
    bool IsActionRegistered(string actionType);

    /// <summary>
    /// Get all registered action types.
    /// </summary>
    /// <returns>Collection of registered action type identifiers</returns>
    IEnumerable<string> GetRegisteredActions();
}

namespace DonkeyWork.Agents.Actions.Contracts.Services;

/// <summary>
/// Service for evaluating template expressions using Scriban
/// </summary>
public interface IExpressionEngine
{
    /// <summary>
    /// Evaluate an expression template with the given context
    /// </summary>
    /// <param name="template">Template string (e.g., "{{Variables.name}}")</param>
    /// <param name="context">Evaluation context containing variables</param>
    /// <returns>Evaluated result as string</returns>
    string Evaluate(string template, object context);

    /// <summary>
    /// Evaluate an expression template and convert to specified type
    /// </summary>
    /// <typeparam name="T">Target type</typeparam>
    /// <param name="template">Template string</param>
    /// <param name="context">Evaluation context</param>
    /// <returns>Evaluated and converted result</returns>
    T Evaluate<T>(string template, object context);
}

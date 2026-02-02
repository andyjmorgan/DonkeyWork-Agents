namespace DonkeyWork.Agents.Agents.Contracts.Services;

/// <summary>
/// Service for rendering Scriban templates with execution context variables.
/// </summary>
public interface ITemplateRenderer
{
    /// <summary>
    /// Renders a Scriban template with the current execution context.
    /// Available variables: Input (execution input), Steps (node outputs by name).
    /// </summary>
    /// <param name="template">The Scriban template string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rendered string.</returns>
    Task<string> RenderAsync(string template, CancellationToken cancellationToken = default);
}

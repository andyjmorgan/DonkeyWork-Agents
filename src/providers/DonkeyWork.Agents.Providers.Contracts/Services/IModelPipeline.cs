using DonkeyWork.Agents.Providers.Contracts.Models.Pipeline;
using DonkeyWork.Agents.Providers.Contracts.Models.Pipeline.Events;

namespace DonkeyWork.Agents.Providers.Contracts.Services;

/// <summary>
/// Interface for executing the model pipeline.
/// </summary>
public interface IModelPipeline
{
    /// <summary>
    /// Executes the model pipeline and streams events.
    /// </summary>
    /// <param name="request">The pipeline request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Stream of pipeline events.</returns>
    IAsyncEnumerable<ModelPipelineEvent> ExecuteAsync(
        ModelPipelineRequest request,
        CancellationToken cancellationToken = default);
}

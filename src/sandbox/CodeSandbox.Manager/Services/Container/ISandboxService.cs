using CodeSandbox.Contracts.Responses;
using CodeSandbox.Manager.Models;

namespace CodeSandbox.Manager.Services.Container;

public interface ISandboxService
{
    IAsyncEnumerable<ContainerCreationEvent> CreateSandboxAsync(CreateSandboxRequest request, CancellationToken cancellationToken = default);
    Task<List<SandboxInfo>> ListContainersAsync(CancellationToken cancellationToken = default);
    Task<SandboxInfo?> GetContainerAsync(string podName, CancellationToken cancellationToken = default);
    Task<DeleteSandboxResponse> DeleteContainerAsync(string podName, CancellationToken cancellationToken = default);
    Task<DeleteAllSandboxesResponse> DeleteAllContainersAsync(CancellationToken cancellationToken = default);

    Task<ToolResponse> ExecuteCommandAsync(string sandboxId, ExecutionRequest request, CancellationToken cancellationToken = default);
    Task<string> GetPodIpAsync(string sandboxId, CancellationToken cancellationToken = default);
    Task UpdateLastActivityAsync(string sandboxId, CancellationToken cancellationToken = default);
    Task<DateTime?> GetLastActivityAsync(string sandboxId, CancellationToken cancellationToken = default);

    Task<SandboxInfo?> FindSandboxAsync(string userId, string conversationId, CancellationToken cancellationToken = default);
}

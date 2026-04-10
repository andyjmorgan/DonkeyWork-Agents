using CodeSandbox.Contracts.Requests.Tools;
using CodeSandbox.Contracts.Responses;

namespace CodeSandbox.Manager.Services.Executor;

public interface IExecutorClient
{
    Task<ToolResponse> BashAsync(BashRequest request, CancellationToken ct = default);

    Task<ToolResponse> ReadAsync(ReadRequest request, CancellationToken ct = default);

    Task<ToolResponse> EditAsync(EditRequest request, CancellationToken ct = default);

    Task<ToolResponse> WriteAsync(WriteRequest request, CancellationToken ct = default);

    Task<ToolResponse> MultiEditAsync(MultiEditRequest request, CancellationToken ct = default);

    Task<ToolResponse> GlobAsync(GlobRequest request, CancellationToken ct = default);

    Task<ToolResponse> GrepAsync(GrepRequest request, CancellationToken ct = default);

    Task<ToolResponse> ResumeAsync(ResumeRequest request, CancellationToken ct = default);
}

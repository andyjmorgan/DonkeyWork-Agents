using DonkeyWork.Agents.Storage.Contracts.Models;

namespace DonkeyWork.Agents.Storage.Contracts.Services;

public interface IFileShareService
{
    Task<Models.FileShare> CreateShareAsync(CreateShareRequest request, CancellationToken cancellationToken = default);

    Task<Models.FileShare?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    Task<FileDownloadResult?> DownloadByTokenAsync(string token, string? password = null, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Models.FileShare> Items, int TotalCount)> ListByFileAsync(Guid fileId, int offset = 0, int limit = 50, CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(Guid shareId, CancellationToken cancellationToken = default);
}

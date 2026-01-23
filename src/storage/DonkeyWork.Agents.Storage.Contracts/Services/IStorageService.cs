using DonkeyWork.Agents.Storage.Contracts.Models;

namespace DonkeyWork.Agents.Storage.Contracts.Services;

public interface IStorageService
{
    Task<StoredFile> UploadAsync(UploadFileRequest request, CancellationToken cancellationToken = default);

    Task<StoredFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FileDownloadResult?> DownloadAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<StoredFile> Items, int TotalCount)> ListAsync(int offset = 0, int limit = 50, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

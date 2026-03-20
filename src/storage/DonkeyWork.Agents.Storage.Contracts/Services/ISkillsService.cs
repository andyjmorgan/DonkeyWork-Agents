using DonkeyWork.Agents.Storage.Contracts.Models;

namespace DonkeyWork.Agents.Storage.Contracts.Services;

public interface ISkillsService
{
    Task<IReadOnlyList<SkillItemV1>> ListAsync(CancellationToken ct = default);
    Task<SkillUploadResultV1> UploadAsync(Stream zipStream, CancellationToken ct = default);
    Task<bool> DeleteAsync(string skillName, CancellationToken ct = default);
    Task<IReadOnlyList<SkillFileNodeV1>?> GetContentsAsync(string skillName, CancellationToken ct = default);
    Task<ReadFileResponseV1?> ReadFileAsync(string skillName, string relativePath, CancellationToken ct = default);
    Task<WriteFileResponseV1?> WriteFileAsync(string skillName, string relativePath, WriteFileRequestV1 request, CancellationToken ct = default);
    Task<bool> DeleteFileAsync(string skillName, string relativePath, CancellationToken ct = default);
    Task<RenameResponseV1?> RenameAsync(string skillName, string relativePath, RenameRequestV1 request, CancellationToken ct = default);
    Task<DuplicateFileResponseV1?> DuplicateFileAsync(string skillName, string relativePath, CancellationToken ct = default);
    Task<CreateFolderResponseV1?> CreateFolderAsync(string skillName, string relativePath, CancellationToken ct = default);
    Task<bool> DeleteFolderAsync(string skillName, string relativePath, CancellationToken ct = default);
    Task<Stream?> DownloadAsync(string skillName, CancellationToken ct = default);
}

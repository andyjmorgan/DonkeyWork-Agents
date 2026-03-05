using DonkeyWork.Agents.Storage.Contracts.Models;

namespace DonkeyWork.Agents.Storage.Contracts.Services;

public interface ISkillsService
{
    Task<IReadOnlyList<SkillItemV1>> ListAsync(CancellationToken ct = default);
    Task<SkillUploadResultV1> UploadAsync(Stream zipStream, CancellationToken ct = default);
    Task<bool> DeleteAsync(string skillName, CancellationToken ct = default);
    Task<IReadOnlyList<SkillFileNodeV1>?> GetContentsAsync(string skillName, CancellationToken ct = default);
}

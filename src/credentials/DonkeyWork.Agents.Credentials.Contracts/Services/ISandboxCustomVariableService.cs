using DonkeyWork.Agents.Credentials.Contracts.Models;

namespace DonkeyWork.Agents.Credentials.Contracts.Services;

public interface ISandboxCustomVariableService
{
    Task<IReadOnlyList<SandboxCustomVariableV1>> ListAsync(CancellationToken ct = default);

    Task<SandboxCustomVariableV1?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<SandboxCustomVariableV1> CreateAsync(CreateSandboxCustomVariableRequestV1 request, CancellationToken ct = default);

    Task<SandboxCustomVariableV1> UpdateAsync(Guid id, UpdateSandboxCustomVariableRequestV1 request, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns all custom variables for the current user as key-value pairs.
    /// Used by sandbox manager to inject environment variables into pods.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetAllAsKeyValuePairsAsync(CancellationToken ct = default);
}

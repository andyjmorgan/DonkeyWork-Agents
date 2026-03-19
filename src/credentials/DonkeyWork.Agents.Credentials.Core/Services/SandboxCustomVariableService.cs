using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Credentials.Core.Services;

public sealed class SandboxCustomVariableService : ISandboxCustomVariableService
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly ILogger<SandboxCustomVariableService> _logger;

    public SandboxCustomVariableService(
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        ILogger<SandboxCustomVariableService> logger)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SandboxCustomVariableV1>> ListAsync(CancellationToken ct = default)
    {
        var entities = await _dbContext.SandboxCustomVariables
            .OrderBy(e => e.Key)
            .ToListAsync(ct);

        return entities.Select(ToModel).ToList();
    }

    public async Task<SandboxCustomVariableV1?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _dbContext.SandboxCustomVariables
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        return entity is null ? null : ToModel(entity);
    }

    public async Task<SandboxCustomVariableV1> CreateAsync(
        CreateSandboxCustomVariableRequestV1 request,
        CancellationToken ct = default)
    {
        // Check for duplicate key
        var existing = await _dbContext.SandboxCustomVariables
            .FirstOrDefaultAsync(e => e.Key == request.Key, ct);

        if (existing is not null)
            throw new InvalidOperationException($"A variable with key '{request.Key}' already exists.");

        var entity = new SandboxCustomVariableEntity
        {
            UserId = _identityContext.UserId,
            Key = request.Key,
            Value = request.Value,
            IsSecret = request.IsSecret,
        };

        _dbContext.SandboxCustomVariables.Add(entity);
        await _dbContext.SaveChangesAsync(ct);

        return ToModel(entity);
    }

    public async Task<SandboxCustomVariableV1> UpdateAsync(
        Guid id,
        UpdateSandboxCustomVariableRequestV1 request,
        CancellationToken ct = default)
    {
        var entity = await _dbContext.SandboxCustomVariables
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new InvalidOperationException("Sandbox custom variable not found.");

        if (request.Value is not null)
            entity.Value = request.Value;

        if (request.IsSecret.HasValue)
            entity.IsSecret = request.IsSecret.Value;

        await _dbContext.SaveChangesAsync(ct);

        return ToModel(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _dbContext.SandboxCustomVariables
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (entity is null)
            return;

        _dbContext.SandboxCustomVariables.Remove(entity);
        await _dbContext.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, string>> GetAllAsKeyValuePairsAsync(CancellationToken ct = default)
    {
        // TODO: This method will be used by the sandbox manager to inject environment variables
        // into sandbox pods at runtime. The sandbox manager integration is a follow-up task.
        var entities = await _dbContext.SandboxCustomVariables
            .ToListAsync(ct);

        return entities.ToDictionary(e => e.Key, e => e.Value);
    }

    private static SandboxCustomVariableV1 ToModel(SandboxCustomVariableEntity entity)
    {
        return new SandboxCustomVariableV1
        {
            Id = entity.Id,
            Key = entity.Key,
            Value = entity.IsSecret ? "********" : entity.Value,
            IsSecret = entity.IsSecret,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };
    }
}

using System.Security.Cryptography;
using System.Text;
using DonkeyWork.Agents.A2a.Contracts.Models;
using DonkeyWork.Agents.A2a.Contracts.Services;
using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.A2a;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DonkeyWork.Agents.A2a.Core.Services;

public class A2aServerConfigurationService : IA2aServerConfigurationService
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly IExternalApiKeyService _externalApiKeyService;
    private readonly IOAuthTokenService _oAuthTokenService;
    private readonly ILogger<A2aServerConfigurationService> _logger;
    private readonly byte[] _encryptionKey;

    public A2aServerConfigurationService(
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        IExternalApiKeyService externalApiKeyService,
        IOAuthTokenService oAuthTokenService,
        IOptions<PersistenceOptions> options,
        ILogger<A2aServerConfigurationService> logger)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
        _externalApiKeyService = externalApiKeyService;
        _oAuthTokenService = oAuthTokenService;
        _logger = logger;
        _encryptionKey = DeriveKey(options.Value.EncryptionKey);
    }

    public async Task<A2aServerDetailsV1> CreateAsync(CreateA2aServerRequestV1 request, CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;
        _logger.LogInformation("Creating A2A server configuration for user {UserId} with name {Name}", userId, request.Name);

        var now = DateTimeOffset.UtcNow;
        var configId = Guid.NewGuid();

        var entity = new A2aServerConfigurationEntity
        {
            Id = configId,
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            Address = request.Address,
            IsEnabled = request.IsEnabled,
            ConnectToNavi = request.ConnectToNavi,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.A2aServerConfigurations.Add(entity);

        if (request.HeaderConfigurations != null)
        {
            foreach (var headerConfig in request.HeaderConfigurations)
            {
                await ValidateCredentialReferenceAsync(headerConfig.CredentialId, headerConfig.CredentialFieldType, $"header '{headerConfig.HeaderName}'", cancellationToken);

                var headerEntity = new A2aServerHeaderConfigurationEntity
                {
                    Id = Guid.NewGuid(),
                    A2aServerConfigurationId = configId,
                    HeaderName = headerConfig.HeaderName,
                    HeaderValueEncrypted = headerConfig.CredentialId.HasValue ? null : Convert.ToBase64String(Encrypt(headerConfig.HeaderValue!)),
                    CredentialId = headerConfig.CredentialId,
                    CredentialFieldType = headerConfig.CredentialFieldType
                };
                _dbContext.A2aServerHeaderConfigurations.Add(headerEntity);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Created A2A server configuration {ConfigId}", configId);

        return (await GetByIdAsync(configId, cancellationToken))!;
    }

    public async Task<A2aServerDetailsV1?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.A2aServerConfigurations
            .AsNoTracking()
            .Include(c => c.HeaderConfigurations)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return entity == null ? null : MapToDetails(entity);
    }

    public async Task<PaginatedResponse<A2aServerSummaryV1>> ListAsync(PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var limit = pagination.GetLimit();

        var query = _dbContext.A2aServerConfigurations.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip(pagination.Offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<A2aServerSummaryV1>
        {
            Items = entities.Select(MapToSummary).ToList(),
            Offset = pagination.Offset,
            Limit = limit,
            TotalCount = totalCount
        };
    }

    public async Task<A2aServerDetailsV1?> UpdateAsync(Guid id, UpdateA2aServerRequestV1 request, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.A2aServerConfigurations
            .Include(c => c.HeaderConfigurations)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (entity == null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.Address = request.Address;
        entity.IsEnabled = request.IsEnabled;
        entity.ConnectToNavi = request.ConnectToNavi;
        entity.UpdatedAt = now;

        var existingHeaders = entity.HeaderConfigurations
            .ToDictionary(h => h.HeaderName, h => (h.HeaderValueEncrypted, h.CredentialId, h.CredentialFieldType), StringComparer.OrdinalIgnoreCase);

        if (entity.HeaderConfigurations.Any())
        {
            _dbContext.A2aServerHeaderConfigurations.RemoveRange(entity.HeaderConfigurations);
        }

        if (request.HeaderConfigurations != null)
        {
            foreach (var headerConfig in request.HeaderConfigurations)
            {
                await ValidateCredentialReferenceAsync(headerConfig.CredentialId, headerConfig.CredentialFieldType, $"header '{headerConfig.HeaderName}'", cancellationToken);

                string? encryptedValue = null;
                Guid? credentialId = headerConfig.CredentialId;
                string? credentialFieldType = headerConfig.CredentialFieldType;

                if (credentialId.HasValue)
                {
                    encryptedValue = null;
                }
                else if (!string.IsNullOrEmpty(headerConfig.HeaderValue))
                {
                    encryptedValue = Convert.ToBase64String(Encrypt(headerConfig.HeaderValue));
                }
                else if (existingHeaders.TryGetValue(headerConfig.HeaderName, out var existing))
                {
                    encryptedValue = existing.HeaderValueEncrypted;
                    credentialId = existing.CredentialId;
                    credentialFieldType = existing.CredentialFieldType;
                }

                var headerEntity = new A2aServerHeaderConfigurationEntity
                {
                    Id = Guid.NewGuid(),
                    A2aServerConfigurationId = id,
                    HeaderName = headerConfig.HeaderName,
                    HeaderValueEncrypted = encryptedValue,
                    CredentialId = credentialId,
                    CredentialFieldType = credentialFieldType
                };
                _dbContext.A2aServerHeaderConfigurations.Add(headerEntity);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated A2A server configuration {ConfigId}", id);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.A2aServerConfigurations
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (entity == null)
        {
            return false;
        }

        _dbContext.A2aServerConfigurations.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Deleted A2A server configuration {ConfigId}", id);

        return true;
    }

    public async Task<IReadOnlyList<A2aConnectionConfigV1>> GetEnabledConnectionConfigsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.A2aServerConfigurations
            .AsNoTracking()
            .Where(c => c.IsEnabled)
            .Include(c => c.HeaderConfigurations)
            .ToListAsync(cancellationToken);

        return await BuildConnectionConfigsAsync(entities, cancellationToken);
    }

    public async Task<IReadOnlyList<A2aConnectionConfigV1>> GetNaviConnectionConfigsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.A2aServerConfigurations
            .AsNoTracking()
            .Where(c => c.IsEnabled && c.ConnectToNavi)
            .Include(c => c.HeaderConfigurations)
            .ToListAsync(cancellationToken);

        return await BuildConnectionConfigsAsync(entities, cancellationToken);
    }

    public async Task<A2aConnectionConfigV1?> GetConnectionConfigByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.A2aServerConfigurations
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Include(c => c.HeaderConfigurations)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity == null)
        {
            return null;
        }

        var headers = new Dictionary<string, string>();

        foreach (var header in entity.HeaderConfigurations)
        {
            try
            {
                var resolvedValue = await ResolveHeaderValueAsync(header, entity.Id, entity.Name, cancellationToken);
                if (resolvedValue != null)
                {
                    headers[header.HeaderName] = resolvedValue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve header {HeaderName} for A2A server {Id} ({Name})", header.HeaderName, entity.Id, entity.Name);
            }
        }

        return new A2aConnectionConfigV1
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Address = entity.Address,
            Headers = headers,
        };
    }

    #region Private Helpers

    private async Task<IReadOnlyList<A2aConnectionConfigV1>> BuildConnectionConfigsAsync(
        List<A2aServerConfigurationEntity> entities,
        CancellationToken cancellationToken)
    {
        var configs = new List<A2aConnectionConfigV1>();

        foreach (var entity in entities)
        {
            var headers = new Dictionary<string, string>();

            foreach (var header in entity.HeaderConfigurations)
            {
                try
                {
                    var resolvedValue = await ResolveHeaderValueAsync(header, entity.Id, entity.Name, cancellationToken);
                    if (resolvedValue != null)
                    {
                        headers[header.HeaderName] = resolvedValue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to resolve header {HeaderName} for A2A server {Id} ({Name})", header.HeaderName, entity.Id, entity.Name);
                }
            }

            configs.Add(new A2aConnectionConfigV1
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                Address = entity.Address,
                Headers = headers,
            });
        }

        return configs;
    }

    #endregion

    #region Mapping

    private A2aServerSummaryV1 MapToSummary(A2aServerConfigurationEntity entity)
    {
        return new A2aServerSummaryV1
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Address = entity.Address,
            IsEnabled = entity.IsEnabled,
            ConnectToNavi = entity.ConnectToNavi,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private A2aServerDetailsV1 MapToDetails(A2aServerConfigurationEntity entity)
    {
        return new A2aServerDetailsV1
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Address = entity.Address,
            IsEnabled = entity.IsEnabled,
            ConnectToNavi = entity.ConnectToNavi,
            HeaderConfigurations = entity.HeaderConfigurations.Select(h => new A2aHeaderConfigurationV1
            {
                Id = h.Id,
                HeaderName = h.HeaderName,
                HeaderValue = h.CredentialId.HasValue ? null : (h.HeaderValueEncrypted != null ? Decrypt(Convert.FromBase64String(h.HeaderValueEncrypted)) : null),
                IsCredentialReference = h.CredentialId.HasValue,
                CredentialId = h.CredentialId,
                CredentialFieldType = h.CredentialFieldType
            }).ToList(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    #endregion

    #region Credential Resolution

    private async Task ValidateCredentialReferenceAsync(Guid? credentialId, string? credentialFieldType, string context, CancellationToken cancellationToken)
    {
        if (!credentialId.HasValue)
        {
            return;
        }

        if (string.IsNullOrEmpty(credentialFieldType))
        {
            throw new ArgumentException($"Credential field type is required when referencing a credential for {context}.");
        }

        if (!Enum.TryParse<CredentialFieldType>(credentialFieldType, ignoreCase: true, out _))
        {
            throw new ArgumentException($"Invalid credential field type '{credentialFieldType}' for {context}. Valid types: {string.Join(", ", Enum.GetNames<CredentialFieldType>())}");
        }

        var credential = await _externalApiKeyService.GetByIdAsync(_identityContext.UserId, credentialId.Value, cancellationToken);
        if (credential == null)
        {
            throw new ArgumentException($"Credential '{credentialId.Value}' not found for {context}.");
        }
    }

    private async Task<string?> ResolveCredentialValueAsync(Guid credentialId, string credentialFieldType, CancellationToken cancellationToken)
    {
        var credential = await _externalApiKeyService.GetByIdAsync(_identityContext.UserId, credentialId, cancellationToken);
        if (credential == null)
        {
            _logger.LogWarning("Credential {CredentialId} not found during resolution", credentialId);
            return null;
        }

        if (Enum.TryParse<CredentialFieldType>(credentialFieldType, ignoreCase: true, out var fieldType)
            && credential.Fields.TryGetValue(fieldType, out var value))
        {
            return value;
        }

        _logger.LogWarning("Credential {CredentialId} does not have field {FieldType}", credentialId, credentialFieldType);
        return null;
    }

    private async Task<string?> ResolveHeaderValueAsync(A2aServerHeaderConfigurationEntity header, Guid serverId, string serverName, CancellationToken cancellationToken)
    {
        if (header.CredentialId.HasValue && header.CredentialFieldType != null)
        {
            return await ResolveCredentialValueAsync(header.CredentialId.Value, header.CredentialFieldType, cancellationToken);
        }

        if (header.HeaderValueEncrypted != null)
        {
            return Decrypt(Convert.FromBase64String(header.HeaderValueEncrypted));
        }

        return null;
    }

    #endregion

    #region Encryption

    private byte[] Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[aes.IV.Length + cipherBytes.Length];
        aes.IV.CopyTo(result, 0);
        cipherBytes.CopyTo(result, aes.IV.Length);
        return result;
    }

    private string Decrypt(byte[] encryptedData)
    {
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;

        var ivLength = aes.BlockSize / 8;
        var iv = encryptedData[..ivLength];
        var cipherBytes = encryptedData[ivLength..];

        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private static byte[] DeriveKey(string password)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    #endregion
}

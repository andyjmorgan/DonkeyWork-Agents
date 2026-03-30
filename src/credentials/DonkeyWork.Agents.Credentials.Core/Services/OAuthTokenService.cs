using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DonkeyWork.Agents.Credentials.Core.Services;

/// <summary>
/// Service for managing OAuth tokens.
/// </summary>
public sealed class OAuthTokenService : IOAuthTokenService
{
    private readonly AgentsDbContext _dbContext;
    private readonly byte[] _encryptionKey;

    public OAuthTokenService(
        AgentsDbContext dbContext,
        IOptions<PersistenceOptions> options)
    {
        _dbContext = dbContext;
        _encryptionKey = DeriveKey(options.Value.EncryptionKey);
    }

    public async Task<OAuthToken?> GetByIdAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.OAuthTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Id == id, cancellationToken);

        return entity == null ? null : ToModel(entity);
    }

    public async Task<IReadOnlyList<OAuthToken>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.OAuthTokens
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(ToModel).ToList();
    }

    public async Task<OAuthToken?> GetByProviderAsync(
        Guid userId,
        OAuthProvider provider,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.OAuthTokens
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Provider == provider, cancellationToken);

        return entity == null ? null : ToModel(entity);
    }

    public async Task<OAuthToken> StoreTokenAsync(
        Guid userId,
        OAuthProvider provider,
        string externalUserId,
        string email,
        string accessToken,
        string refreshToken,
        IEnumerable<string> scopes,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken = default)
    {
        var scopesList = scopes.ToList();

        var existing = await _dbContext.OAuthTokens
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Provider == provider, cancellationToken);

        if (existing != null)
        {
            existing.ExternalUserId = externalUserId;
            existing.Email = email;
            existing.AccessTokenEncrypted = Encrypt(accessToken);
            existing.RefreshTokenEncrypted = Encrypt(refreshToken);
            existing.ScopesJson = JsonSerializer.Serialize(scopesList);
            existing.ExpiresAt = expiresAt;
            existing.LastRefreshedAt = DateTimeOffset.UtcNow;
            existing.UpdatedAt = DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return ToModel(existing);
        }

        var entity = new OAuthTokenEntity
        {
            UserId = userId,
            Provider = provider,
            ExternalUserId = externalUserId,
            Email = email,
            AccessTokenEncrypted = Encrypt(accessToken),
            RefreshTokenEncrypted = Encrypt(refreshToken),
            ScopesJson = JsonSerializer.Serialize(scopesList),
            ExpiresAt = expiresAt,
            LastRefreshedAt = DateTimeOffset.UtcNow
        };

        _dbContext.OAuthTokens.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToModel(entity);
    }

    public async Task<OAuthToken> RefreshTokenAsync(
        Guid id,
        string newAccessToken,
        string newRefreshToken,
        DateTimeOffset? newExpiresAt,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.OAuthTokens
            .IgnoreQueryFilters() // Need to update across all users
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity == null)
        {
            throw new InvalidOperationException($"OAuth token {id} not found");
        }

        entity.AccessTokenEncrypted = Encrypt(newAccessToken);
        entity.RefreshTokenEncrypted = Encrypt(newRefreshToken);
        entity.ExpiresAt = newExpiresAt;
        entity.LastRefreshedAt = DateTimeOffset.UtcNow;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToModel(entity);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.OAuthTokens
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Id == id, cancellationToken);

        if (entity == null)
        {
            throw new InvalidOperationException($"OAuth token {id} not found");
        }

        _dbContext.OAuthTokens.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OAuthToken>> GetExpiringTokensAsync(
        TimeSpan expirationWindow,
        CancellationToken cancellationToken = default)
    {
        var expirationThreshold = DateTimeOffset.UtcNow.Add(expirationWindow);

        var entities = await _dbContext.OAuthTokens
            .IgnoreQueryFilters() // Get tokens for all users
            .Where(e => e.ExpiresAt.HasValue && e.ExpiresAt.Value <= expirationThreshold)
            .ToListAsync(cancellationToken);

        return entities.Select(ToModel).ToList();
    }

    private OAuthToken ToModel(OAuthTokenEntity entity)
    {
        var scopesList = JsonSerializer.Deserialize<List<string>>(entity.ScopesJson) ?? new List<string>();

        return new OAuthToken
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Provider = entity.Provider,
            ExternalUserId = entity.ExternalUserId,
            Email = entity.Email,
            AccessToken = Decrypt(entity.AccessTokenEncrypted),
            RefreshToken = Decrypt(entity.RefreshTokenEncrypted),
            Scopes = scopesList,
            ExpiresAt = entity.ExpiresAt,
            CreatedAt = entity.CreatedAt,
            LastRefreshedAt = entity.LastRefreshedAt
        };
    }

    private byte[] Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to cipher text
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        aes.IV.CopyTo(result, 0);
        cipherBytes.CopyTo(result, aes.IV.Length);
        return result;
    }

    private string Decrypt(byte[] cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;

        // Extract IV from beginning of cipher text
        var iv = new byte[16];
        var cipher = new byte[cipherText.Length - 16];
        Array.Copy(cipherText, 0, iv, 0, 16);
        Array.Copy(cipherText, 16, cipher, 0, cipher.Length);

        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private static byte[] DeriveKey(string password)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
    }
}

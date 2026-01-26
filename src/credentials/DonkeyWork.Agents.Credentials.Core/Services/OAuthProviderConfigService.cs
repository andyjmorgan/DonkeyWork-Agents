using System.Security.Cryptography;
using System.Text;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DonkeyWork.Agents.Credentials.Core.Services;

/// <summary>
/// Service for managing OAuth provider configurations.
/// </summary>
public sealed class OAuthProviderConfigService : IOAuthProviderConfigService
{
    private readonly AgentsDbContext _dbContext;
    private readonly byte[] _encryptionKey;

    public OAuthProviderConfigService(
        AgentsDbContext dbContext,
        IOptions<PersistenceOptions> options)
    {
        _dbContext = dbContext;
        _encryptionKey = DeriveKey(options.Value.EncryptionKey);
    }

    public async Task<OAuthProviderConfig?> GetByIdAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.OAuthProviderConfigs
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Id == id, cancellationToken);

        return entity == null ? null : ToModel(entity);
    }

    public async Task<OAuthProviderConfig?> GetByProviderAsync(
        Guid userId,
        OAuthProvider provider,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.OAuthProviderConfigs
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Provider == provider, cancellationToken);

        return entity == null ? null : ToModel(entity);
    }

    public async Task<IReadOnlyList<OAuthProviderConfig>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.OAuthProviderConfigs
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.Provider)
            .ToListAsync(cancellationToken);

        return entities.Select(ToModel).ToList();
    }

    public async Task<OAuthProviderConfig> CreateAsync(
        Guid userId,
        OAuthProvider provider,
        string clientId,
        string clientSecret,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        // Check for existing config for this provider
        var existing = await _dbContext.OAuthProviderConfigs
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Provider == provider, cancellationToken);

        if (existing != null)
        {
            throw new InvalidOperationException(
                $"OAuth provider config already exists for {provider}. Use Update instead.");
        }

        var entity = new OAuthProviderConfigEntity
        {
            UserId = userId,
            Provider = provider,
            ClientIdEncrypted = Encrypt(clientId),
            ClientSecretEncrypted = Encrypt(clientSecret),
            RedirectUri = redirectUri
        };

        _dbContext.OAuthProviderConfigs.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToModel(entity);
    }

    public async Task<OAuthProviderConfig> UpdateAsync(
        Guid userId,
        Guid id,
        string? clientId,
        string? clientSecret,
        string? redirectUri,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.OAuthProviderConfigs
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Id == id, cancellationToken);

        if (entity == null)
        {
            throw new InvalidOperationException($"OAuth provider config {id} not found");
        }

        if (!string.IsNullOrEmpty(clientId))
        {
            entity.ClientIdEncrypted = Encrypt(clientId);
        }

        if (!string.IsNullOrEmpty(clientSecret))
        {
            entity.ClientSecretEncrypted = Encrypt(clientSecret);
        }

        if (!string.IsNullOrEmpty(redirectUri))
        {
            entity.RedirectUri = redirectUri;
        }

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToModel(entity);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.OAuthProviderConfigs
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Id == id, cancellationToken);

        if (entity == null)
        {
            throw new InvalidOperationException($"OAuth provider config {id} not found");
        }

        _dbContext.OAuthProviderConfigs.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private OAuthProviderConfig ToModel(OAuthProviderConfigEntity entity)
    {
        return new OAuthProviderConfig
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Provider = entity.Provider,
            ClientId = Decrypt(entity.ClientIdEncrypted),
            ClientSecret = Decrypt(entity.ClientSecretEncrypted),
            RedirectUri = entity.RedirectUri,
            CreatedAt = entity.CreatedAt
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

using System.Security.Cryptography;
using System.Text;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DonkeyWork.Agents.Credentials.Core.Services;

public sealed class UserApiKeyService : IUserApiKeyService
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly byte[] _encryptionKey;

    private const string KeyPrefix = "dk_";

    public UserApiKeyService(
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        IOptions<PersistenceOptions> options)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
        _encryptionKey = DeriveKey(options.Value.EncryptionKey);
    }

    public async Task<(IReadOnlyList<UserApiKey> Items, int TotalCount)> ListAsync(int offset = 0, int limit = 50, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.UserApiKeys.OrderByDescending(e => e.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var items = entities.Select(e => ToModel(e, masked: true)).ToList();
        return (items, totalCount);
    }

    public async Task<UserApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.UserApiKeys
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        return entity == null ? null : ToModel(entity, masked: false);
    }

    public async Task<UserApiKey> CreateAsync(string name, string? description = null, CancellationToken cancellationToken = default)
    {
        var apiKey = GenerateApiKey();
        var encryptedKey = Encrypt(apiKey);

        var entity = new UserApiKeyEntity
        {
            UserId = _identityContext.UserId,
            Name = name,
            Description = description ?? string.Empty,
            EncryptedKey = encryptedKey
        };

        _dbContext.UserApiKeys.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UserApiKey
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Name = entity.Name,
            Description = entity.Description,
            Key = apiKey,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.UserApiKeys
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity == null)
            return false;

        _dbContext.UserApiKeys.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<Guid?> ValidateAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (!apiKey.StartsWith(KeyPrefix))
            return null;

        // Need to check all keys since we can't query by encrypted value
        // This is intentional - we iterate to find the matching key
        var entities = await _dbContext.UserApiKeys
            .IgnoreQueryFilters() // Need to check all users' keys
            .ToListAsync(cancellationToken);

        foreach (var entity in entities)
        {
            var decryptedKey = Decrypt(entity.EncryptedKey);
            if (decryptedKey == apiKey)
                return entity.UserId;
        }

        return null;
    }

    private UserApiKey ToModel(UserApiKeyEntity entity, bool masked)
    {
        var key = Decrypt(entity.EncryptedKey);

        return new UserApiKey
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Name = entity.Name,
            Description = string.IsNullOrEmpty(entity.Description) ? null : entity.Description,
            Key = masked ? MaskKey(key) : key,
            CreatedAt = entity.CreatedAt
        };
    }

    private static string GenerateApiKey()
    {
        // Generate extra bytes to ensure we have enough characters after filtering
        var bytes = new byte[40];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        var base64 = Convert.ToBase64String(bytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");
        return $"{KeyPrefix}{base64[..40]}";
    }

    private static string MaskKey(string key)
    {
        if (key.Length <= 10)
            return key;

        var prefix = key[..7]; // dk_abc
        var suffix = key[^3..]; // xyz
        return $"{prefix}***{suffix}";
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

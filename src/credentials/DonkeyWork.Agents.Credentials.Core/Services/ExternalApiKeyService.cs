using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DonkeyWork.Agents.Credentials.Core.Services;

public sealed class ExternalApiKeyService : IExternalApiKeyService
{
    private readonly AgentsDbContext _dbContext;
    private readonly byte[] _encryptionKey;

    private static readonly ExternalApiKeyProvider[] LlmProviders =
    [
        ExternalApiKeyProvider.OpenAI,
        ExternalApiKeyProvider.Anthropic,
        ExternalApiKeyProvider.Google
    ];

    public ExternalApiKeyService(
        AgentsDbContext dbContext,
        IOptions<PersistenceOptions> options)
    {
        _dbContext = dbContext;
        _encryptionKey = DeriveKey(options.Value.EncryptionKey);
    }

    public async Task<ExternalApiKey?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ExternalApiKeys
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, cancellationToken);

        return entity == null ? null : ToModel(entity, masked: false);
    }

    public async Task<IReadOnlyList<ExternalApiKey>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.ExternalApiKeys
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(e => ToModel(e, masked: true)).ToList();
    }

    public async Task<IReadOnlyList<ExternalApiKey>> GetByProviderAsync(
        Guid userId,
        ExternalApiKeyProvider provider,
        CancellationToken cancellationToken = default)
    {
        var providerName = provider.ToString();
        var entities = await _dbContext.ExternalApiKeys
            .Where(e => e.UserId == userId && e.Provider == providerName)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(e => ToModel(e, masked: true)).ToList();
    }

    public async Task<ExternalApiKey> CreateAsync(
        Guid userId,
        ExternalApiKeyProvider provider,
        string name,
        IDictionary<CredentialFieldType, string> fields,
        CancellationToken cancellationToken = default)
    {
        var fieldsJson = SerializeFields(fields);
        var encryptedFields = Encrypt(fieldsJson);

        var entity = new ExternalApiKeyEntity
        {
            UserId = userId,
            Provider = provider.ToString(),
            Name = name,
            FieldsEncrypted = Convert.ToBase64String(encryptedFields)
        };

        _dbContext.ExternalApiKeys.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToModel(entity, masked: false, originalFields: fields.ToDictionary(k => k.Key, v => v.Value));
    }

    public async Task<ExternalApiKey> UpdateAsync(
        Guid userId,
        Guid id,
        string? name,
        IDictionary<CredentialFieldType, string>? fields,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ExternalApiKeys
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("External API key not found");

        if (name != null)
            entity.Name = name;

        if (fields != null)
        {
            var fieldsJson = SerializeFields(fields);
            var encryptedFields = Encrypt(fieldsJson);
            entity.FieldsEncrypted = Convert.ToBase64String(encryptedFields);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToModel(entity, masked: false);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ExternalApiKeys
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, cancellationToken);

        if (entity == null)
            return;

        _dbContext.ExternalApiKeys.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> GetApiKeyValueAsync(
        Guid userId,
        ExternalApiKeyProvider provider,
        CancellationToken cancellationToken = default)
    {
        var providerName = provider.ToString();
        var entity = await _dbContext.ExternalApiKeys
            .FirstOrDefaultAsync(e => e.UserId == userId && e.Provider == providerName, cancellationToken);

        if (entity == null)
            return null;

        var fields = DecryptFields(entity.FieldsEncrypted);
        return fields.GetValueOrDefault(CredentialFieldType.ApiKey);
    }

    public async Task<IReadOnlyList<ExternalApiKeyProvider>> GetConfiguredLlmProvidersAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var llmProviderNames = LlmProviders.Select(p => p.ToString()).ToList();

        var configuredProviders = await _dbContext.ExternalApiKeys
            .Where(e => e.UserId == userId && llmProviderNames.Contains(e.Provider))
            .Select(e => e.Provider)
            .Distinct()
            .ToListAsync(cancellationToken);

        return configuredProviders
            .Select(p => Enum.Parse<ExternalApiKeyProvider>(p))
            .ToList();
    }

    private ExternalApiKey ToModel(
        ExternalApiKeyEntity entity,
        bool masked,
        IReadOnlyDictionary<CredentialFieldType, string>? originalFields = null)
    {
        var fields = originalFields ?? DecryptFields(entity.FieldsEncrypted);

        if (masked)
        {
            fields = fields.ToDictionary(
                k => k.Key,
                v => MaskValue(v.Value));
        }

        return new ExternalApiKey
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Provider = Enum.Parse<ExternalApiKeyProvider>(entity.Provider),
            Name = entity.Name,
            Fields = fields,
            CreatedAt = entity.CreatedAt,
            LastUsedAt = entity.LastUsedAt
        };
    }

    private IReadOnlyDictionary<CredentialFieldType, string> DecryptFields(string encryptedBase64)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedBase64);
        var json = Decrypt(encryptedBytes);
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? new Dictionary<string, string>();

        return dict.ToDictionary(
            k => Enum.Parse<CredentialFieldType>(k.Key),
            v => v.Value);
    }

    private static string SerializeFields(IDictionary<CredentialFieldType, string> fields)
    {
        var dict = fields.ToDictionary(k => k.Key.ToString(), v => v.Value);
        return JsonSerializer.Serialize(dict);
    }

    private static string MaskValue(string value)
    {
        if (value.Length <= 8)
            return "***";

        var prefix = value[..4];
        var suffix = value[^4..];
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

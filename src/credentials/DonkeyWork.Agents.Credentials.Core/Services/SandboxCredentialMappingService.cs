using System.Text;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Credentials.Core.Templates;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Credentials;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Credentials.Core.Services;

public sealed class SandboxCredentialMappingService : ISandboxCredentialMappingService
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly IExternalApiKeyService _externalApiKeyService;
    private readonly IOAuthTokenService _oAuthTokenService;
    private readonly ILogger<SandboxCredentialMappingService> _logger;

    public SandboxCredentialMappingService(
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        IExternalApiKeyService externalApiKeyService,
        IOAuthTokenService oAuthTokenService,
        ILogger<SandboxCredentialMappingService> logger)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
        _externalApiKeyService = externalApiKeyService;
        _oAuthTokenService = oAuthTokenService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SandboxCredentialMappingV1>> ListAsync(CancellationToken ct = default)
    {
        var entities = await _dbContext.SandboxCredentialMappings
            .OrderBy(e => e.BaseDomain)
            .ThenBy(e => e.HeaderName)
            .ToListAsync(ct);

        return entities.Select(ToModel).ToList();
    }

    public async Task<SandboxCredentialMappingV1?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _dbContext.SandboxCredentialMappings
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        return entity is null ? null : ToModel(entity);
    }

    public async Task<SandboxCredentialMappingV1> CreateAsync(
        CreateSandboxCredentialMappingRequestV1 request,
        CancellationToken ct = default)
    {
        var entity = new SandboxCredentialMappingEntity
        {
            UserId = _identityContext.UserId,
            BaseDomain = request.BaseDomain,
            HeaderName = request.HeaderName,
            HeaderValuePrefix = request.HeaderValuePrefix,
            HeaderValueFormat = request.HeaderValueFormat.ToString(),
            BasicAuthUsername = request.BasicAuthUsername,
            CredentialId = request.CredentialId,
            CredentialType = request.CredentialType,
            CredentialFieldType = request.CredentialFieldType.ToString(),
        };

        _dbContext.SandboxCredentialMappings.Add(entity);
        await _dbContext.SaveChangesAsync(ct);

        return ToModel(entity);
    }

    public async Task<SandboxCredentialMappingV1> UpdateAsync(
        Guid id,
        UpdateSandboxCredentialMappingRequestV1 request,
        CancellationToken ct = default)
    {
        var entity = await _dbContext.SandboxCredentialMappings
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new InvalidOperationException("Sandbox credential mapping not found");

        if (request.HeaderName is not null)
            entity.HeaderName = request.HeaderName;

        if (request.HeaderValuePrefix is not null)
            entity.HeaderValuePrefix = request.HeaderValuePrefix;

        if (request.CredentialId.HasValue)
            entity.CredentialId = request.CredentialId.Value;

        if (request.CredentialType is not null)
            entity.CredentialType = request.CredentialType;

        if (request.CredentialFieldType.HasValue)
            entity.CredentialFieldType = request.CredentialFieldType.Value.ToString();

        if (request.HeaderValueFormat.HasValue)
            entity.HeaderValueFormat = request.HeaderValueFormat.Value.ToString();

        if (request.BasicAuthUsername is not null)
            entity.BasicAuthUsername = request.BasicAuthUsername;

        await _dbContext.SaveChangesAsync(ct);

        return ToModel(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _dbContext.SandboxCredentialMappings
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (entity is null)
            return;

        _dbContext.SandboxCredentialMappings.Remove(entity);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<ResolvedDomainCredentialV1?> ResolveForDomainAsync(
        string baseDomain,
        CancellationToken ct = default)
    {
        var mappings = await _dbContext.SandboxCredentialMappings
            .Where(e => e.BaseDomain == baseDomain)
            .ToListAsync(ct);

        if (mappings.Count == 0)
            return null;

        var headers = new Dictionary<string, string>();
        var userId = _identityContext.UserId;

        foreach (var mapping in mappings)
        {
            var credentialValue = await ResolveCredentialValueAsync(
                userId, mapping.CredentialId, mapping.CredentialType,
                Enum.Parse<CredentialFieldType>(mapping.CredentialFieldType), ct);

            if (credentialValue is null)
            {
                _logger.LogWarning(
                    "Could not resolve credential {CredentialId} ({CredentialType}) for domain {Domain}",
                    mapping.CredentialId, mapping.CredentialType, baseDomain);
                continue;
            }

            var format = Enum.TryParse<HeaderValueFormat>(mapping.HeaderValueFormat, out var f)
                ? f
                : HeaderValueFormat.Raw;

            var headerValue = format switch
            {
                HeaderValueFormat.BasicAuth => $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{mapping.BasicAuthUsername}:{credentialValue}"))}",
                _ => string.IsNullOrEmpty(mapping.HeaderValuePrefix)
                    ? credentialValue
                    : $"{mapping.HeaderValuePrefix}{credentialValue}",
            };

            headers[mapping.HeaderName] = headerValue;
        }

        if (headers.Count == 0)
            return null;

        return new ResolvedDomainCredentialV1
        {
            BaseDomain = baseDomain,
            Headers = headers,
        };
    }

    public async Task<IReadOnlyList<string>> GetConfiguredDomainsAsync(CancellationToken ct = default)
    {
        return await _dbContext.SandboxCredentialMappings
            .Select(e => e.BaseDomain)
            .Distinct()
            .ToListAsync(ct);
    }

    private async Task<string?> ResolveCredentialValueAsync(
        Guid userId,
        Guid credentialId,
        string credentialType,
        CredentialFieldType fieldType,
        CancellationToken ct)
    {
        switch (credentialType)
        {
            case "ExternalApiKey":
                var apiKey = await _externalApiKeyService.GetByIdAsync(userId, credentialId, ct);
                return apiKey?.Fields.GetValueOrDefault(fieldType);

            case "OAuthToken":
                var token = await _oAuthTokenService.GetByIdAsync(userId, credentialId, ct);
                if (token is null) return null;
                return fieldType switch
                {
                    CredentialFieldType.AccessToken => token.AccessToken,
                    CredentialFieldType.RefreshToken => token.RefreshToken,
                    _ => null,
                };

            default:
                _logger.LogWarning("Unknown credential type: {CredentialType}", credentialType);
                return null;
        }
    }

    public async Task<IReadOnlyList<SandboxProviderStatusV1>> ListProviderStatusesAsync(CancellationToken ct = default)
    {
        var templates = SandboxProviderTemplateRegistry.GetAll();
        var userId = _identityContext.UserId;
        var existingDomains = await _dbContext.SandboxCredentialMappings
            .Select(e => e.BaseDomain)
            .ToListAsync(ct);

        var statuses = new List<SandboxProviderStatusV1>();

        foreach (var template in templates)
        {
            var token = await _oAuthTokenService.GetByProviderAsync(userId, template.Provider, ct);
            var templateDomains = template.Mappings.Select(m => m.BaseDomain).ToList();
            var isEnabled = templateDomains.All(d => existingDomains.Contains(d));

            statuses.Add(new SandboxProviderStatusV1
            {
                Provider = template.Provider,
                DisplayName = template.DisplayName,
                HasOAuthToken = token is not null,
                IsEnabled = isEnabled,
                Domains = templateDomains,
            });
        }

        return statuses;
    }

    public async Task<IReadOnlyList<SandboxCredentialMappingV1>> CreateFromProviderAsync(
        OAuthProvider provider,
        CancellationToken ct = default)
    {
        var template = SandboxProviderTemplateRegistry.GetTemplate(provider)
            ?? throw new InvalidOperationException($"No template found for provider {provider}");

        var userId = _identityContext.UserId;
        var token = await _oAuthTokenService.GetByProviderAsync(userId, provider, ct)
            ?? throw new InvalidOperationException($"No OAuth token found for provider {provider}");

        var results = new List<SandboxCredentialMappingV1>();

        foreach (var mapping in template.Mappings)
        {
            // Upsert: check if mapping already exists for this domain + header
            var existing = await _dbContext.SandboxCredentialMappings
                .FirstOrDefaultAsync(e => e.BaseDomain == mapping.BaseDomain && e.HeaderName == mapping.HeaderName, ct);

            if (existing is not null)
            {
                existing.HeaderValuePrefix = mapping.HeaderValuePrefix;
                existing.HeaderValueFormat = mapping.HeaderValueFormat.ToString();
                existing.BasicAuthUsername = mapping.BasicAuthUsername;
                existing.CredentialId = token.Id;
                existing.CredentialType = "OAuthToken";
                existing.CredentialFieldType = mapping.CredentialFieldType.ToString();
            }
            else
            {
                existing = new SandboxCredentialMappingEntity
                {
                    UserId = userId,
                    BaseDomain = mapping.BaseDomain,
                    HeaderName = mapping.HeaderName,
                    HeaderValuePrefix = mapping.HeaderValuePrefix,
                    HeaderValueFormat = mapping.HeaderValueFormat.ToString(),
                    BasicAuthUsername = mapping.BasicAuthUsername,
                    CredentialId = token.Id,
                    CredentialType = "OAuthToken",
                    CredentialFieldType = mapping.CredentialFieldType.ToString(),
                };
                _dbContext.SandboxCredentialMappings.Add(existing);
            }

            await _dbContext.SaveChangesAsync(ct);
            results.Add(ToModel(existing));
        }

        return results;
    }

    public async Task DeleteByProviderAsync(OAuthProvider provider, CancellationToken ct = default)
    {
        var template = SandboxProviderTemplateRegistry.GetTemplate(provider)
            ?? throw new InvalidOperationException($"No template found for provider {provider}");

        var templateDomains = template.Mappings.Select(m => m.BaseDomain).ToHashSet();

        var mappings = await _dbContext.SandboxCredentialMappings
            .Where(e => templateDomains.Contains(e.BaseDomain))
            .ToListAsync(ct);

        _dbContext.SandboxCredentialMappings.RemoveRange(mappings);
        await _dbContext.SaveChangesAsync(ct);
    }

    private static SandboxCredentialMappingV1 ToModel(SandboxCredentialMappingEntity entity)
    {
        return new SandboxCredentialMappingV1
        {
            Id = entity.Id,
            BaseDomain = entity.BaseDomain,
            HeaderName = entity.HeaderName,
            HeaderValuePrefix = entity.HeaderValuePrefix,
            HeaderValueFormat = Enum.Parse<HeaderValueFormat>(entity.HeaderValueFormat),
            BasicAuthUsername = entity.BasicAuthUsername,
            CredentialId = entity.CredentialId,
            CredentialType = entity.CredentialType,
            CredentialFieldType = Enum.Parse<CredentialFieldType>(entity.CredentialFieldType),
            CreatedAt = entity.CreatedAt,
        };
    }
}

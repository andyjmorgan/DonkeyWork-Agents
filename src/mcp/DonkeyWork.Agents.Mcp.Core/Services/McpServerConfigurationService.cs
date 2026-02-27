using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Mcp.Contracts.Models;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Mcp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DonkeyWork.Agents.Mcp.Core.Services;

public class McpServerConfigurationService : IMcpServerConfigurationService
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly ILogger<McpServerConfigurationService> _logger;
    private readonly byte[] _encryptionKey;

    public McpServerConfigurationService(
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        IOptions<PersistenceOptions> options,
        ILogger<McpServerConfigurationService> logger)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
        _logger = logger;
        _encryptionKey = DeriveKey(options.Value.EncryptionKey);
    }

    public async Task<McpServerDetailsV1> CreateAsync(CreateMcpServerRequestV1 request, CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;
        _logger.LogInformation("Creating MCP server configuration for user {UserId} with name {Name}", userId, request.Name);

        var now = DateTimeOffset.UtcNow;
        var configId = Guid.NewGuid();

        var entity = new McpServerConfigurationEntity
        {
            Id = configId,
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            TransportType = request.TransportType,
            IsEnabled = request.IsEnabled,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.McpServerConfigurations.Add(entity);

        // Add transport-specific configuration
        if (request.TransportType == McpTransportType.Stdio && request.StdioConfiguration != null)
        {
            var stdioConfig = new McpStdioConfigurationEntity
            {
                Id = Guid.NewGuid(),
                McpServerConfigurationId = configId,
                Command = request.StdioConfiguration.Command,
                Arguments = JsonSerializer.Serialize(request.StdioConfiguration.Arguments ?? []),
                EnvironmentVariables = JsonSerializer.Serialize(request.StdioConfiguration.EnvironmentVariables ?? new Dictionary<string, string>()),
                PreExecScripts = JsonSerializer.Serialize(request.StdioConfiguration.PreExecScripts ?? []),
                WorkingDirectory = request.StdioConfiguration.WorkingDirectory
            };
            _dbContext.McpStdioConfigurations.Add(stdioConfig);
        }
        else if (request.TransportType == McpTransportType.Http && request.HttpConfiguration != null)
        {
            var httpConfigId = Guid.NewGuid();
            var httpConfig = new McpHttpConfigurationEntity
            {
                Id = httpConfigId,
                McpServerConfigurationId = configId,
                Endpoint = request.HttpConfiguration.Endpoint,
                TransportMode = request.HttpConfiguration.TransportMode,
                AuthType = request.HttpConfiguration.AuthType
            };
            _dbContext.McpHttpConfigurations.Add(httpConfig);

            // Add OAuth configuration if applicable
            if (request.HttpConfiguration.AuthType == McpHttpAuthType.OAuth && request.HttpConfiguration.OAuthConfiguration != null)
            {
                var oauthConfig = new McpHttpOAuthConfigurationEntity
                {
                    Id = Guid.NewGuid(),
                    McpHttpConfigurationId = httpConfigId,
                    ClientId = request.HttpConfiguration.OAuthConfiguration.ClientId,
                    ClientSecretEncrypted = Convert.ToBase64String(Encrypt(request.HttpConfiguration.OAuthConfiguration.ClientSecret)),
                    RedirectUri = request.HttpConfiguration.OAuthConfiguration.RedirectUri,
                    Scopes = JsonSerializer.Serialize(request.HttpConfiguration.OAuthConfiguration.Scopes ?? []),
                    AuthorizationEndpoint = request.HttpConfiguration.OAuthConfiguration.AuthorizationEndpoint,
                    TokenEndpoint = request.HttpConfiguration.OAuthConfiguration.TokenEndpoint
                };
                _dbContext.McpHttpOAuthConfigurations.Add(oauthConfig);
            }

            // Add header configurations if applicable
            if (request.HttpConfiguration.AuthType == McpHttpAuthType.Header && request.HttpConfiguration.HeaderConfigurations != null)
            {
                foreach (var headerConfig in request.HttpConfiguration.HeaderConfigurations)
                {
                    var headerEntity = new McpHttpHeaderConfigurationEntity
                    {
                        Id = Guid.NewGuid(),
                        McpHttpConfigurationId = httpConfigId,
                        HeaderName = headerConfig.HeaderName,
                        HeaderValueEncrypted = Convert.ToBase64String(Encrypt(headerConfig.HeaderValue))
                    };
                    _dbContext.McpHttpHeaderConfigurations.Add(headerEntity);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Created MCP server configuration {ConfigId}", configId);

        return (await GetByIdAsync(configId, cancellationToken))!;
    }

    public async Task<McpServerDetailsV1?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.McpServerConfigurations
            .AsNoTracking()
            .Include(c => c.StdioConfiguration)
            .Include(c => c.HttpConfiguration)
                .ThenInclude(h => h!.OAuthConfiguration)
            .Include(c => c.HttpConfiguration)
                .ThenInclude(h => h!.HeaderConfigurations)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return entity == null ? null : MapToDetails(entity);
    }

    public async Task<IReadOnlyList<McpServerSummaryV1>> ListAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.McpServerConfigurations
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToSummary).ToList();
    }

    public async Task<McpServerDetailsV1?> UpdateAsync(Guid id, UpdateMcpServerRequestV1 request, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.McpServerConfigurations
            .Include(c => c.StdioConfiguration)
            .Include(c => c.HttpConfiguration)
                .ThenInclude(h => h!.OAuthConfiguration)
            .Include(c => c.HttpConfiguration)
                .ThenInclude(h => h!.HeaderConfigurations)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (entity == null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.TransportType = request.TransportType;
        entity.IsEnabled = request.IsEnabled;
        entity.UpdatedAt = now;

        // Remove old transport configurations
        if (entity.StdioConfiguration != null)
        {
            _dbContext.McpStdioConfigurations.Remove(entity.StdioConfiguration);
        }

        if (entity.HttpConfiguration != null)
        {
            if (entity.HttpConfiguration.OAuthConfiguration != null)
            {
                _dbContext.McpHttpOAuthConfigurations.Remove(entity.HttpConfiguration.OAuthConfiguration);
            }
            if (entity.HttpConfiguration.HeaderConfigurations.Any())
            {
                _dbContext.McpHttpHeaderConfigurations.RemoveRange(entity.HttpConfiguration.HeaderConfigurations);
            }
            _dbContext.McpHttpConfigurations.Remove(entity.HttpConfiguration);
        }

        // Add new transport configuration
        if (request.TransportType == McpTransportType.Stdio && request.StdioConfiguration != null)
        {
            var stdioConfig = new McpStdioConfigurationEntity
            {
                Id = Guid.NewGuid(),
                McpServerConfigurationId = id,
                Command = request.StdioConfiguration.Command,
                Arguments = JsonSerializer.Serialize(request.StdioConfiguration.Arguments ?? []),
                EnvironmentVariables = JsonSerializer.Serialize(request.StdioConfiguration.EnvironmentVariables ?? new Dictionary<string, string>()),
                PreExecScripts = JsonSerializer.Serialize(request.StdioConfiguration.PreExecScripts ?? []),
                WorkingDirectory = request.StdioConfiguration.WorkingDirectory
            };
            _dbContext.McpStdioConfigurations.Add(stdioConfig);
        }
        else if (request.TransportType == McpTransportType.Http && request.HttpConfiguration != null)
        {
            var httpConfigId = Guid.NewGuid();
            var httpConfig = new McpHttpConfigurationEntity
            {
                Id = httpConfigId,
                McpServerConfigurationId = id,
                Endpoint = request.HttpConfiguration.Endpoint,
                TransportMode = request.HttpConfiguration.TransportMode,
                AuthType = request.HttpConfiguration.AuthType
            };
            _dbContext.McpHttpConfigurations.Add(httpConfig);

            if (request.HttpConfiguration.AuthType == McpHttpAuthType.OAuth && request.HttpConfiguration.OAuthConfiguration != null)
            {
                var oauthConfig = new McpHttpOAuthConfigurationEntity
                {
                    Id = Guid.NewGuid(),
                    McpHttpConfigurationId = httpConfigId,
                    ClientId = request.HttpConfiguration.OAuthConfiguration.ClientId,
                    ClientSecretEncrypted = Convert.ToBase64String(Encrypt(request.HttpConfiguration.OAuthConfiguration.ClientSecret)),
                    RedirectUri = request.HttpConfiguration.OAuthConfiguration.RedirectUri,
                    Scopes = JsonSerializer.Serialize(request.HttpConfiguration.OAuthConfiguration.Scopes ?? []),
                    AuthorizationEndpoint = request.HttpConfiguration.OAuthConfiguration.AuthorizationEndpoint,
                    TokenEndpoint = request.HttpConfiguration.OAuthConfiguration.TokenEndpoint
                };
                _dbContext.McpHttpOAuthConfigurations.Add(oauthConfig);
            }

            if (request.HttpConfiguration.AuthType == McpHttpAuthType.Header && request.HttpConfiguration.HeaderConfigurations != null)
            {
                foreach (var headerConfig in request.HttpConfiguration.HeaderConfigurations)
                {
                    var headerEntity = new McpHttpHeaderConfigurationEntity
                    {
                        Id = Guid.NewGuid(),
                        McpHttpConfigurationId = httpConfigId,
                        HeaderName = headerConfig.HeaderName,
                        HeaderValueEncrypted = Convert.ToBase64String(Encrypt(headerConfig.HeaderValue))
                    };
                    _dbContext.McpHttpHeaderConfigurations.Add(headerEntity);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated MCP server configuration {ConfigId}", id);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.McpServerConfigurations
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (entity == null)
        {
            return false;
        }

        _dbContext.McpServerConfigurations.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Deleted MCP server configuration {ConfigId}", id);

        return true;
    }

    private McpServerSummaryV1 MapToSummary(McpServerConfigurationEntity entity)
    {
        return new McpServerSummaryV1
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            TransportType = entity.TransportType,
            IsEnabled = entity.IsEnabled,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private McpServerDetailsV1 MapToDetails(McpServerConfigurationEntity entity)
    {
        return new McpServerDetailsV1
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            TransportType = entity.TransportType,
            IsEnabled = entity.IsEnabled,
            StdioConfiguration = entity.StdioConfiguration == null ? null : MapStdioConfiguration(entity.StdioConfiguration),
            HttpConfiguration = entity.HttpConfiguration == null ? null : MapHttpConfiguration(entity.HttpConfiguration),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private McpStdioConfigurationV1 MapStdioConfiguration(McpStdioConfigurationEntity entity)
    {
        return new McpStdioConfigurationV1
        {
            Command = entity.Command,
            Arguments = JsonSerializer.Deserialize<List<string>>(entity.Arguments) ?? [],
            EnvironmentVariables = JsonSerializer.Deserialize<Dictionary<string, string>>(entity.EnvironmentVariables) ?? new(),
            PreExecScripts = JsonSerializer.Deserialize<List<string>>(entity.PreExecScripts) ?? [],
            WorkingDirectory = entity.WorkingDirectory
        };
    }

    private McpHttpConfigurationV1 MapHttpConfiguration(McpHttpConfigurationEntity entity)
    {
        return new McpHttpConfigurationV1
        {
            Endpoint = entity.Endpoint,
            TransportMode = entity.TransportMode,
            AuthType = entity.AuthType,
            OAuthConfiguration = entity.OAuthConfiguration == null ? null : MapOAuthConfiguration(entity.OAuthConfiguration),
            HeaderConfigurations = entity.HeaderConfigurations.Select(h => new McpHttpHeaderConfigurationV1
            {
                HeaderName = h.HeaderName
            }).ToList()
        };
    }

    private McpHttpOAuthConfigurationV1 MapOAuthConfiguration(McpHttpOAuthConfigurationEntity entity)
    {
        return new McpHttpOAuthConfigurationV1
        {
            ClientId = entity.ClientId,
            RedirectUri = entity.RedirectUri,
            Scopes = JsonSerializer.Deserialize<List<string>>(entity.Scopes) ?? [],
            AuthorizationEndpoint = entity.AuthorizationEndpoint,
            TokenEndpoint = entity.TokenEndpoint
        };
    }

    public async Task<IReadOnlyList<McpConnectionConfigV1>> GetEnabledConnectionConfigsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.McpServerConfigurations
            .AsNoTracking()
            .Where(c => c.IsEnabled && c.TransportType == McpTransportType.Http)
            .Include(c => c.HttpConfiguration)
                .ThenInclude(h => h!.HeaderConfigurations)
            .ToListAsync(cancellationToken);

        var configs = new List<McpConnectionConfigV1>();

        foreach (var entity in entities)
        {
            if (entity.HttpConfiguration is null)
            {
                _logger.LogWarning("Enabled HTTP MCP server {Id} ({Name}) has no HTTP configuration, skipping", entity.Id, entity.Name);
                continue;
            }

            var headers = new Dictionary<string, string>();

            if (entity.HttpConfiguration.AuthType == McpHttpAuthType.Header)
            {
                foreach (var header in entity.HttpConfiguration.HeaderConfigurations)
                {
                    try
                    {
                        var decryptedValue = Decrypt(Convert.FromBase64String(header.HeaderValueEncrypted));
                        headers[header.HeaderName] = decryptedValue;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to decrypt header {HeaderName} for MCP server {Id} ({Name})", header.HeaderName, entity.Id, entity.Name);
                    }
                }
            }

            configs.Add(new McpConnectionConfigV1
            {
                Id = entity.Id,
                Name = entity.Name,
                Endpoint = entity.HttpConfiguration.Endpoint,
                TransportMode = entity.HttpConfiguration.TransportMode,
                Headers = headers,
            });
        }

        return configs;
    }

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

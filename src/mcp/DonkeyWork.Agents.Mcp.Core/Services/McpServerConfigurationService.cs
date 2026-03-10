using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Services;
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
    private readonly IExternalApiKeyService _externalApiKeyService;
    private readonly IOAuthTokenService _oAuthTokenService;
    private readonly ILogger<McpServerConfigurationService> _logger;
    private readonly byte[] _encryptionKey;

    public McpServerConfigurationService(
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        IExternalApiKeyService externalApiKeyService,
        IOAuthTokenService oAuthTokenService,
        IOptions<PersistenceOptions> options,
        ILogger<McpServerConfigurationService> logger)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
        _externalApiKeyService = externalApiKeyService;
        _oAuthTokenService = oAuthTokenService;
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
            ConnectToNavi = request.ConnectToNavi,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.McpServerConfigurations.Add(entity);

        // Add transport-specific configuration
        if (request.TransportType == McpTransportType.Stdio && request.StdioConfiguration != null)
        {
            var stdioConfigId = Guid.NewGuid();
            var stdioConfig = new McpStdioConfigurationEntity
            {
                Id = stdioConfigId,
                McpServerConfigurationId = configId,
                Command = request.StdioConfiguration.Command,
                Arguments = JsonSerializer.Serialize(request.StdioConfiguration.Arguments ?? []),
                PreExecScripts = JsonSerializer.Serialize(request.StdioConfiguration.PreExecScripts ?? []),
                WorkingDirectory = request.StdioConfiguration.WorkingDirectory
            };
            _dbContext.McpStdioConfigurations.Add(stdioConfig);

            // Add environment variable entities
            if (request.StdioConfiguration.EnvironmentVariables != null)
            {
                foreach (var envVar in request.StdioConfiguration.EnvironmentVariables)
                {
                    await ValidateCredentialReferenceAsync(envVar.CredentialId, envVar.CredentialFieldType, $"environment variable '{envVar.Name}'", cancellationToken);

                    _dbContext.McpStdioEnvironmentVariables.Add(new McpStdioEnvironmentVariableEntity
                    {
                        Id = Guid.NewGuid(),
                        McpStdioConfigurationId = stdioConfigId,
                        Name = envVar.Name,
                        Value = envVar.CredentialId.HasValue ? null : envVar.Value,
                        CredentialId = envVar.CredentialId,
                        CredentialFieldType = envVar.CredentialFieldType
                    });
                }
            }
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
                AuthType = request.HttpConfiguration.AuthType,
                OAuthTokenId = request.HttpConfiguration.OAuthTokenId
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
                    await ValidateCredentialReferenceAsync(headerConfig.CredentialId, headerConfig.CredentialFieldType, $"header '{headerConfig.HeaderName}'", cancellationToken);

                    var headerEntity = new McpHttpHeaderConfigurationEntity
                    {
                        Id = Guid.NewGuid(),
                        McpHttpConfigurationId = httpConfigId,
                        HeaderName = headerConfig.HeaderName,
                        HeaderValueEncrypted = headerConfig.CredentialId.HasValue ? null : Convert.ToBase64String(Encrypt(headerConfig.HeaderValue!)),
                        CredentialId = headerConfig.CredentialId,
                        CredentialFieldType = headerConfig.CredentialFieldType
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
                .ThenInclude(s => s!.EnvironmentVariableConfigurations)
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
                .ThenInclude(s => s!.EnvironmentVariableConfigurations)
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
        entity.ConnectToNavi = request.ConnectToNavi;
        entity.UpdatedAt = now;

        // Capture existing header values (encrypted or credential refs) so we can preserve them if the update doesn't provide new values
        var existingHeaders = entity.HttpConfiguration?.HeaderConfigurations
            .ToDictionary(h => h.HeaderName, h => (h.HeaderValueEncrypted, h.CredentialId, h.CredentialFieldType), StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, (string? HeaderValueEncrypted, Guid? CredentialId, string? CredentialFieldType)>(StringComparer.OrdinalIgnoreCase);

        // Remove old transport configurations
        if (entity.StdioConfiguration != null)
        {
            if (entity.StdioConfiguration.EnvironmentVariableConfigurations.Any())
            {
                _dbContext.McpStdioEnvironmentVariables.RemoveRange(entity.StdioConfiguration.EnvironmentVariableConfigurations);
            }
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
            var stdioConfigId = Guid.NewGuid();
            var stdioConfig = new McpStdioConfigurationEntity
            {
                Id = stdioConfigId,
                McpServerConfigurationId = id,
                Command = request.StdioConfiguration.Command,
                Arguments = JsonSerializer.Serialize(request.StdioConfiguration.Arguments ?? []),
                PreExecScripts = JsonSerializer.Serialize(request.StdioConfiguration.PreExecScripts ?? []),
                WorkingDirectory = request.StdioConfiguration.WorkingDirectory
            };
            _dbContext.McpStdioConfigurations.Add(stdioConfig);

            if (request.StdioConfiguration.EnvironmentVariables != null)
            {
                foreach (var envVar in request.StdioConfiguration.EnvironmentVariables)
                {
                    await ValidateCredentialReferenceAsync(envVar.CredentialId, envVar.CredentialFieldType, $"environment variable '{envVar.Name}'", cancellationToken);

                    _dbContext.McpStdioEnvironmentVariables.Add(new McpStdioEnvironmentVariableEntity
                    {
                        Id = Guid.NewGuid(),
                        McpStdioConfigurationId = stdioConfigId,
                        Name = envVar.Name,
                        Value = envVar.CredentialId.HasValue ? null : envVar.Value,
                        CredentialId = envVar.CredentialId,
                        CredentialFieldType = envVar.CredentialFieldType
                    });
                }
            }
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
                AuthType = request.HttpConfiguration.AuthType,
                OAuthTokenId = request.HttpConfiguration.OAuthTokenId
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
                    await ValidateCredentialReferenceAsync(headerConfig.CredentialId, headerConfig.CredentialFieldType, $"header '{headerConfig.HeaderName}'", cancellationToken);

                    string? encryptedValue = null;
                    Guid? credentialId = headerConfig.CredentialId;
                    string? credentialFieldType = headerConfig.CredentialFieldType;

                    if (credentialId.HasValue)
                    {
                        // Credential reference — no encrypted value
                        encryptedValue = null;
                    }
                    else if (!string.IsNullOrEmpty(headerConfig.HeaderValue))
                    {
                        // New literal value provided
                        encryptedValue = Convert.ToBase64String(Encrypt(headerConfig.HeaderValue));
                    }
                    else if (existingHeaders.TryGetValue(headerConfig.HeaderName, out var existing))
                    {
                        // Preserve existing value (encrypted or credential ref)
                        encryptedValue = existing.HeaderValueEncrypted;
                        credentialId = existing.CredentialId;
                        credentialFieldType = existing.CredentialFieldType;
                    }

                    var headerEntity = new McpHttpHeaderConfigurationEntity
                    {
                        Id = Guid.NewGuid(),
                        McpHttpConfigurationId = httpConfigId,
                        HeaderName = headerConfig.HeaderName,
                        HeaderValueEncrypted = encryptedValue,
                        CredentialId = credentialId,
                        CredentialFieldType = credentialFieldType
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

            if (entity.HttpConfiguration.AuthType == McpHttpAuthType.OAuth
                && entity.HttpConfiguration.OAuthTokenId.HasValue)
            {
                try
                {
                    var token = await _oAuthTokenService.GetByIdAsync(
                        _identityContext.UserId,
                        entity.HttpConfiguration.OAuthTokenId.Value,
                        cancellationToken);
                    if (token != null)
                    {
                        headers["Authorization"] = $"Bearer {token.AccessToken}";
                    }
                    else
                    {
                        _logger.LogWarning("OAuth token {TokenId} not found for MCP server {Id} ({Name})",
                            entity.HttpConfiguration.OAuthTokenId.Value, entity.Id, entity.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to resolve OAuth token for MCP server {Id} ({Name})", entity.Id, entity.Name);
                }
            }
            else if (entity.HttpConfiguration.AuthType == McpHttpAuthType.Header)
            {
                foreach (var header in entity.HttpConfiguration.HeaderConfigurations)
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
                        _logger.LogError(ex, "Failed to resolve header {HeaderName} for MCP server {Id} ({Name})", header.HeaderName, entity.Id, entity.Name);
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

    public async Task<IReadOnlyList<McpStdioConnectionConfigV1>> GetEnabledStdioConfigsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.McpServerConfigurations
            .AsNoTracking()
            .Where(c => c.IsEnabled && c.TransportType == McpTransportType.Stdio)
            .Include(c => c.StdioConfiguration)
                .ThenInclude(s => s!.EnvironmentVariableConfigurations)
            .ToListAsync(cancellationToken);

        var configs = new List<McpStdioConnectionConfigV1>();

        foreach (var entity in entities)
        {
            if (entity.StdioConfiguration is null)
            {
                _logger.LogWarning("Enabled stdio MCP server {Id} ({Name}) has no stdio configuration, skipping", entity.Id, entity.Name);
                continue;
            }

            var envVars = new Dictionary<string, string>();
            foreach (var envVar in entity.StdioConfiguration.EnvironmentVariableConfigurations)
            {
                try
                {
                    var resolvedValue = await ResolveEnvVarValueAsync(envVar, entity.Id, entity.Name, cancellationToken);
                    if (resolvedValue != null)
                    {
                        envVars[envVar.Name] = resolvedValue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to resolve environment variable {EnvVarName} for MCP server {Id} ({Name})", envVar.Name, entity.Id, entity.Name);
                }
            }

            configs.Add(new McpStdioConnectionConfigV1
            {
                Id = entity.Id,
                Name = entity.Name,
                Command = entity.StdioConfiguration.Command,
                Arguments = JsonSerializer.Deserialize<List<string>>(entity.StdioConfiguration.Arguments) ?? [],
                EnvironmentVariables = envVars,
                PreExecScripts = JsonSerializer.Deserialize<List<string>>(entity.StdioConfiguration.PreExecScripts) ?? [],
                WorkingDirectory = entity.StdioConfiguration.WorkingDirectory,
            });
        }

        return configs;
    }

    public async Task<IReadOnlyList<McpConnectionConfigV1>> GetNaviConnectionConfigsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.McpServerConfigurations
            .AsNoTracking()
            .Where(c => c.IsEnabled && c.ConnectToNavi && c.TransportType == McpTransportType.Http)
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

            if (entity.HttpConfiguration.AuthType == McpHttpAuthType.OAuth
                && entity.HttpConfiguration.OAuthTokenId.HasValue)
            {
                try
                {
                    var token = await _oAuthTokenService.GetByIdAsync(
                        _identityContext.UserId,
                        entity.HttpConfiguration.OAuthTokenId.Value,
                        cancellationToken);
                    if (token != null)
                    {
                        headers["Authorization"] = $"Bearer {token.AccessToken}";
                    }
                    else
                    {
                        _logger.LogWarning("OAuth token {TokenId} not found for MCP server {Id} ({Name})",
                            entity.HttpConfiguration.OAuthTokenId.Value, entity.Id, entity.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to resolve OAuth token for MCP server {Id} ({Name})", entity.Id, entity.Name);
                }
            }
            else if (entity.HttpConfiguration.AuthType == McpHttpAuthType.Header)
            {
                foreach (var header in entity.HttpConfiguration.HeaderConfigurations)
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
                        _logger.LogError(ex, "Failed to resolve header {HeaderName} for MCP server {Id} ({Name})", header.HeaderName, entity.Id, entity.Name);
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

    public async Task<IReadOnlyList<McpStdioConnectionConfigV1>> GetNaviStdioConfigsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.McpServerConfigurations
            .AsNoTracking()
            .Where(c => c.IsEnabled && c.ConnectToNavi && c.TransportType == McpTransportType.Stdio)
            .Include(c => c.StdioConfiguration)
                .ThenInclude(s => s!.EnvironmentVariableConfigurations)
            .ToListAsync(cancellationToken);

        var configs = new List<McpStdioConnectionConfigV1>();

        foreach (var entity in entities)
        {
            if (entity.StdioConfiguration is null)
            {
                _logger.LogWarning("Enabled stdio MCP server {Id} ({Name}) has no stdio configuration, skipping", entity.Id, entity.Name);
                continue;
            }

            var envVars = new Dictionary<string, string>();
            foreach (var envVar in entity.StdioConfiguration.EnvironmentVariableConfigurations)
            {
                try
                {
                    var resolvedValue = await ResolveEnvVarValueAsync(envVar, entity.Id, entity.Name, cancellationToken);
                    if (resolvedValue != null)
                    {
                        envVars[envVar.Name] = resolvedValue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to resolve environment variable {EnvVarName} for MCP server {Id} ({Name})", envVar.Name, entity.Id, entity.Name);
                }
            }

            configs.Add(new McpStdioConnectionConfigV1
            {
                Id = entity.Id,
                Name = entity.Name,
                Command = entity.StdioConfiguration.Command,
                Arguments = JsonSerializer.Deserialize<List<string>>(entity.StdioConfiguration.Arguments) ?? [],
                EnvironmentVariables = envVars,
                PreExecScripts = JsonSerializer.Deserialize<List<string>>(entity.StdioConfiguration.PreExecScripts) ?? [],
                WorkingDirectory = entity.StdioConfiguration.WorkingDirectory,
            });
        }

        return configs;
    }

    #region Mapping

    private McpServerSummaryV1 MapToSummary(McpServerConfigurationEntity entity)
    {
        return new McpServerSummaryV1
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            TransportType = entity.TransportType,
            IsEnabled = entity.IsEnabled,
            ConnectToNavi = entity.ConnectToNavi,
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
            ConnectToNavi = entity.ConnectToNavi,
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
            EnvironmentVariables = entity.EnvironmentVariableConfigurations.Select(ev => new McpEnvironmentVariableV1
            {
                Name = ev.Name,
                IsCredentialReference = ev.CredentialId.HasValue,
                Value = ev.CredentialId.HasValue ? null : ev.Value,
                CredentialId = ev.CredentialId,
                CredentialFieldType = ev.CredentialFieldType
            }).ToList(),
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
            OAuthTokenId = entity.OAuthTokenId,
            OAuthConfiguration = entity.OAuthConfiguration == null ? null : MapOAuthConfiguration(entity.OAuthConfiguration),
            HeaderConfigurations = entity.HeaderConfigurations.Select(h => new McpHttpHeaderConfigurationV1
            {
                HeaderName = h.HeaderName,
                HeaderValue = h.CredentialId.HasValue ? null : (h.HeaderValueEncrypted != null ? Decrypt(Convert.FromBase64String(h.HeaderValueEncrypted)) : null),
                IsCredentialReference = h.CredentialId.HasValue,
                CredentialId = h.CredentialId,
                CredentialFieldType = h.CredentialFieldType
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

    private async Task<string?> ResolveEnvVarValueAsync(McpStdioEnvironmentVariableEntity envVar, Guid serverId, string serverName, CancellationToken cancellationToken)
    {
        if (envVar.CredentialId.HasValue && envVar.CredentialFieldType != null)
        {
            return await ResolveCredentialValueAsync(envVar.CredentialId.Value, envVar.CredentialFieldType, cancellationToken);
        }

        return envVar.Value;
    }

    private async Task<string?> ResolveHeaderValueAsync(McpHttpHeaderConfigurationEntity header, Guid serverId, string serverName, CancellationToken cancellationToken)
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

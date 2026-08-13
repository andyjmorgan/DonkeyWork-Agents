using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Providers;
using DonkeyWork.Agents.Providers.Contracts.Enums;
using DonkeyWork.Agents.Providers.Contracts.Models;
using DonkeyWork.Agents.Providers.Contracts.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace DonkeyWork.Agents.Providers.Core.Services;

public sealed class CustomModelService : ICustomModelService
{
    public const string CatalogPrefix = "custom:";
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly IDataProtector _protector;
    private readonly IHttpClientFactory _httpClientFactory;

    public CustomModelService(
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        IDataProtectionProvider dataProtectionProvider,
        IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
        _protector = dataProtectionProvider.CreateProtector("DonkeyWork.Agents.CustomModels.ApiKey.v1");
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IReadOnlyList<CustomModelV1>> GetAllAsync(CancellationToken cancellationToken = default) =>
        (await _dbContext.CustomModels.OrderBy(e => e.Name).ToListAsync(cancellationToken)).Select(ToModel).ToList();

    public async Task<CustomModelV1?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.CustomModels.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    public async Task<ResolvedCustomModel?> ResolveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.CustomModels.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity is null ? null : ToResolved(entity);
    }

    public async Task<CustomModelV1> CreateAsync(CreateCustomModelRequestV1 request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.Endpoint, request.WireFormat, request.ModelName, request.MaxInputTokens, request.MaxOutputTokens);
        var entity = new CustomModelEntity
        {
            UserId = _identityContext.UserId,
            Name = request.Name.Trim(),
            Endpoint = request.Endpoint.Trim(),
            WireFormat = request.WireFormat.ToString(),
            ModelName = request.ModelName.Trim(),
            ApiKeyEncrypted = Protect(request.ApiKey),
            MaxInputTokens = request.MaxInputTokens,
            MaxOutputTokens = request.MaxOutputTokens,
            SupportsTools = request.SupportsTools
        };
        _dbContext.CustomModels.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToModel(entity);
    }

    public async Task<CustomModelV1?> UpdateAsync(Guid id, UpdateCustomModelRequestV1 request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.Endpoint, request.WireFormat, request.ModelName, request.MaxInputTokens, request.MaxOutputTokens);
        var entity = await _dbContext.CustomModels.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null) return null;

        entity.Name = request.Name.Trim();
        entity.Endpoint = request.Endpoint.Trim();
        entity.WireFormat = request.WireFormat.ToString();
        entity.ModelName = request.ModelName.Trim();
        entity.MaxInputTokens = request.MaxInputTokens;
        entity.MaxOutputTokens = request.MaxOutputTokens;
        entity.SupportsTools = request.SupportsTools;
        if (request.ClearApiKey) entity.ApiKeyEncrypted = null;
        else if (!string.IsNullOrWhiteSpace(request.ApiKey)) entity.ApiKeyEncrypted = Protect(request.ApiKey);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToModel(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.CustomModels.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null) return false;
        _dbContext.CustomModels.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TestCustomModelResponseV1> TestAsync(TestCustomModelRequestV1 request, CancellationToken cancellationToken = default)
    {
        ResolvedCustomModel? saved = request.Id.HasValue
            ? await ResolveAsync(request.Id.Value, cancellationToken)
            : null;
        var endpoint = request.Endpoint?.Trim() ?? saved?.Endpoint;
        var modelName = request.ModelName?.Trim() ?? saved?.ModelName;
        var wireFormat = request.WireFormat ?? saved?.WireFormat;
        var apiKey = request.ClearApiKey
            ? null
            : !string.IsNullOrWhiteSpace(request.ApiKey) ? request.ApiKey : saved?.ApiKey;
        if (wireFormat is null) throw new ArgumentException("Wire format is required.");
        Validate("Test model", endpoint ?? "", wireFormat.Value, modelName ?? "", 1, 1);

        var body = wireFormat == CustomModelWireFormat.AnthropicMessages
            ? new { model = modelName, max_tokens = 8, messages = new[] { new { role = "user", content = "Reply with OK." } } }
            : (object)new { model = modelName, max_output_tokens = 16, input = "Reply with OK." };

        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        ApplyAuthentication(message, wireFormat.Value, apiKey);
        if (wireFormat == CustomModelWireFormat.AnthropicMessages)
            message.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var response = await _httpClientFactory.CreateClient(nameof(CustomModelService))
                .SendAsync(message, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            stopwatch.Stop();
            if (!response.IsSuccessStatusCode)
            {
                var detail = responseBody.Length > 500 ? responseBody[..500] : responseBody;
                return new TestCustomModelResponseV1
                {
                    Success = false,
                    Message = $"{(int)response.StatusCode} {response.ReasonPhrase}: {detail}",
                    DurationMs = stopwatch.ElapsedMilliseconds
                };
            }

            return new TestCustomModelResponseV1
            {
                Success = true,
                Message = "The model endpoint responded successfully.",
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            stopwatch.Stop();
            return new TestCustomModelResponseV1
            {
                Success = false,
                Message = ex.Message,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    internal static void ApplyAuthentication(HttpRequestMessage message, CustomModelWireFormat wireFormat, string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return;
        if (wireFormat == CustomModelWireFormat.AnthropicMessages)
            message.Headers.TryAddWithoutValidation("x-api-key", apiKey);
        else
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    private string? Protect(string? value) => string.IsNullOrWhiteSpace(value) ? null : _protector.Protect(value.Trim());
    private string? Unprotect(string? value) => string.IsNullOrEmpty(value) ? null : _protector.Unprotect(value);

    private static void Validate(
        string name,
        string endpoint,
        CustomModelWireFormat wireFormat,
        string modelName,
        int maxInputTokens,
        int maxOutputTokens)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Display name is required.");
        if (!Enum.IsDefined(wireFormat)) throw new ArgumentException("Wire format is invalid.");
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new ArgumentException("Endpoint must be an absolute HTTP or HTTPS URL.");
        if (string.IsNullOrWhiteSpace(modelName)) throw new ArgumentException("Model name is required.");
        if (maxInputTokens <= 0 || maxOutputTokens <= 0) throw new ArgumentException("Token limits must be greater than zero.");
    }

    private static CustomModelV1 ToModel(CustomModelEntity entity) => new()
    {
        Id = entity.Id,
        CatalogId = $"{CatalogPrefix}{entity.Id:D}",
        Name = entity.Name,
        Endpoint = entity.Endpoint,
        WireFormat = Enum.Parse<CustomModelWireFormat>(entity.WireFormat),
        ModelName = entity.ModelName,
        HasApiKey = !string.IsNullOrEmpty(entity.ApiKeyEncrypted),
        MaxInputTokens = entity.MaxInputTokens,
        MaxOutputTokens = entity.MaxOutputTokens,
        SupportsTools = entity.SupportsTools,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    private ResolvedCustomModel ToResolved(CustomModelEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Endpoint = entity.Endpoint,
        WireFormat = Enum.Parse<CustomModelWireFormat>(entity.WireFormat),
        ModelName = entity.ModelName,
        ApiKey = Unprotect(entity.ApiKeyEncrypted),
        MaxInputTokens = entity.MaxInputTokens,
        MaxOutputTokens = entity.MaxOutputTokens,
        SupportsTools = entity.SupportsTools
    };
}

using DonkeyWork.Agents.Providers.Contracts.Enums;

namespace DonkeyWork.Agents.Providers.Contracts.Models;

public sealed class CustomModelV1
{
    public required Guid Id { get; init; }
    public required string CatalogId { get; init; }
    public required string Name { get; init; }
    public required string Endpoint { get; init; }
    public required CustomModelWireFormat WireFormat { get; init; }
    public required string ModelName { get; init; }
    public bool HasApiKey { get; init; }
    public int MaxInputTokens { get; init; }
    public int MaxOutputTokens { get; init; }
    public bool SupportsTools { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class CreateCustomModelRequestV1
{
    public required string Name { get; init; }
    public required string Endpoint { get; init; }
    public required CustomModelWireFormat WireFormat { get; init; }
    public required string ModelName { get; init; }
    public string? ApiKey { get; init; }
    public int MaxInputTokens { get; init; } = 131_072;
    public int MaxOutputTokens { get; init; } = 16_384;
    public bool SupportsTools { get; init; } = true;
}

public sealed class UpdateCustomModelRequestV1
{
    public required string Name { get; init; }
    public required string Endpoint { get; init; }
    public required CustomModelWireFormat WireFormat { get; init; }
    public required string ModelName { get; init; }
    public string? ApiKey { get; init; }
    public bool ClearApiKey { get; init; }
    public int MaxInputTokens { get; init; } = 131_072;
    public int MaxOutputTokens { get; init; } = 16_384;
    public bool SupportsTools { get; init; } = true;
}

public sealed class TestCustomModelRequestV1
{
    public Guid? Id { get; init; }
    public string? Endpoint { get; init; }
    public CustomModelWireFormat? WireFormat { get; init; }
    public string? ModelName { get; init; }
    public string? ApiKey { get; init; }
    public bool ClearApiKey { get; init; }
}

public sealed class TestCustomModelResponseV1
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public long DurationMs { get; init; }
}

public sealed class ResolvedCustomModel
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Endpoint { get; init; }
    public required CustomModelWireFormat WireFormat { get; init; }
    public required string ModelName { get; init; }
    public string? ApiKey { get; init; }
    public int MaxInputTokens { get; init; }
    public int MaxOutputTokens { get; init; }
    public bool SupportsTools { get; init; }
}

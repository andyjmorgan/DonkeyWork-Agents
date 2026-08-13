namespace DonkeyWork.Agents.Persistence.Entities.Providers;

public sealed class CustomModelEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string WireFormat { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string? ApiKeyEncrypted { get; set; }
    public int MaxInputTokens { get; set; }
    public int MaxOutputTokens { get; set; }
    public bool SupportsTools { get; set; }
}

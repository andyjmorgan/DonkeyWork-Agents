namespace DonkeyWork.Agents.Persistence.Entities.A2a;

public class A2aServerHeaderConfigurationEntity
{
    public Guid Id { get; set; }

    public Guid A2aServerConfigurationId { get; set; }

    public string HeaderName { get; set; } = string.Empty;

    public string? HeaderValueEncrypted { get; set; }

    public Guid? CredentialId { get; set; }

    public string? CredentialFieldType { get; set; }

    public A2aServerConfigurationEntity A2aServerConfiguration { get; set; } = null!;
}

namespace DonkeyWork.Agents.Persistence.Entities.A2a;

public class A2aServerConfigurationEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Address { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public bool ConnectToNavi { get; set; }

    public bool PublishToMcp { get; set; }

    public int? TimeoutSeconds { get; set; }

    public ICollection<A2aServerHeaderConfigurationEntity> HeaderConfigurations { get; set; } = new List<A2aServerHeaderConfigurationEntity>();
}

namespace DonkeyWork.Agents.Persistence.Entities.Actors;

public class GrainCompactionMarkerEntity : BaseEntity
{
    public string GrainKey { get; set; } = string.Empty;

    public int AtSequenceNumber { get; set; }

    public Guid AtTurnId { get; set; }

    public string Summary { get; set; } = string.Empty;
}

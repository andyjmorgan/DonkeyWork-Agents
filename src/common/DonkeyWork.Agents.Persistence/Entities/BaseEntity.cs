using DonkeyWork.Agents.Common.Contracts.Interfaces;

namespace DonkeyWork.Agents.Persistence.Entities;

/// <summary>
/// Base class for all entities with standard properties:
/// Id, UserId, and audit timestamps.
/// </summary>
public abstract class BaseEntity : IEntity, IAuditable
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}

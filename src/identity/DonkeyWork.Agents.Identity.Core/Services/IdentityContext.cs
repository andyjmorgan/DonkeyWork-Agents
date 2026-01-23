using DonkeyWork.Agents.Identity.Contracts.Services;

namespace DonkeyWork.Agents.Identity.Core.Services;

/// <summary>
/// Scoped implementation of <see cref="IIdentityContext"/>.
/// Populated by authentication middleware.
/// </summary>
public sealed class IdentityContext : IIdentityContext
{
    public Guid UserId { get; private set; }

    public string? Email { get; private set; }

    public string? Name { get; private set; }

    public string? Username { get; private set; }

    public bool IsAuthenticated { get; private set; }

    /// <summary>
    /// Sets the identity from authenticated user claims.
    /// </summary>
    public void SetIdentity(Guid userId, string? email, string? name, string? username)
    {
        UserId = userId;
        Email = email;
        Name = name;
        Username = username;
        IsAuthenticated = true;
    }

    /// <summary>
    /// Clears the identity context.
    /// </summary>
    public void Clear()
    {
        UserId = Guid.Empty;
        Email = null;
        Name = null;
        Username = null;
        IsAuthenticated = false;
    }
}

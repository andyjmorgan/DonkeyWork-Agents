namespace DonkeyWork.Agents.Providers.Core.Middleware.Internal;

/// <summary>
/// Internal base class for conversation messages.
/// </summary>
internal abstract class InternalMessage
{
    public required InternalMessageRole Role { get; set; }
}

internal enum InternalMessageRole
{
    System,
    User,
    Assistant
}

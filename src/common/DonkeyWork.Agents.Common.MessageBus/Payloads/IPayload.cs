namespace DonkeyWork.Agents.Common.MessageBus.Payloads;

/// <summary>
/// Marker interface for types that can be published and consumed via the message bus.
/// Using an interface (not an abstract class) allows C# records and other sealed types
/// to participate without inheritance constraints.
/// </summary>
public interface IPayload
{
    string Discriminator { get; }
}

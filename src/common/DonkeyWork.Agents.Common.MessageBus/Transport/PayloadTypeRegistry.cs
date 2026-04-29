// Vendored from /Users/andrewmorgan/Personal/source/nats-jetstream-object-abstraction;
// backport changes upstream when stable. Key deviation: removed demo type registrations
// from constructor, added explicit Add<T>() API so consumers register their own types.

using DonkeyWork.Agents.Common.MessageBus.Payloads;

namespace DonkeyWork.Agents.Common.MessageBus.Transport;

public sealed class PayloadTypeRegistry
{
    private readonly Dictionary<string, Type> _byDiscriminator = new();

    /// <summary>
    /// Registers a payload type using the class name as the discriminator.
    /// </summary>
    public PayloadTypeRegistry Add<T>() where T : IPayload
    {
        _byDiscriminator[typeof(T).Name] = typeof(T);
        return this;
    }

    /// <summary>
    /// Registers a payload type with an explicit discriminator string.
    /// </summary>
    public PayloadTypeRegistry Add<T>(string discriminator) where T : IPayload
    {
        _byDiscriminator[discriminator] = typeof(T);
        return this;
    }

    public bool TryResolve(string discriminator, out Type type)
        => _byDiscriminator.TryGetValue(discriminator, out type!);
}

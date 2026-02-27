namespace DonkeyWork.Agents.Actors.Core.Providers;

internal interface IAiProviderFactory
{
    IAiProvider Create(ProviderType providerType);
}

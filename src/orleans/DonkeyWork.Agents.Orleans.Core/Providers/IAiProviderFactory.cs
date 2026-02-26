namespace DonkeyWork.Agents.Orleans.Core.Providers;

internal interface IAiProviderFactory
{
    IAiProvider Create(ProviderType providerType);
}

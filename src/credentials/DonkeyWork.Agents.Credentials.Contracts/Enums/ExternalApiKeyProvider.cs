namespace DonkeyWork.Agents.Credentials.Contracts.Enums;

/// <summary>
/// Known external API key providers.
/// </summary>
public enum ExternalApiKeyProvider
{
    // LLM Providers
    OpenAI,
    Anthropic,
    Google,

    // Payment Providers
    Stripe,

    // Email/Communication Providers
    SendGrid,
    Twilio,

    // Other
    Custom
}

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models.Interfaces;

/// <summary>
/// Configuration for Chat interface.
/// </summary>
public class ChatInterfaceConfig : InterfaceConfig
{
    /// <summary>
    /// System prompt for the chat interface.
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// Welcome message shown when chat starts.
    /// </summary>
    public string? WelcomeMessage { get; set; }
}

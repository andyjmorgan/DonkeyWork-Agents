using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Core.Templates;

/// <summary>
/// Static registry of known provider templates for sandbox credential mappings.
/// </summary>
public static class SandboxProviderTemplateRegistry
{
    private static readonly IReadOnlyList<SandboxProviderTemplate> Templates =
    [
        new SandboxProviderTemplate
        {
            Provider = OAuthProvider.GitHub,
            DisplayName = "GitHub",
            Mappings =
            [
                new SandboxProviderMappingTemplate
                {
                    BaseDomain = "api.github.com",
                    HeaderName = "Authorization",
                    HeaderValueFormat = HeaderValueFormat.Raw,
                    HeaderValuePrefix = "Bearer ",
                    CredentialFieldType = CredentialFieldType.AccessToken,
                },
                new SandboxProviderMappingTemplate
                {
                    BaseDomain = "github.com",
                    HeaderName = "Authorization",
                    HeaderValueFormat = HeaderValueFormat.BasicAuth,
                    BasicAuthUsername = "x-access-token",
                    CredentialFieldType = CredentialFieldType.AccessToken,
                },
            ],
        },
        new SandboxProviderTemplate
        {
            Provider = OAuthProvider.Microsoft,
            DisplayName = "Microsoft Graph",
            Mappings =
            [
                new SandboxProviderMappingTemplate
                {
                    BaseDomain = "graph.microsoft.com",
                    HeaderName = "Authorization",
                    HeaderValueFormat = HeaderValueFormat.Raw,
                    HeaderValuePrefix = "Bearer ",
                    CredentialFieldType = CredentialFieldType.AccessToken,
                },
            ],
        },
        new SandboxProviderTemplate
        {
            Provider = OAuthProvider.Google,
            DisplayName = "Google APIs",
            Mappings =
            [
                new SandboxProviderMappingTemplate
                {
                    BaseDomain = "www.googleapis.com",
                    HeaderName = "Authorization",
                    HeaderValueFormat = HeaderValueFormat.Raw,
                    HeaderValuePrefix = "Bearer ",
                    CredentialFieldType = CredentialFieldType.AccessToken,
                },
                new SandboxProviderMappingTemplate
                {
                    BaseDomain = "oauth2.googleapis.com",
                    HeaderName = "Authorization",
                    HeaderValueFormat = HeaderValueFormat.Raw,
                    HeaderValuePrefix = "Bearer ",
                    CredentialFieldType = CredentialFieldType.AccessToken,
                },
            ],
        },
    ];

    /// <summary>
    /// Gets the template for a specific OAuth provider.
    /// </summary>
    public static SandboxProviderTemplate? GetTemplate(OAuthProvider provider)
    {
        return Templates.FirstOrDefault(t => t.Provider == provider);
    }

    /// <summary>
    /// Gets all registered provider templates.
    /// </summary>
    public static IReadOnlyList<SandboxProviderTemplate> GetAll() => Templates;
}

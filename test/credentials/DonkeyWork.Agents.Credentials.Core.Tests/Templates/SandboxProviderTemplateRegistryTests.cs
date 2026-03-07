using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Core.Templates;
using Xunit;

namespace DonkeyWork.Agents.Credentials.Core.Tests.Templates;

public class SandboxProviderTemplateRegistryTests
{
    #region GetTemplate Tests

    [Fact]
    public void GetTemplate_GitHub_ReturnsTwoMappings()
    {
        var template = SandboxProviderTemplateRegistry.GetTemplate(OAuthProvider.GitHub);

        Assert.NotNull(template);
        Assert.Equal(OAuthProvider.GitHub, template.Provider);
        Assert.Equal("GitHub", template.DisplayName);
        Assert.Equal(2, template.Mappings.Count);
    }

    [Fact]
    public void GetTemplate_GitHub_HasApiDomainWithRawFormat()
    {
        var template = SandboxProviderTemplateRegistry.GetTemplate(OAuthProvider.GitHub)!;
        var apiMapping = template.Mappings.First(m => m.BaseDomain == "api.github.com");

        Assert.Equal("Authorization", apiMapping.HeaderName);
        Assert.Equal(HeaderValueFormat.Raw, apiMapping.HeaderValueFormat);
        Assert.Equal("Bearer ", apiMapping.HeaderValuePrefix);
        Assert.Equal(CredentialFieldType.AccessToken, apiMapping.CredentialFieldType);
    }

    [Fact]
    public void GetTemplate_GitHub_HasGitDomainWithBasicAuthFormat()
    {
        var template = SandboxProviderTemplateRegistry.GetTemplate(OAuthProvider.GitHub)!;
        var gitMapping = template.Mappings.First(m => m.BaseDomain == "github.com");

        Assert.Equal("Authorization", gitMapping.HeaderName);
        Assert.Equal(HeaderValueFormat.BasicAuth, gitMapping.HeaderValueFormat);
        Assert.Equal("x-access-token", gitMapping.BasicAuthUsername);
        Assert.Equal(CredentialFieldType.AccessToken, gitMapping.CredentialFieldType);
    }

    [Fact]
    public void GetTemplate_UnknownProvider_ReturnsNull()
    {
        var template = SandboxProviderTemplateRegistry.GetTemplate(OAuthProvider.Google);

        Assert.Null(template);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public void GetAll_ReturnsAllTemplates()
    {
        var templates = SandboxProviderTemplateRegistry.GetAll();

        Assert.NotEmpty(templates);
        Assert.Contains(templates, t => t.Provider == OAuthProvider.GitHub);
    }

    #endregion
}

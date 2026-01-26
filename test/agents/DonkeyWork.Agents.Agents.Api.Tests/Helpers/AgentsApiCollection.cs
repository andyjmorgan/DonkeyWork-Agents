namespace DonkeyWork.Agents.Agents.Api.Tests.Helpers;

/// <summary>
/// Collection definition for sharing AgentsApiFactory across all test classes.
/// This ensures only one factory instance is created, avoiding Serilog frozen logger issues.
/// </summary>
[CollectionDefinition(nameof(AgentsApiCollection))]
public class AgentsApiCollection : ICollectionFixture<AgentsApiFactory>
{
}

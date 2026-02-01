using DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;

namespace DonkeyWork.Agents.Integration.Tests.Fixtures;

[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<InfrastructureFixture>
{
    public const string Name = "IntegrationTests";
}

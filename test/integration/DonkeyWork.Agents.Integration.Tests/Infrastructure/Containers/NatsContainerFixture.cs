using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;

public class NatsContainerFixture : IAsyncLifetime
{
    private readonly IContainer _container;

    public NatsContainerFixture()
    {
        _container = new ContainerBuilder()
            .WithImage("nats:2.10-alpine")
            .WithCommand("--jetstream")
            .WithPortBinding(4222, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(4222))
            .Build();
    }

    public string Url => $"nats://localhost:{_container.GetMappedPublicPort(4222)}";

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

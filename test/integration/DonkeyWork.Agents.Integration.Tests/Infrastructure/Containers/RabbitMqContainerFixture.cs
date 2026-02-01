using Testcontainers.RabbitMq;

namespace DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;

public class RabbitMqContainerFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container;

    public RabbitMqContainerFixture()
    {
        _container = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .WithUsername("guest")
            .WithPassword("guest")
            .WithCommand("bash", "-c", "rabbitmq-plugins enable rabbitmq_stream && rabbitmq-server")
            .WithPortBinding(5552, true) // Stream port
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5672))
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public string HostName => _container.Hostname;

    public int AmqpPort => _container.GetMappedPublicPort(5672);

    public int StreamPort => _container.GetMappedPublicPort(5552);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Wait a bit for the stream plugin to be fully ready
        await Task.Delay(2000);
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

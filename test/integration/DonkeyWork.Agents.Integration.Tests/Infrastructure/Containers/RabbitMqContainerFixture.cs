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

    /// <summary>
    /// Returns the host for connecting to the RabbitMQ container.
    /// Uses localhost since ports are mapped to the host machine.
    /// </summary>
    public string HostName => "localhost";

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

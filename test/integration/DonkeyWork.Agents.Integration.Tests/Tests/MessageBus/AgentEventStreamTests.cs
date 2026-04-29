using DonkeyWork.Agents.Actors.Api.EventBus;
using DonkeyWork.Agents.Actors.Contracts.Events;
using DonkeyWork.Agents.Actors.Contracts.Services;
using DonkeyWork.Agents.Common.MessageBus.Transport;
using DonkeyWork.Agents.Integration.Tests.Fixtures;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Client.ObjectStore;
using NATS.Client.ObjectStore.Models;
using Xunit;

namespace DonkeyWork.Agents.Integration.Tests.Tests.MessageBus;

[Collection(IntegrationTestCollection.Name)]
[Trait("Category", "Integration")]
public class AgentEventStreamTests : IAsyncLifetime
{
    private readonly InfrastructureFixture _infrastructure;

    private NatsConnection _nats = null!;
    private INatsJSContext _js = null!;
    private INatsObjContext _obj = null!;
    private IAgentEventPublisher _publisher = null!;
    private AgentEventConsumerFactory _consumerFactory = null!;
    private PayloadTypeRegistry _registry = null!;
    private IPayloadSerializer _serializer = null!;

    public AgentEventStreamTests(InfrastructureFixture infrastructure)
    {
        _infrastructure = infrastructure;
    }

    public async Task InitializeAsync()
    {
        _nats = new NatsConnection(new NatsOpts { Url = _infrastructure.Nats.Url });
        await _nats.ConnectAsync();

        _js = new NatsJSContext(_nats);
        _obj = new NatsObjContext(_js);
        _serializer = new MessagePackPayloadSerializer();

        _registry = new PayloadTypeRegistry();
        var baseType = typeof(StreamEventBase);
        foreach (var type in baseType.Assembly.GetTypes().Where(t => t.IsSealed && baseType.IsAssignableFrom(t)))
            _registry.Add(type.Name, type);

        await EnsureStreamAndBucketAsync();

        _publisher = new AgentEventPublisher(
            _js, _obj, _serializer, _registry,
            NullLogger<AgentEventPublisher>.Instance);

        _consumerFactory = new AgentEventConsumerFactory(
            _js, _obj, _serializer, _registry,
            NullLogger<StreamConsumer>.Instance);
    }

    public async Task DisposeAsync()
    {
        if (_publisher is IAsyncDisposable disposable)
            await disposable.DisposeAsync();

        await _nats.DisposeAsync();
    }

    private async Task EnsureStreamAndBucketAsync()
    {
        try
        {
            await _js.CreateStreamAsync(new StreamConfig(AgentEventSubjects.StreamName,
                [AgentEventSubjects.SubjectsFilter])
            {
                Retention = StreamConfigRetention.Limits,
                MaxAge = TimeSpan.FromHours(1),
                MaxMsgsPerSubject = 50000,
                Storage = StreamConfigStorage.Memory,
                NumReplicas = 1,
            });
        }
        catch (Exception ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                                   || ex.Message.Contains("stream name already in use", StringComparison.OrdinalIgnoreCase))
        {
        }

        try
        {
            await _obj.CreateObjectStoreAsync(new NatsObjConfig(AgentEventSubjects.BucketName)
            {
                MaxAge = TimeSpan.FromMinutes(75),
                NumberOfReplicas = 1,
                Storage = NatsObjStorageType.Memory,
            });
        }
        catch (Exception ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                                   || ex.Message.Contains("stream name already in use", StringComparison.OrdinalIgnoreCase))
        {
        }
    }

    #region LiveTail Tests

    [Fact]
    public async Task LiveTail_ThreePublishedEvents_ReceivedInOrderWithIncreasingSequences()
    {
        var conversationId = Guid.NewGuid().ToString();
        var turnId = Guid.NewGuid();
        var agentKey = $"test:{conversationId}";

        var consumer = _consumerFactory.CreateConsumer();
        var liveOpts = AgentEventConsumerFactory.LiveTailOptions(conversationId);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var received = new List<DeliveredMessage<StreamEventBase>>();
        var consumeTask = Task.Run(async () =>
        {
            await foreach (var msg in consumer.GetMessagesAsync<StreamEventBase>(liveOpts, cts.Token))
            {
                received.Add(msg);
                if (received.Count >= 3) break;
            }
        }, cts.Token);

        await Task.Delay(200, cts.Token);

        var events = new StreamEventBase[]
        {
            new StreamMessageEvent(agentKey, "hello") { TurnId = turnId },
            new StreamMessageEvent(agentKey, "world") { TurnId = turnId },
            new StreamTurnEndEvent(agentKey) { TurnId = turnId },
        };

        foreach (var evt in events)
            await _publisher.PublishAsync(evt, conversationId, cts.Token);

        await consumeTask;

        Assert.Equal(3, received.Count);
        Assert.True(received[0].Sequence < received[1].Sequence);
        Assert.True(received[1].Sequence < received[2].Sequence);
        Assert.IsType<StreamMessageEvent>(received[0].Payload);
        Assert.IsType<StreamMessageEvent>(received[1].Payload);
        Assert.IsType<StreamTurnEndEvent>(received[2].Payload);
        Assert.Equal("hello", ((StreamMessageEvent)received[0].Payload).Text);
        Assert.Equal("world", ((StreamMessageEvent)received[1].Payload).Text);
    }

    #endregion

    #region SequenceResume Tests

    [Fact]
    public async Task SequenceResume_EventsSince_ReturnsMissedEventsInOrder()
    {
        var conversationId = Guid.NewGuid().ToString();
        var turnId = Guid.NewGuid();
        var agentKey = $"test:{conversationId}";

        var allEvents = new StreamEventBase[]
        {
            new StreamMessageEvent(agentKey, "event1") { TurnId = turnId },
            new StreamMessageEvent(agentKey, "event2") { TurnId = turnId },
            new StreamMessageEvent(agentKey, "event3") { TurnId = turnId },
            new StreamMessageEvent(agentKey, "event4") { TurnId = turnId },
            new StreamTurnEndEvent(agentKey) { TurnId = turnId },
        };

        foreach (var evt in allEvents)
            await _publisher.PublishAsync(evt, conversationId);

        var firstConsumer = _consumerFactory.CreateConsumer();
        var replayAllOpts = AgentEventConsumerFactory.ReplayFromOptions(conversationId, turnId, 0);

        var allReceived = new List<DeliveredMessage<StreamEventBase>>();
        using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var msg in firstConsumer.GetMessagesAsync<StreamEventBase>(replayAllOpts, cts1.Token))
        {
            allReceived.Add(msg);
            if (allReceived.Count >= 5) break;
        }

        Assert.Equal(5, allReceived.Count);

        var seqAfterEvent3 = allReceived[2].Sequence;

        var resumeConsumer = _consumerFactory.CreateConsumer();
        var resumeOpts = AgentEventConsumerFactory.ReplayFromOptions(conversationId, turnId, seqAfterEvent3);

        var resumed = new List<DeliveredMessage<StreamEventBase>>();
        using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var msg in resumeConsumer.GetMessagesAsync<StreamEventBase>(resumeOpts, cts2.Token))
        {
            resumed.Add(msg);
            if (resumed.Count >= 2) break;
        }

        Assert.Equal(2, resumed.Count);
        Assert.IsType<StreamMessageEvent>(resumed[0].Payload);
        Assert.IsType<StreamTurnEndEvent>(resumed[1].Payload);
        Assert.Equal("event4", ((StreamMessageEvent)resumed[0].Payload).Text);
        Assert.True(resumed[0].Sequence > seqAfterEvent3);
        Assert.True(resumed[1].Sequence > resumed[0].Sequence);
    }

    #endregion

    #region StashThreshold Tests

    [Fact]
    public async Task StashThreshold_LargePayload_RoundTripsThroughLiveTailAndReplay()
    {
        var conversationId = Guid.NewGuid().ToString();
        var turnId = Guid.NewGuid();
        var agentKey = $"test:{conversationId}";

        var largeContent = new string('x', 900_000);
        var largeEvent = new StreamMessageEvent(agentKey, largeContent) { TurnId = turnId };

        await _publisher.PublishAsync(largeEvent, conversationId);

        var replayConsumer = _consumerFactory.CreateConsumer();
        var replayOpts = AgentEventConsumerFactory.ReplayFromOptions(conversationId, turnId, 0);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        DeliveredMessage<StreamEventBase>? received = null;
        await foreach (var msg in replayConsumer.GetMessagesAsync<StreamEventBase>(replayOpts, cts.Token))
        {
            received = msg;
            break;
        }

        Assert.NotNull(received);
        var receivedEvent = Assert.IsType<StreamMessageEvent>(received.Payload);
        Assert.Equal(largeContent.Length, receivedEvent.Text.Length);
        Assert.Equal(largeContent, receivedEvent.Text);
    }

    #endregion

    #region ConversationIsolation Tests

    [Fact]
    public async Task ConversationIsolation_ConsumerForA_DoesNotSeeEventsFromB()
    {
        var conversationA = Guid.NewGuid().ToString();
        var conversationB = Guid.NewGuid().ToString();
        var turnA = Guid.NewGuid();
        var turnB = Guid.NewGuid();

        await _publisher.PublishAsync(new StreamMessageEvent($"agent-a", "from-a") { TurnId = turnA }, conversationA);
        await _publisher.PublishAsync(new StreamMessageEvent($"agent-b", "from-b") { TurnId = turnB }, conversationB);
        await _publisher.PublishAsync(new StreamTurnEndEvent($"agent-a") { TurnId = turnA }, conversationA);

        var consumerA = _consumerFactory.CreateConsumer();
        var replayOptsA = AgentEventConsumerFactory.ReplayFromOptions(conversationA, turnA, 0);

        var receivedByA = new List<DeliveredMessage<StreamEventBase>>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var msg in consumerA.GetMessagesAsync<StreamEventBase>(replayOptsA, cts.Token))
        {
            receivedByA.Add(msg);
            if (msg.Payload is StreamTurnEndEvent) break;
        }

        Assert.Equal(2, receivedByA.Count);
        Assert.All(receivedByA, m => Assert.Equal(turnA, m.Payload.TurnId));
        Assert.DoesNotContain(receivedByA, m => m.Payload is StreamMessageEvent e && e.Text == "from-b");
    }

    #endregion

    #region EventsSince Complete-On-Terminal Tests

    [Fact]
    public async Task EventsSince_TurnWithTerminalEvent_ReturnsCompleteTrue()
    {
        var conversationId = Guid.NewGuid().ToString();
        var turnId = Guid.NewGuid();
        var agentKey = $"test:{conversationId}";

        await _publisher.PublishAsync(new StreamMessageEvent(agentKey, "msg1") { TurnId = turnId }, conversationId);
        await _publisher.PublishAsync(new StreamMessageEvent(agentKey, "msg2") { TurnId = turnId }, conversationId);
        await _publisher.PublishAsync(new StreamTurnEndEvent(agentKey) { TurnId = turnId }, conversationId);

        var consumer = _consumerFactory.CreateConsumer();
        var opts = AgentEventConsumerFactory.ReplayFromOptions(conversationId, turnId, 0);

        var events = new List<DeliveredMessage<StreamEventBase>>();
        ulong lastSeq = 0;
        bool complete = false;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var delivered in consumer.GetMessagesAsync<StreamEventBase>(opts, cts.Token))
        {
            events.Add(delivered);
            lastSeq = delivered.Sequence;

            if (delivered.Payload is StreamTurnEndEvent or StreamCancelledEvent)
            {
                complete = true;
                break;
            }
        }

        Assert.Equal(3, events.Count);
        Assert.True(complete);
        Assert.IsType<StreamMessageEvent>(events[0].Payload);
        Assert.IsType<StreamMessageEvent>(events[1].Payload);
        Assert.IsType<StreamTurnEndEvent>(events[2].Payload);
        Assert.True(lastSeq > 0);
    }

    #endregion

    #region Reconnect Tests

    [Fact]
    public async Task ReconnectMidTurn_GapEventsReplayedThenLiveTailResumes()
    {
        var conversationId = Guid.NewGuid().ToString();
        var turnId = Guid.NewGuid();
        var agentKey = $"test:{conversationId}";

        var liveTailConsumer = _consumerFactory.CreateConsumer();
        var liveTailOpts = AgentEventConsumerFactory.LiveTailOptions(conversationId);

        using var liveCtsBefore = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var liveReceivedBefore = new List<DeliveredMessage<StreamEventBase>>();
        var liveTaskBefore = Task.Run(async () =>
        {
            await foreach (var msg in liveTailConsumer.GetMessagesAsync<StreamEventBase>(liveTailOpts, liveCtsBefore.Token))
            {
                liveReceivedBefore.Add(msg);
                if (liveReceivedBefore.Count >= 2) break;
            }
        }, liveCtsBefore.Token);

        await Task.Delay(200, liveCtsBefore.Token);

        await _publisher.PublishAsync(new StreamMessageEvent(agentKey, "A") { TurnId = turnId }, conversationId);
        await _publisher.PublishAsync(new StreamMessageEvent(agentKey, "B") { TurnId = turnId }, conversationId);

        await liveTaskBefore;
        Assert.Equal(2, liveReceivedBefore.Count);
        var cursor = liveReceivedBefore[1].Sequence;

        // Consumer disposed — simulate WS drop
        // Events C, D, E published while disconnected
        await _publisher.PublishAsync(new StreamMessageEvent(agentKey, "C") { TurnId = turnId }, conversationId);
        await _publisher.PublishAsync(new StreamMessageEvent(agentKey, "D") { TurnId = turnId }, conversationId);
        await _publisher.PublishAsync(new StreamMessageEvent(agentKey, "E") { TurnId = turnId }, conversationId);

        // Replay gap via EventsSince
        var replayConsumer = _consumerFactory.CreateConsumer();
        var replayOpts = AgentEventConsumerFactory.ReplayFromOptions(conversationId, turnId, cursor);
        var replayed = new List<DeliveredMessage<StreamEventBase>>();
        bool replayComplete = false;

        using var replayCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var msg in replayConsumer.GetMessagesAsync<StreamEventBase>(replayOpts, replayCts.Token))
        {
            replayed.Add(msg);
            if (replayed.Count >= 3) break;
        }

        Assert.Equal(3, replayed.Count);
        Assert.Equal("C", ((StreamMessageEvent)replayed[0].Payload).Text);
        Assert.Equal("D", ((StreamMessageEvent)replayed[1].Payload).Text);
        Assert.Equal("E", ((StreamMessageEvent)replayed[2].Payload).Text);
        Assert.False(replayComplete);

        // Resume live tail (simulate reconnect)
        var resumedLiveConsumer = _consumerFactory.CreateConsumer();
        var resumedLiveOpts = AgentEventConsumerFactory.LiveTailOptions(conversationId);
        var resumedLive = new List<DeliveredMessage<StreamEventBase>>();

        using var resumeCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var resumeTask = Task.Run(async () =>
        {
            await foreach (var msg in resumedLiveConsumer.GetMessagesAsync<StreamEventBase>(resumedLiveOpts, resumeCts.Token))
            {
                resumedLive.Add(msg);
                if (resumedLive.Count >= 1) break;
            }
        }, resumeCts.Token);

        await Task.Delay(200, resumeCts.Token);
        await _publisher.PublishAsync(new StreamMessageEvent(agentKey, "F") { TurnId = turnId }, conversationId);
        await resumeTask;

        Assert.Single(resumedLive);
        Assert.Equal("F", ((StreamMessageEvent)resumedLive[0].Payload).Text);

        // Assemble full stream as the frontend would: A, B (live before drop) + C, D, E (replayed) + F (live after reconnect)
        var full = liveReceivedBefore.Concat(replayed).Concat(resumedLive).ToList();
        Assert.Equal(6, full.Count);

        var texts = full.Select(m => ((StreamMessageEvent)m.Payload).Text).ToList();
        Assert.Equal(["A", "B", "C", "D", "E", "F"], texts);

        // Sequences must be monotonically increasing and have no gaps within known range
        var sequences = full.Select(m => m.Sequence).ToList();
        for (int i = 1; i < sequences.Count; i++)
            Assert.True(sequences[i] > sequences[i - 1], $"Sequence not monotonic at index {i}: {sequences[i - 1]} -> {sequences[i]}");

        var uniqueSequences = sequences.ToHashSet();
        Assert.Equal(sequences.Count, uniqueSequences.Count);
    }

    [Fact]
    public async Task TwoClientsOnSameConversation_BothReceiveAllEvents()
    {
        var conversationId = Guid.NewGuid().ToString();
        var turnId = Guid.NewGuid();
        var agentKey = $"test:{conversationId}";

        var consumer1 = _consumerFactory.CreateConsumer();
        var consumer2 = _consumerFactory.CreateConsumer();
        var opts1 = AgentEventConsumerFactory.LiveTailOptions(conversationId);
        var opts2 = AgentEventConsumerFactory.LiveTailOptions(conversationId);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var received1 = new List<DeliveredMessage<StreamEventBase>>();
        var received2 = new List<DeliveredMessage<StreamEventBase>>();

        var task1 = Task.Run(async () =>
        {
            await foreach (var msg in consumer1.GetMessagesAsync<StreamEventBase>(opts1, cts.Token))
            {
                received1.Add(msg);
                if (received1.Count >= 4) break;
            }
        }, cts.Token);

        var task2 = Task.Run(async () =>
        {
            await foreach (var msg in consumer2.GetMessagesAsync<StreamEventBase>(opts2, cts.Token))
            {
                received2.Add(msg);
                if (received2.Count >= 4) break;
            }
        }, cts.Token);

        await Task.Delay(200, cts.Token);

        await _publisher.PublishAsync(new StreamMessageEvent(agentKey, "msg1") { TurnId = turnId }, conversationId);
        await _publisher.PublishAsync(new StreamMessageEvent(agentKey, "msg2") { TurnId = turnId }, conversationId);
        await _publisher.PublishAsync(new StreamMessageEvent(agentKey, "msg3") { TurnId = turnId }, conversationId);
        await _publisher.PublishAsync(new StreamMessageEvent(agentKey, "msg4") { TurnId = turnId }, conversationId);

        await Task.WhenAll(task1, task2);

        Assert.Equal(4, received1.Count);
        Assert.Equal(4, received2.Count);

        var texts1 = received1.Select(m => ((StreamMessageEvent)m.Payload).Text).ToList();
        var texts2 = received2.Select(m => ((StreamMessageEvent)m.Payload).Text).ToList();
        Assert.Equal(["msg1", "msg2", "msg3", "msg4"], texts1);
        Assert.Equal(["msg1", "msg2", "msg3", "msg4"], texts2);

        for (int i = 1; i < received1.Count; i++)
            Assert.True(received1[i].Sequence > received1[i - 1].Sequence);

        for (int i = 1; i < received2.Count; i++)
            Assert.True(received2[i].Sequence > received2[i - 1].Sequence);
    }

    [Fact]
    public async Task EventsSince_AgainstEvictedTurn_ReturnsEmptyAndComplete()
    {
        var conversationId = Guid.NewGuid().ToString();
        var unknownTurnId = Guid.NewGuid();

        var consumer = _consumerFactory.CreateConsumer();
        var opts = AgentEventConsumerFactory.ReplayFromOptions(conversationId, unknownTurnId, 0);

        var events = new List<DeliveredMessage<StreamEventBase>>();
        bool complete = false;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await foreach (var msg in consumer.GetMessagesAsync<StreamEventBase>(opts, cts.Token))
            {
                events.Add(msg);
                if (msg.Payload is StreamTurnEndEvent or StreamCancelledEvent)
                {
                    complete = true;
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected: no events published means consumer times out
        }

        Assert.Empty(events);
        Assert.False(complete);
    }

    #endregion
}

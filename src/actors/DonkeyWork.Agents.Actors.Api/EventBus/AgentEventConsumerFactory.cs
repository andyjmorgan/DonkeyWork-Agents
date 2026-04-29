using DonkeyWork.Agents.Actors.Contracts.Events;
using DonkeyWork.Agents.Common.MessageBus.Transport;
using Microsoft.Extensions.Logging;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Client.ObjectStore;

namespace DonkeyWork.Agents.Actors.Api.EventBus;

/// <summary>
/// Factory for creating per-connection StreamConsumer instances for agent events.
/// </summary>
public sealed class AgentEventConsumerFactory
{
    private readonly INatsJSContext _js;
    private readonly INatsObjContext _obj;
    private readonly IPayloadSerializer _serializer;
    private readonly PayloadTypeRegistry _registry;
    private readonly ILogger<StreamConsumer> _consumerLog;

    public AgentEventConsumerFactory(
        INatsJSContext js,
        INatsObjContext obj,
        IPayloadSerializer serializer,
        PayloadTypeRegistry registry,
        ILogger<StreamConsumer> consumerLog)
    {
        _js = js;
        _obj = obj;
        _serializer = serializer;
        _registry = registry;
        _consumerLog = consumerLog;
    }

    public StreamConsumer CreateConsumer()
    {
        var options = new TransportOptions
        {
            Stream = AgentEventSubjects.StreamName,
            Bucket = AgentEventSubjects.BucketName,
            SubjectFilter = AgentEventSubjects.SubjectsFilter,
            Consumer = "ws-agent-events",
        };
        return new StreamConsumer(_js, _obj, _serializer, _registry, options, _consumerLog);
    }

    /// <summary>
    /// Creates ConsumerOptions for a live-tail subscription on a conversation.
    /// </summary>
    public static ConsumerOptions LiveTailOptions(string conversationId) => new()
    {
        DeliverPolicy = ConsumerConfigDeliverPolicy.New,
        FilterSubject = AgentEventSubjects.ForConversation(conversationId),
    };

    /// <summary>
    /// Creates ConsumerOptions for replaying events from a specific JetStream sequence.
    /// </summary>
    public static ConsumerOptions ReplayFromOptions(string conversationId, Guid turnId, ulong afterSequence) => new()
    {
        DeliverPolicy = ConsumerConfigDeliverPolicy.ByStartSequence,
        OptStartSeq = afterSequence + 1,
        FilterSubject = AgentEventSubjects.ForEvent(conversationId, turnId),
    };
}

using DonkeyWork.Agents.Orleans.Contracts.Events;

namespace DonkeyWork.Agents.Orleans.Contracts.Grains;

public interface IAgentResponseObserver : IGrainObserver
{
    void OnEvent(StreamEventBase streamEvent);
}

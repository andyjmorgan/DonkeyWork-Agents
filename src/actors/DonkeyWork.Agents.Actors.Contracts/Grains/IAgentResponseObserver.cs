using DonkeyWork.Agents.Actors.Contracts.Events;

namespace DonkeyWork.Agents.Actors.Contracts.Grains;

public interface IAgentResponseObserver : IGrainObserver
{
    void OnEvent(StreamEventBase streamEvent);
}

using DonkeyWork.Agents.Actors.Core.Providers.Responses;

namespace DonkeyWork.Agents.Actors.Core.Middleware.Messages;

internal class ModelMiddlewareMessage : BaseMiddlewareMessage
{
    public required ModelResponseBase ModelMessage { get; init; }
}

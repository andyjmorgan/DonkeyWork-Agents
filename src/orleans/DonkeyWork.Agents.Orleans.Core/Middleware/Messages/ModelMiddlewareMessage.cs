using DonkeyWork.Agents.Orleans.Core.Providers.Responses;

namespace DonkeyWork.Agents.Orleans.Core.Middleware.Messages;

internal class ModelMiddlewareMessage : BaseMiddlewareMessage
{
    public required ModelResponseBase ModelMessage { get; init; }
}

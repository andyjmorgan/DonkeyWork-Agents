using DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;

namespace DonkeyWork.Agents.Providers.Core.Middleware.Internal.Messages;

/// <summary>
/// Message containing model response content.
/// </summary>
internal class ModelMiddlewareMessage : BaseMiddlewareMessage
{
    public required ModelResponseBase ModelMessage { get; set; }
}

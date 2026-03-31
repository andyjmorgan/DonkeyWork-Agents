namespace DonkeyWork.Agents.Actors.Core.Middleware.Messages;

internal class RetryMessage : BaseMiddlewareMessage
{
    public required int Attempt { get; init; }
    public required int MaxRetries { get; init; }
    public required int DelayMs { get; init; }
    public required string Reason { get; init; }
}

namespace DonkeyWork.Agents.Orleans.Core.Middleware.Messages;

internal class ErrorMessage : BaseMiddlewareMessage
{
    public required string ErrorText { get; init; }
    public Exception? Exception { get; init; }
}

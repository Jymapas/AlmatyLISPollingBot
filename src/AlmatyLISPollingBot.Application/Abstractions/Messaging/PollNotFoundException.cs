namespace AlmatyLISPollingBot.Application.Abstractions.Messaging;

public sealed class PollNotFoundException : Exception
{
    public PollNotFoundException(Exception innerException)
        : base("The Telegram chat or poll could not be found.", innerException)
    {
    }
}

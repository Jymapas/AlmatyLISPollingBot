namespace AlmatyLISPollingBot.Application.Abstractions.Clock;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

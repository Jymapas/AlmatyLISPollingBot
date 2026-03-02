using AlmatyLISPollingBot.Application.Abstractions.Clock;

namespace AlmatyLISPollingBot.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

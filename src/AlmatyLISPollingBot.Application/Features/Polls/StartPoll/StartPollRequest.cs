using AlmatyLISPollingBot.Domain.Common;

namespace AlmatyLISPollingBot.Application.Features.Polls.StartPoll;

public sealed record StartPollRequest(DateOnly? TargetDate, int DesiredTournamentCount)
{
    public static StartPollRequest Default { get; } = new(null, PollRules.DefaultDesiredTournamentCount);
}

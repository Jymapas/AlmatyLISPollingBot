namespace AlmatyLISPollingBot.Application.Features.Polls.StartPoll;

public sealed record StartPollRequestParseResult(StartPollRequest? Request)
{
    public bool IsValid => Request is not null;
}

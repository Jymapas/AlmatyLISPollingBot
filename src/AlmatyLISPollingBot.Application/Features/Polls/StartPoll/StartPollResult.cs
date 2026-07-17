using AlmatyLISPollingBot.Domain.Entities;

namespace AlmatyLISPollingBot.Application.Features.Polls.StartPoll;

public sealed record StartPollResult(PollSession? PollSession, PollStartRejectionReason? RejectionReason);

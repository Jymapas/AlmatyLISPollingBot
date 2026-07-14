namespace AlmatyLISPollingBot.Application.Contracts.Polls;

public sealed record PollPublicationRequest(
    long ChatId,
    string Question,
    IReadOnlyList<string> Options,
    DateTimeOffset CloseDateUtc,
    bool IsAnonymous,
    bool AllowsMultipleAnswers,
    bool ShuffleOptions,
    bool AllowAddingOptions);

using AlmatyLISPollingBot.Application.Abstractions.Persistence;

namespace AlmatyLISPollingBot.Application.Features.ExcludedTournaments;

public sealed class ExcludeTournamentsService
{
    private readonly IExcludedTournamentRepository excludedTournamentRepository;

    public ExcludeTournamentsService(IExcludedTournamentRepository excludedTournamentRepository)
    {
        this.excludedTournamentRepository = excludedTournamentRepository;
    }

    public async Task<ExcludeTournamentsResult> ExecuteAsync(string? input, CancellationToken cancellationToken)
    {
        var parsedResult = ExcludedTournamentInputParser.Parse(input);
        if (!parsedResult.IsValid)
        {
            return parsedResult;
        }

        var addedIds = await excludedTournamentRepository.AddMissingAsync(
            parsedResult.AddedTournamentIds,
            cancellationToken);
        var alreadyExcludedIds = parsedResult.AddedTournamentIds.Except(addedIds).ToArray();

        return new ExcludeTournamentsResult(
            addedIds,
            alreadyExcludedIds,
            Array.Empty<string>(),
            IsEmptyInput: false);
    }
}

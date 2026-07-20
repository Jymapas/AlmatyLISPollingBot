using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Features.TournamentInput;

namespace AlmatyLISPollingBot.Application.Features.ExcludedTournaments;

public sealed class UnexcludeTournamentsService
{
    private readonly IExcludedTournamentRepository excludedTournamentRepository;

    public UnexcludeTournamentsService(IExcludedTournamentRepository excludedTournamentRepository)
    {
        this.excludedTournamentRepository = excludedTournamentRepository;
    }

    public async Task<UnexcludeTournamentsResult> ExecuteAsync(string? input, CancellationToken cancellationToken)
    {
        var parsedResult = TournamentInputParser.Parse(input);
        if (!parsedResult.IsValid)
        {
            return new UnexcludeTournamentsResult(
                Array.Empty<int>(),
                Array.Empty<int>(),
                parsedResult.InvalidTokens,
                parsedResult.IsEmptyInput);
        }

        var returnedTournamentIds = await excludedTournamentRepository.SoftDeleteActiveAsync(
            parsedResult.TournamentIds,
            cancellationToken);
        var alreadyIncludedTournamentIds = parsedResult.TournamentIds.Except(returnedTournamentIds).ToArray();

        return new UnexcludeTournamentsResult(
            returnedTournamentIds,
            alreadyIncludedTournamentIds,
            Array.Empty<string>(),
            IsEmptyInput: false);
    }
}

using AlmatyLISPollingBot.Application.Abstractions.Clock;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Application.Abstractions.Tournaments;
using AlmatyLISPollingBot.Application.Features.TournamentInput;

namespace AlmatyLISPollingBot.Application.Features.ForcedTournaments;

public sealed class ForceTournamentsService
{
    private readonly IClock clock;
    private readonly IForcedTournamentRepository forcedTournamentRepository;
    private readonly IChgkTournamentClient tournamentClient;

    public ForceTournamentsService(
        IClock clock,
        IForcedTournamentRepository forcedTournamentRepository,
        IChgkTournamentClient tournamentClient)
    {
        this.clock = clock;
        this.forcedTournamentRepository = forcedTournamentRepository;
        this.tournamentClient = tournamentClient;
    }

    public async Task<ForceTournamentsResult> ExecuteAsync(string? input, CancellationToken cancellationToken)
    {
        var parsedResult = TournamentInputParser.Parse(input);
        if (!parsedResult.IsValid)
        {
            return new ForceTournamentsResult(
                Array.Empty<int>(),
                Array.Empty<int>(),
                parsedResult.InvalidTokens,
                Array.Empty<int>(),
                Array.Empty<Contracts.Tournaments.TournamentDetails>(),
                parsedResult.IsEmptyInput);
        }

        var tournaments = await tournamentClient.GetTournamentsByIdsAsync(parsedResult.TournamentIds, cancellationToken);
        var foundTournamentIds = tournaments.Select(x => x.Id).ToHashSet();
        var notFoundTournamentIds = parsedResult.TournamentIds
            .Where(tournamentId => !foundTournamentIds.Contains(tournamentId))
            .ToArray();
        if (notFoundTournamentIds.Length > 0)
        {
            return new ForceTournamentsResult(
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<string>(),
                notFoundTournamentIds,
                tournaments,
                IsEmptyInput: false);
        }

        var addedTournamentIds = await forcedTournamentRepository.AddMissingAsync(
            parsedResult.TournamentIds,
            clock.UtcNow,
            cancellationToken);
        var alreadyQueuedTournamentIds = parsedResult.TournamentIds.Except(addedTournamentIds).ToArray();

        return new ForceTournamentsResult(
            addedTournamentIds,
            alreadyQueuedTournamentIds,
            Array.Empty<string>(),
            Array.Empty<int>(),
            tournaments,
            IsEmptyInput: false);
    }
}

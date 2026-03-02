using AlmatyLISPollingBot.Application.Abstractions.Tournaments;

namespace AlmatyLISPollingBot.Application.Features.MakePost;

public sealed class MakePostService
{
    private readonly IChgkTournamentClient tournamentClient;

    public MakePostService(IChgkTournamentClient tournamentClient)
    {
        this.tournamentClient = tournamentClient;
    }

    public Task GenerateDraftAsync(MakePostRequest request, CancellationToken cancellationToken)
    {
        return tournamentClient.GetTournamentsByIdsAsync(request.TournamentIds, cancellationToken);
    }
}

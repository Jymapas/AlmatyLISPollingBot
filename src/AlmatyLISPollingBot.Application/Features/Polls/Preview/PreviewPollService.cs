using AlmatyLISPollingBot.Application.Features.Polls.StartPoll;

namespace AlmatyLISPollingBot.Application.Features.Polls.Preview;

public sealed class PreviewPollService
{
    private readonly PollCandidatePreparationService candidatePreparationService;
    private readonly TournamentListFormatter tournamentListFormatter;

    public PreviewPollService(
        PollCandidatePreparationService candidatePreparationService,
        TournamentListFormatter tournamentListFormatter)
    {
        this.candidatePreparationService = candidatePreparationService;
        this.tournamentListFormatter = tournamentListFormatter;
    }

    public async Task<PollPreviewResult> ExecuteAsync(
        StartPollRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var preparationResult = await candidatePreparationService.PrepareAsync(request, cancellationToken);
        if (preparationResult.RejectionReason is not null)
        {
            return new PollPreviewResult(
                preparationResult.TargetDate,
                request.DesiredTournamentCount,
                Array.Empty<string>(),
                preparationResult.RejectionReason,
                preparationResult.ForcedCandidateCount);
        }

        var formattingResult = await tournamentListFormatter.FormatAsync(
            preparationResult.Candidates,
            TournamentIdDisplayMode.WithoutTournamentId,
            TournamentPaymentCategoriesDisplayMode.All,
            cancellationToken);

        return new PollPreviewResult(
            preparationResult.TargetDate,
            request.DesiredTournamentCount,
            formattingResult.Pages,
            RejectionReason: null,
            preparationResult.ForcedCandidateCount);
    }
}

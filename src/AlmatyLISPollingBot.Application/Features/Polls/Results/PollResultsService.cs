using System.Net;
using System.Text.Json;
using AlmatyLISPollingBot.Application.Abstractions.Persistence;
using AlmatyLISPollingBot.Domain.Enums;

namespace AlmatyLISPollingBot.Application.Features.Polls.Results;

public sealed class PollResultsService
{
    private readonly IPollSessionRepository pollSessionRepository;
    private readonly IReadOnlyLookupRepository lookupRepository;

    public PollResultsService(IPollSessionRepository pollSessionRepository, IReadOnlyLookupRepository lookupRepository)
    {
        this.pollSessionRepository = pollSessionRepository;
        this.lookupRepository = lookupRepository;
    }

    public async Task<PollResultsSummary?> GetActiveAsync(CancellationToken cancellationToken)
    {
        var session = await pollSessionRepository.GetActiveAsync(cancellationToken);
        if (session is null)
        {
            return null;
        }

        var excludedIds = (await lookupRepository.GetShadowBannedUserIdsAsync(cancellationToken)).ToHashSet();
        var selections = session.VoterStates.ToDictionary(
            x => x.Id,
            x => JsonSerializer.Deserialize<string[]>(x.OptionPersistentIdsJson) ?? Array.Empty<string>());
        var options = session.OptionStates
            .Where(x => x.IsActive && !x.IsResultsOption)
            .Select(option =>
            {
                var voters = session.VoterStates.Where(x => selections[x.Id].Contains(option.PersistentId, StringComparer.Ordinal)).ToArray();
                var known = voters.Length;
                var excluded = voters.Count(x => x.VoterKind == PollVoterKind.User && excludedIds.Contains(x.TelegramPeerId));
                var raw = Math.Max(option.TelegramVoterCount, known);
                return new PollResultsOption(option.Id, option.PersistentId, option.Text, option.Position, Math.Max(0, raw - excluded), raw, excluded, Math.Max(0, raw - known));
            })
            .OrderByDescending(x => x.AdjustedCount)
            .ThenBy(x => x.Position)
            .ToArray();
        var snapshot = session.OptionStates.Where(x => x.IsActive).Select(x => (DateTimeOffset?)x.LastSnapshotAtUtc).OrderByDescending(x => x).FirstOrDefault();
        return new PollResultsSummary(session.Id, snapshot, options);
    }

    public async Task<IReadOnlyList<PollResultsVoter>?> GetVotersAsync(Guid sessionId, Guid optionId, CancellationToken cancellationToken)
    {
        var session = await pollSessionRepository.GetActiveAsync(cancellationToken);
        if (session is null || session.Id != sessionId || !session.OptionStates.Any(x => x.Id == optionId && x.IsActive && !x.IsResultsOption))
        {
            return null;
        }

        var option = session.OptionStates.Single(x => x.Id == optionId);
        var excludedIds = (await lookupRepository.GetShadowBannedUserIdsAsync(cancellationToken)).ToHashSet();
        return session.VoterStates
            .Where(x => (JsonSerializer.Deserialize<string[]>(x.OptionPersistentIdsJson) ?? Array.Empty<string>()).Contains(option.PersistentId, StringComparer.Ordinal))
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(x => new PollResultsVoter(x.VoterKind, x.TelegramPeerId, x.DisplayName, x.Username, x.VoterKind == PollVoterKind.User && excludedIds.Contains(x.TelegramPeerId)))
            .ToArray();
    }

    public static string FormatSummary(PollResultsSummary summary, TimeZoneInfo timeZone)
    {
        var timestamp = summary.LastSnapshotAtUtc is null
            ? "нет снимка Telegram"
            : TimeZoneInfo.ConvertTime(summary.LastSnapshotAtUtc.Value, timeZone).ToString("dd.MM.yyyy HH:mm");
        var lines = new List<string> { $"<b>Результаты активного опроса</b>\nСнимок Telegram: {timestamp}" };
        lines.AddRange(summary.Options.Select(x =>
        {
            var details = x.ExcludedCount > 0 ? $" (в Telegram: {x.RawCount}, исключено: {x.ExcludedCount})" : string.Empty;
            var unmatched = x.UnmatchedCount > 0 ? $"; несопоставлено: {x.UnmatchedCount}" : string.Empty;
            return $"{WebUtility.HtmlEncode(x.Text)} — {x.AdjustedCount}{details}{unmatched}";
        }));
        return string.Join('\n', lines);
    }
}

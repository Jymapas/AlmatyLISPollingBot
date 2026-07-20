using System.Globalization;
using AlmatyLISPollingBot.Domain.Common;

namespace AlmatyLISPollingBot.Application.Features.Polls.StartPoll;

public static class StartPollRequestParser
{
    private const string DateFormat = "dd.MM.yyyy";

    public static StartPollRequestParseResult Parse(string? input)
    {
        var tokens = input?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? Array.Empty<string>();

        return tokens.Length switch
        {
            0 => new StartPollRequestParseResult(StartPollRequest.Default),
            1 when tokens[0] == PollRules.SingleTournamentCount.ToString(CultureInfo.InvariantCulture) =>
                new StartPollRequestParseResult(new StartPollRequest(null, PollRules.SingleTournamentCount)),
            1 when TryParseDate(tokens[0], out var targetDate) =>
                new StartPollRequestParseResult(new StartPollRequest(targetDate, PollRules.DefaultDesiredTournamentCount)),
            2 when TryParseDate(tokens[0], out var targetDate)
                && tokens[1] == PollRules.SingleTournamentCount.ToString(CultureInfo.InvariantCulture) =>
                new StartPollRequestParseResult(new StartPollRequest(targetDate, PollRules.SingleTournamentCount)),
            _ => new StartPollRequestParseResult(null)
        };
    }

    private static bool TryParseDate(string value, out DateOnly targetDate)
    {
        return DateOnly.TryParseExact(
            value,
            DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out targetDate);
    }
}

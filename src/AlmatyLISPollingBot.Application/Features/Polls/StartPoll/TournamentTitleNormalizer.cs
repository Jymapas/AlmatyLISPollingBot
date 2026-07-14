namespace AlmatyLISPollingBot.Application.Features.Polls.StartPoll;

internal static class TournamentTitleNormalizer
{
    private static readonly string[] TechnicalSuffixes =
    {
        " (синхрон)",
        "(асинхрон и онлайн)",
        " (асинхрон/онлайн)",
        " (асинхрон)"
    };

    public static string Normalize(string title)
    {
        ArgumentNullException.ThrowIfNull(title);

        var trimmedTitle = title.TrimEnd();
        var suffix = TechnicalSuffixes.FirstOrDefault(x => trimmedTitle.EndsWith(x, StringComparison.OrdinalIgnoreCase));

        return suffix is null
            ? trimmedTitle
            : trimmedTitle[..^suffix.Length].TrimEnd();
    }
}

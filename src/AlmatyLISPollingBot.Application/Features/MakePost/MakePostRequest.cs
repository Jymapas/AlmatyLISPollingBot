namespace AlmatyLISPollingBot.Application.Features.MakePost;

public sealed record MakePostRequest(IReadOnlyCollection<int> TournamentIds)
{
    public static MakePostRequest Parse(string rawIds)
    {
        var ids = rawIds
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .Distinct()
            .ToArray();

        if (ids.Length != 2)
        {
            throw new ArgumentException("Manual post generation requires exactly two tournament ids.", nameof(rawIds));
        }

        return new MakePostRequest(ids);
    }
}

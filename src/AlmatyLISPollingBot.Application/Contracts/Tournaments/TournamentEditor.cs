namespace AlmatyLISPollingBot.Application.Contracts.Tournaments;

public sealed record TournamentEditor(string Name, string Patronymic, string Surname)
{
    public string DisplayName
    {
        get
        {
            var initials = string.Concat(
                GetInitial(Name),
                GetInitial(Patronymic));

            return string.IsNullOrWhiteSpace(Surname)
                ? string.Join(' ', new[] { Name, Patronymic }.Where(x => !string.IsNullOrWhiteSpace(x)))
                : string.Concat(Surname, string.IsNullOrWhiteSpace(initials) ? string.Empty : $" {initials}");
        }
    }

    private static string GetInitial(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Concat(value.Trim()[0], '.');
    }
}

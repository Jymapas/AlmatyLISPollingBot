namespace AlmatyLISPollingBot.Application.Contracts.Bot;

public sealed class DatabaseConfiguration
{
    public const string SectionName = "Database";

    public string ConnectionString { get; init; } = string.Empty;
}

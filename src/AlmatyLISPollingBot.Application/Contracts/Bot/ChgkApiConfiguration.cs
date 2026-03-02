namespace AlmatyLISPollingBot.Application.Contracts.Bot;

public sealed class ChgkApiConfiguration
{
    public const string SectionName = "ChgkApi";

    public string BaseUrl { get; init; } = "https://api.rating.chgk.info/";
}

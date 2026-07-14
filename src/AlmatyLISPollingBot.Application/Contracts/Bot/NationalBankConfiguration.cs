namespace AlmatyLISPollingBot.Application.Contracts.Bot;

public sealed class NationalBankConfiguration
{
    public const string SectionName = "NationalBank";

    public string BaseUrl { get; init; } = "https://nationalbank.kz/";
}

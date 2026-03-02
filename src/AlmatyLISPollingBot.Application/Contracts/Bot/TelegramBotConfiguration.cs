namespace AlmatyLISPollingBot.Application.Contracts.Bot;

public sealed class TelegramBotConfiguration
{
    public const string SectionName = "Telegram";

    public string BotToken { get; init; } = string.Empty;
}

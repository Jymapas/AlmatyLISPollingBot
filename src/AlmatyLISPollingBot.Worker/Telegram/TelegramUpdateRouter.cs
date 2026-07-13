using AlmatyLISPollingBot.Application.Features.MakePost;
using AlmatyLISPollingBot.Application.Features.Polls.StartPoll;
using AlmatyLISPollingBot.Application.Features.Polls.StopPoll;
using AlmatyLISPollingBot.Application.Contracts.Bot;
using AlmatyLISPollingBot.Application.Contracts.Polls;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AlmatyLISPollingBot.Worker.Telegram;

public sealed class TelegramUpdateRouter
{
    private readonly StartPollService startPollService;
    private readonly StopPollService stopPollService;
    private readonly MakePostService makePostService;
    private readonly PollCommandAuthorizer pollCommandAuthorizer;
    private readonly ITelegramBotClient botClient;
    private readonly ILogger<TelegramUpdateRouter> logger;

    public TelegramUpdateRouter(
        StartPollService startPollService,
        StopPollService stopPollService,
        MakePostService makePostService,
        PollCommandAuthorizer pollCommandAuthorizer,
        ITelegramBotClient botClient,
        ILogger<TelegramUpdateRouter> logger)
    {
        this.startPollService = startPollService;
        this.stopPollService = stopPollService;
        this.makePostService = makePostService;
        this.pollCommandAuthorizer = pollCommandAuthorizer;
        this.botClient = botClient;
        this.logger = logger;
    }

    public async Task RouteAsync(Update update, CancellationToken cancellationToken)
    {
        var message = update.Message;
        var messageText = message?.Text;
        var user = message?.From;
        if (string.IsNullOrWhiteSpace(messageText) || user is null || message is null)
        {
            return;
        }

        var commandContext = new PollCommandContext(
            message.Chat.Id,
            user.Id,
            message.Chat.Type == ChatType.Private);

        if (await IsBotCommandAsync(messageText, BotCommands.Poll, cancellationToken))
        {
            if (!await pollCommandAuthorizer.IsAuthorizedAsync(commandContext, cancellationToken))
            {
                return;
            }

            await startPollService.StartAsync(cancellationToken);
            logger.LogInformation("Received /poll command.");
            return;
        }

        if (await IsBotCommandAsync(messageText, BotCommands.Stop, cancellationToken))
        {
            if (!await pollCommandAuthorizer.IsAuthorizedAsync(commandContext, cancellationToken))
            {
                return;
            }

            await stopPollService.StopActivePollAsync(cancellationToken);
            logger.LogInformation("Received /stop command.");
            return;
        }

        if (await IsBotCommandAsync(messageText, BotCommands.MakePost, cancellationToken))
        {
            if (!commandContext.IsPrivateChat
                || !await pollCommandAuthorizer.IsAuthorizedAsync(commandContext, cancellationToken))
            {
                return;
            }

            var payload = GetCommandPayload(messageText);
            await makePostService.GenerateDraftAsync(MakePostRequest.Parse(payload), cancellationToken);
            logger.LogInformation("Received /makepost command.");
        }
    }

    private async Task<bool> IsBotCommandAsync(string messageText, string commandName, CancellationToken cancellationToken)
    {
        var commandToken = GetCommandToken(messageText);
        var command = BotCommands.ToMessageCommand(commandName);
        if (string.Equals(commandToken, command, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var commandWithUsernamePrefix = string.Concat(command, "@");
        if (!commandToken.StartsWith(commandWithUsernamePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var botUsername = (await botClient.GetMe(cancellationToken)).Username;
        return !string.IsNullOrWhiteSpace(botUsername)
            && string.Equals(
                commandToken[commandWithUsernamePrefix.Length..],
                botUsername,
                StringComparison.OrdinalIgnoreCase);
    }

    private static string GetCommandToken(string messageText)
    {
        var separatorIndex = messageText.IndexOfAny(new[] { ' ', '\n', '\r', '\t' });
        return separatorIndex < 0 ? messageText : messageText[..separatorIndex];
    }

    private static string GetCommandPayload(string messageText)
    {
        var commandToken = GetCommandToken(messageText);
        return messageText[commandToken.Length..].Trim();
    }
}

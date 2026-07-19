using AlmatyLISPollingBot.Application.Abstractions.Messaging;
using AlmatyLISPollingBot.Application.Contracts.Polls;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AlmatyLISPollingBot.Worker.Telegram;

public sealed class TelegramPollPublisher : IPollPublisher
{
    private readonly ITelegramBotClient botClient;

    public TelegramPollPublisher(ITelegramBotClient botClient)
    {
        this.botClient = botClient;
    }

    public async Task<int> SendHtmlMessageAsync(long chatId, string message, CancellationToken cancellationToken)
    {
        var sentMessage = await botClient.SendMessage(
            chatId,
            message,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
        return sentMessage.MessageId;
    }

    public async Task<PublishedPoll> SendPollAsync(PollPublicationRequest request, CancellationToken cancellationToken)
    {
        var sentMessage = await botClient.SendPoll(
            request.ChatId,
            request.Question,
            request.Options.Select(x => new InputPollOption(x)),
            isAnonymous: request.IsAnonymous,
            allowsMultipleAnswers: request.AllowsMultipleAnswers,
            closeDate: request.CloseDateUtc.UtcDateTime,
            shuffleOptions: request.ShuffleOptions,
            allowAddingOptions: request.AllowAddingOptions,
            cancellationToken: cancellationToken);
        var pollId = sentMessage.Poll?.Id
            ?? throw new InvalidOperationException("Telegram returned a poll message without a poll identifier.");

        var options = sentMessage.Poll?.Options
            .Select((x, index) => new PublishedPollOption(x.PersistentId, x.Text, index))
            .ToArray();
        return new PublishedPoll(pollId, sentMessage.MessageId, options);
    }

    public async Task StopPollAsync(long chatId, int pollMessageId, CancellationToken cancellationToken)
    {
        try
        {
            await botClient.StopPoll(chatId, pollMessageId, cancellationToken: cancellationToken);
        }
        catch (ApiRequestException exception) when (IsStoppedPollUnavailable(exception))
        {
            throw new PollNotFoundException(exception);
        }
    }

    public async Task DeleteMessageAsync(long chatId, int messageId, CancellationToken cancellationToken)
    {
        await botClient.DeleteMessage(chatId, messageId, cancellationToken);
    }

    private static bool IsStoppedPollUnavailable(ApiRequestException exception)
    {
        return exception.Message.Contains("chat not found", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("poll has already been closed", StringComparison.OrdinalIgnoreCase);
    }
}

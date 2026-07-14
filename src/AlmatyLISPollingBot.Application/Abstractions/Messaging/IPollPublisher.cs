using AlmatyLISPollingBot.Application.Contracts.Polls;

namespace AlmatyLISPollingBot.Application.Abstractions.Messaging;

public interface IPollPublisher
{
    Task<int> SendHtmlMessageAsync(long chatId, string message, CancellationToken cancellationToken);
    Task<PublishedPoll> SendPollAsync(PollPublicationRequest request, CancellationToken cancellationToken);
    Task StopPollAsync(long chatId, int pollMessageId, CancellationToken cancellationToken);
    Task DeleteMessageAsync(long chatId, int messageId, CancellationToken cancellationToken);
}

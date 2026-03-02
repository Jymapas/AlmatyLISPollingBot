using AlmatyLISPollingBot.Application.Features.MakePost;
using AlmatyLISPollingBot.Application.Features.Polls.StartPoll;
using AlmatyLISPollingBot.Application.Features.Polls.StopPoll;
using Telegram.Bot.Types;

namespace AlmatyLISPollingBot.Worker.Telegram;

public sealed class TelegramUpdateRouter
{
    private readonly StartPollService startPollService;
    private readonly StopPollService stopPollService;
    private readonly MakePostService makePostService;
    private readonly ILogger<TelegramUpdateRouter> logger;

    public TelegramUpdateRouter(
        StartPollService startPollService,
        StopPollService stopPollService,
        MakePostService makePostService,
        ILogger<TelegramUpdateRouter> logger)
    {
        this.startPollService = startPollService;
        this.stopPollService = stopPollService;
        this.makePostService = makePostService;
        this.logger = logger;
    }

    public async Task RouteAsync(Update update, CancellationToken cancellationToken)
    {
        var messageText = update.Message?.Text;
        if (string.IsNullOrWhiteSpace(messageText))
        {
            return;
        }

        if (messageText.StartsWith("/poll", StringComparison.OrdinalIgnoreCase))
        {
            await startPollService.CreateDraftAsync(cancellationToken);
            logger.LogInformation("Received /poll command.");
            return;
        }

        if (messageText.StartsWith("/stop", StringComparison.OrdinalIgnoreCase))
        {
            await stopPollService.StopActivePollAsync(cancellationToken);
            logger.LogInformation("Received /stop command.");
            return;
        }

        if (messageText.StartsWith("/makepost", StringComparison.OrdinalIgnoreCase))
        {
            var payload = messageText["/makepost".Length..].Trim();
            await makePostService.GenerateDraftAsync(MakePostRequest.Parse(payload), cancellationToken);
            logger.LogInformation("Received /makepost command.");
        }
    }
}

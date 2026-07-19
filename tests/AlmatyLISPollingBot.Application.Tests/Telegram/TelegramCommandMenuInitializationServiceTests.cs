using AlmatyLISPollingBot.Application.Contracts.Bot;
using AlmatyLISPollingBot.Worker.HostedServices;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests.Telegram;

public sealed class TelegramCommandMenuInitializationServiceTests
{
    [Fact]
    public void AdministratorCommands_ShouldContainOnlyPollAndStop()
    {
        TelegramCommandMenuInitializationService.AdministratorCommands
            .Select(x => x.Command)
            .Should()
            .Equal(BotCommands.Poll, BotCommands.Stop);
    }

    [Fact]
    public void PrivateAdministratorCommands_ShouldContainAllPrivateAdministratorCommands()
    {
        TelegramCommandMenuInitializationService.PrivateAdministratorCommands
            .Select(x => x.Command)
            .Should()
            .Equal(
                BotCommands.Poll,
                BotCommands.Stop,
                BotCommands.Preview,
                BotCommands.Options,
                BotCommands.Exclude,
                BotCommands.Excluded,
                BotCommands.Unexclude,
                BotCommands.Force,
                BotCommands.Cancel,
                BotCommands.MakePost,
                BotCommands.Results);
    }
}

using AlmatyLISPollingBot.Worker.Telegram;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests.Telegram;

public sealed class InMemoryExcludeDialogStateTests
{
    [Fact]
    public void Start_ShouldTrackOnlyTheSpecifiedAdministrator()
    {
        var sut = new InMemoryExcludeDialogState();

        sut.Start(7);

        sut.IsAwaitingInput(7).Should().BeTrue();
        sut.IsAwaitingInput(8).Should().BeFalse();
    }

    [Fact]
    public void Cancel_ShouldRemoveOnlyTheSpecifiedAdministratorState()
    {
        var sut = new InMemoryExcludeDialogState();
        sut.Start(7);
        sut.Start(8);

        var result = sut.Cancel(7);

        result.Should().BeTrue();
        sut.IsAwaitingInput(7).Should().BeFalse();
        sut.IsAwaitingInput(8).Should().BeTrue();
    }
}

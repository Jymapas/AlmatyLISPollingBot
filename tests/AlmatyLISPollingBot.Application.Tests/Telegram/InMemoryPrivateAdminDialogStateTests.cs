using AlmatyLISPollingBot.Worker.Telegram;
using FluentAssertions;

namespace AlmatyLISPollingBot.Application.Tests.Telegram;

public sealed class InMemoryPrivateAdminDialogStateTests
{
    [Fact]
    public void Start_ShouldTrackOnlyTheSpecifiedAdministratorAndDialog()
    {
        var sut = new InMemoryPrivateAdminDialogState();

        sut.Start(7, PrivateAdminDialogKind.ExcludeTournaments);

        sut.GetActive(7).Should().Be(PrivateAdminDialogKind.ExcludeTournaments);
        sut.GetActive(8).Should().BeNull();
    }

    [Fact]
    public void Start_ShouldReplaceTheSpecifiedAdministratorsExistingDialog()
    {
        var sut = new InMemoryPrivateAdminDialogState();
        sut.Start(7, PrivateAdminDialogKind.ExcludeTournaments);

        sut.Start(7, PrivateAdminDialogKind.ForceTournaments);

        sut.GetActive(7).Should().Be(PrivateAdminDialogKind.ForceTournaments);
    }

    [Fact]
    public void Cancel_ShouldRemoveOnlyTheSpecifiedAdministratorState()
    {
        var sut = new InMemoryPrivateAdminDialogState();
        sut.Start(7, PrivateAdminDialogKind.ExcludeTournaments);
        sut.Start(8, PrivateAdminDialogKind.ForceTournaments);

        var result = sut.Cancel(7);

        result.Should().Be(PrivateAdminDialogKind.ExcludeTournaments);
        sut.GetActive(7).Should().BeNull();
        sut.GetActive(8).Should().Be(PrivateAdminDialogKind.ForceTournaments);
    }
}

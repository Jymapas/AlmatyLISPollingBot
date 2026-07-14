using AlmatyLISPollingBot.Domain.Common;

namespace AlmatyLISPollingBot.Domain.Entities;

public sealed class ForcedTournament : Entity
{
    public int TournamentId { get; set; }
    public DateTimeOffset QueuedAtUtc { get; set; }
}

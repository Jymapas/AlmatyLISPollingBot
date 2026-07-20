using AlmatyLISPollingBot.Domain.Common;

namespace AlmatyLISPollingBot.Domain.Entities;

public sealed class ExcludedTournament : Entity
{
    public int TournamentId { get; set; }
    public string? Reason { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
}

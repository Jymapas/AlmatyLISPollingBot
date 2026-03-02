using AlmatyLISPollingBot.Domain.Common;

namespace AlmatyLISPollingBot.Domain.Entities;

public sealed class TournamentHistoryEntry : Entity
{
    public int TournamentId { get; set; }
    public DateOnly PlayDate { get; set; }
    public TimeOnly SlotTime { get; set; }
}

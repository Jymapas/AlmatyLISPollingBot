using AlmatyLISPollingBot.Domain.Common;

namespace AlmatyLISPollingBot.Domain.Entities;

public sealed class PollCandidate : Entity
{
    public Guid PollSessionId { get; set; }
    public int TournamentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal? DifficultyForecast { get; set; }
    public bool IsAvailableAtFirstSlot { get; set; }
    public bool IsAvailableAtSecondSlot { get; set; }
    public int SortOrder { get; set; }
}

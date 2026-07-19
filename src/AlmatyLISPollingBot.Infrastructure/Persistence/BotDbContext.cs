using AlmatyLISPollingBot.Domain.Common;
using AlmatyLISPollingBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlmatyLISPollingBot.Infrastructure.Persistence;

public sealed class BotDbContext : DbContext
{
    public BotDbContext(DbContextOptions<BotDbContext> options)
        : base(options)
    {
    }

    public DbSet<BotSettings> BotSettings => Set<BotSettings>();
    public DbSet<ChatAdministrator> ChatAdministrators => Set<ChatAdministrator>();
    public DbSet<ExcludedTournament> ExcludedTournaments => Set<ExcludedTournament>();
    public DbSet<ForcedTournament> ForcedTournaments => Set<ForcedTournament>();
    public DbSet<ShadowBannedUser> ShadowBannedUsers => Set<ShadowBannedUser>();
    public DbSet<TournamentHistoryEntry> TournamentHistoryEntries => Set<TournamentHistoryEntry>();
    public DbSet<PollSession> PollSessions => Set<PollSession>();
    public DbSet<PollCandidate> PollCandidates => Set<PollCandidate>();
    public DbSet<PollOptionState> PollOptionStates => Set<PollOptionState>();
    public DbSet<PollVoterState> PollVoterStates => Set<PollVoterState>();
    public DbSet<CurrencyExchangeRate> CurrencyExchangeRates => Set<CurrencyExchangeRate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BotSettings>(entity =>
        {
            entity.ToTable("bot_settings");
            entity.Property(x => x.ApplicationTimeZone).HasMaxLength(128);
            entity.Property(x => x.Venue).HasMaxLength(512);
        });

        modelBuilder.Entity<ChatAdministrator>(entity =>
        {
            entity.ToTable("chat_administrators");
            entity.HasIndex(x => x.TelegramUserId).IsUnique();
        });

        modelBuilder.Entity<ExcludedTournament>(entity =>
        {
            entity.ToTable("excluded_tournaments");
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.HasIndex(x => x.TournamentId).IsUnique();
        });

        modelBuilder.Entity<ForcedTournament>(entity =>
        {
            entity.ToTable("forced_tournaments");
            entity.HasIndex(x => x.TournamentId).IsUnique();
        });

        modelBuilder.Entity<ShadowBannedUser>(entity =>
        {
            entity.ToTable("shadow_banned_users");
            entity.Property(x => x.IsDeleted).HasDefaultValue(false);
            entity.HasIndex(x => x.TelegramUserId).IsUnique();
        });

        modelBuilder.Entity<TournamentHistoryEntry>(entity =>
        {
            entity.ToTable("tournament_history_entries");
        });

        modelBuilder.Entity<PollSession>(entity =>
        {
            entity.ToTable("poll_sessions");
            entity.Property(x => x.DesiredTournamentCount).HasDefaultValue(PollRules.DefaultDesiredTournamentCount);
            entity.HasMany(x => x.Candidates)
                .WithOne()
                .HasForeignKey(x => x.PollSessionId);
            entity.HasMany(x => x.OptionStates)
                .WithOne()
                .HasForeignKey(x => x.PollSessionId);
            entity.HasMany(x => x.VoterStates)
                .WithOne()
                .HasForeignKey(x => x.PollSessionId);
        });

        modelBuilder.Entity<PollCandidate>(entity =>
        {
            entity.ToTable("poll_candidates");
        });

        modelBuilder.Entity<PollOptionState>(entity =>
        {
            entity.ToTable("poll_option_states");
            entity.Property(x => x.PersistentId).HasMaxLength(128);
            entity.Property(x => x.Text).HasMaxLength(512);
            entity.HasIndex(x => new { x.PollSessionId, x.PersistentId }).IsUnique();
        });

        modelBuilder.Entity<PollVoterState>(entity =>
        {
            entity.ToTable("poll_voter_states");
            entity.Property(x => x.DisplayName).HasMaxLength(512);
            entity.Property(x => x.Username).HasMaxLength(128);
            entity.Property(x => x.OptionPersistentIdsJson).HasMaxLength(4096);
            entity.HasIndex(x => new { x.PollSessionId, x.VoterKind, x.TelegramPeerId }).IsUnique();
        });

        modelBuilder.Entity<CurrencyExchangeRate>(entity =>
        {
            entity.ToTable("currency_exchange_rates");
            entity.Property(x => x.CurrencyCode).HasMaxLength(3);
            entity.HasIndex(x => x.CurrencyCode).IsUnique();
        });
    }
}

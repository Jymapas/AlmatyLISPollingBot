using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlmatyLISPollingBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyExchangeRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bot_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetChatId = table.Column<long>(type: "bigint", nullable: false),
                    MainAdminUserId = table.Column<long>(type: "bigint", nullable: false),
                    ApplicationTimeZone = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DefaultPollStopTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Venue = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bot_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chat_administrators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: true),
                    SyncedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_administrators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "currency_exchange_rates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TengePerNominal = table.Column<decimal>(type: "numeric", nullable: false),
                    Nominal = table.Column<int>(type: "integer", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currency_exchange_rates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "excluded_tournaments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_excluded_tournaments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "poll_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    TelegramPollId = table.Column<string>(type: "text", nullable: true),
                    PollMessageId = table.Column<int>(type: "integer", nullable: true),
                    ListMessageId = table.Column<int>(type: "integer", nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ScheduledStopAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StoppedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poll_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "shadow_banned_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shadow_banned_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tournament_history_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<int>(type: "integer", nullable: false),
                    PlayDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SlotTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_history_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "poll_candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PollSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    DifficultyForecast = table.Column<decimal>(type: "numeric", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poll_candidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_poll_candidates_poll_sessions_PollSessionId",
                        column: x => x.PollSessionId,
                        principalTable: "poll_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chat_administrators_TelegramUserId",
                table: "chat_administrators",
                column: "TelegramUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_currency_exchange_rates_CurrencyCode",
                table: "currency_exchange_rates",
                column: "CurrencyCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_excluded_tournaments_TournamentId",
                table: "excluded_tournaments",
                column: "TournamentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_poll_candidates_PollSessionId",
                table: "poll_candidates",
                column: "PollSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_shadow_banned_users_TelegramUserId",
                table: "shadow_banned_users",
                column: "TelegramUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bot_settings");

            migrationBuilder.DropTable(
                name: "chat_administrators");

            migrationBuilder.DropTable(
                name: "currency_exchange_rates");

            migrationBuilder.DropTable(
                name: "excluded_tournaments");

            migrationBuilder.DropTable(
                name: "poll_candidates");

            migrationBuilder.DropTable(
                name: "shadow_banned_users");

            migrationBuilder.DropTable(
                name: "tournament_history_entries");

            migrationBuilder.DropTable(
                name: "poll_sessions");
        }
    }
}

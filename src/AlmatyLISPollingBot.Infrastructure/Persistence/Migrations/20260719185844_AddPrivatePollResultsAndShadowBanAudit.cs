using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlmatyLISPollingBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivatePollResultsAndShadowBanAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExcludedAtUtc",
                table: "shadow_banned_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ExcludedByTelegramUserId",
                table: "shadow_banned_users",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "shadow_banned_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReturnedAtUtc",
                table: "shadow_banned_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReturnedByTelegramUserId",
                table: "shadow_banned_users",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "poll_option_states",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PollSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersistentId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Text = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    TelegramVoterCount = table.Column<int>(type: "integer", nullable: false),
                    IsResultsOption = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastSnapshotAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poll_option_states", x => x.Id);
                    table.ForeignKey(
                        name: "FK_poll_option_states_poll_sessions_PollSessionId",
                        column: x => x.PollSessionId,
                        principalTable: "poll_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "poll_voter_states",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PollSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoterKind = table.Column<int>(type: "integer", nullable: false),
                    TelegramPeerId = table.Column<long>(type: "bigint", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Username = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    OptionPersistentIdsJson = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    LastUpdateId = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poll_voter_states", x => x.Id);
                    table.ForeignKey(
                        name: "FK_poll_voter_states_poll_sessions_PollSessionId",
                        column: x => x.PollSessionId,
                        principalTable: "poll_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_poll_option_states_PollSessionId_PersistentId",
                table: "poll_option_states",
                columns: new[] { "PollSessionId", "PersistentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_poll_voter_states_PollSessionId_VoterKind_TelegramPeerId",
                table: "poll_voter_states",
                columns: new[] { "PollSessionId", "VoterKind", "TelegramPeerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "poll_option_states");

            migrationBuilder.DropTable(
                name: "poll_voter_states");

            migrationBuilder.DropColumn(
                name: "ExcludedAtUtc",
                table: "shadow_banned_users");

            migrationBuilder.DropColumn(
                name: "ExcludedByTelegramUserId",
                table: "shadow_banned_users");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "shadow_banned_users");

            migrationBuilder.DropColumn(
                name: "ReturnedAtUtc",
                table: "shadow_banned_users");

            migrationBuilder.DropColumn(
                name: "ReturnedByTelegramUserId",
                table: "shadow_banned_users");
        }
    }
}

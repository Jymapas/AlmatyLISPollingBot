using System;
using AlmatyLISPollingBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlmatyLISPollingBot.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BotDbContext))]
[Migration("20260715120000_AddForcedTournaments")]
public sealed class AddForcedTournaments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "forced_tournaments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TournamentId = table.Column<int>(type: "integer", nullable: false),
                QueuedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_forced_tournaments", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_forced_tournaments_TournamentId",
            table: "forced_tournaments",
            column: "TournamentId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "forced_tournaments");
    }
}

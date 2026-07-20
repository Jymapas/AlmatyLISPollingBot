using AlmatyLISPollingBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlmatyLISPollingBot.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BotDbContext))]
[Migration("20260717220000_AddPollSessionDesiredTournamentCount")]
public sealed class AddPollSessionDesiredTournamentCount : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "DesiredTournamentCount",
            table: "poll_sessions",
            type: "integer",
            nullable: false,
            defaultValue: 2);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DesiredTournamentCount",
            table: "poll_sessions");
    }
}

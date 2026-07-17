using System;
using AlmatyLISPollingBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlmatyLISPollingBot.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BotDbContext))]
[Migration("20260717213000_AddExcludedTournamentSoftDelete")]
public sealed class AddExcludedTournamentSoftDelete : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "DeletedAtUtc",
            table: "excluded_tournaments",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsDeleted",
            table: "excluded_tournaments",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DeletedAtUtc",
            table: "excluded_tournaments");

        migrationBuilder.DropColumn(
            name: "IsDeleted",
            table: "excluded_tournaments");
    }
}

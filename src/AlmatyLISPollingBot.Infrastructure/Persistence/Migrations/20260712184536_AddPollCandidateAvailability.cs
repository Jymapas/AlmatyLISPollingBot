using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlmatyLISPollingBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPollCandidateAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAvailableAtFirstSlot",
                table: "poll_candidates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailableAtSecondSlot",
                table: "poll_candidates",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAvailableAtFirstSlot",
                table: "poll_candidates");

            migrationBuilder.DropColumn(
                name: "IsAvailableAtSecondSlot",
                table: "poll_candidates");
        }
    }
}

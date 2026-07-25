using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsAnalytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArchiveFieldsToPrediction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActualAwayGoals",
                table: "Predictions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActualHomeGoals",
                table: "Predictions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActualResult",
                table: "Predictions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "Predictions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MatchId1",
                table: "MatchStatistics",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchStatistics_MatchId1",
                table: "MatchStatistics",
                column: "MatchId1",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchStatistics_Matches_MatchId1",
                table: "MatchStatistics",
                column: "MatchId1",
                principalTable: "Matches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchStatistics_Matches_MatchId1",
                table: "MatchStatistics");

            migrationBuilder.DropIndex(
                name: "IX_MatchStatistics_MatchId1",
                table: "MatchStatistics");

            migrationBuilder.DropColumn(
                name: "ActualAwayGoals",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "ActualHomeGoals",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "ActualResult",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "MatchId1",
                table: "MatchStatistics");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsAnalytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    League = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HomeTeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    AwayTeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    MatchDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HomeGoals = table.Column<int>(type: "INTEGER", nullable: true),
                    AwayGoals = table.Column<int>(type: "INTEGER", nullable: true),
                    League = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Season = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_AwayTeamId",
                        column: x => x.AwayTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_HomeTeamId",
                        column: x => x.HomeTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Position = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    IsInjured = table.Column<bool>(type: "INTEGER", nullable: false),
                    InjuryUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchStatistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    HomeShotsOnTarget = table.Column<int>(type: "INTEGER", nullable: false),
                    HomeShotsTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    HomePossessionPct = table.Column<double>(type: "REAL", nullable: false),
                    HomeCorners = table.Column<int>(type: "INTEGER", nullable: false),
                    HomeFouls = table.Column<int>(type: "INTEGER", nullable: false),
                    HomeYellowCards = table.Column<int>(type: "INTEGER", nullable: false),
                    HomeRedCards = table.Column<int>(type: "INTEGER", nullable: false),
                    AwayShotsOnTarget = table.Column<int>(type: "INTEGER", nullable: false),
                    AwayShotsTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    AwayPossessionPct = table.Column<double>(type: "REAL", nullable: false),
                    AwayCorners = table.Column<int>(type: "INTEGER", nullable: false),
                    AwayFouls = table.Column<int>(type: "INTEGER", nullable: false),
                    AwayYellowCards = table.Column<int>(type: "INTEGER", nullable: false),
                    AwayRedCards = table.Column<int>(type: "INTEGER", nullable: false),
                    DataQualityScore = table.Column<double>(type: "REAL", nullable: false),
                    DataSource = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchStatistics_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Predictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    HomeWinProbability = table.Column<double>(type: "REAL", nullable: false),
                    DrawProbability = table.Column<double>(type: "REAL", nullable: false),
                    AwayWinProbability = table.Column<double>(type: "REAL", nullable: false),
                    LambdaHome = table.Column<double>(type: "REAL", nullable: false),
                    LambdaAway = table.Column<double>(type: "REAL", nullable: false),
                    RiskScore = table.Column<double>(type: "REAL", nullable: false),
                    ModelVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Predictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Predictions_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_AwayTeamId",
                table: "Matches",
                column: "AwayTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_HomeTeamId_AwayTeamId",
                table: "Matches",
                columns: new[] { "HomeTeamId", "AwayTeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_MatchDate",
                table: "Matches",
                column: "MatchDate");

            migrationBuilder.CreateIndex(
                name: "IX_MatchStatistics_MatchId",
                table: "MatchStatistics",
                column: "MatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamId",
                table: "Players",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_CreatedAt",
                table: "Predictions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_MatchId",
                table: "Predictions",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_Name",
                table: "Teams",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchStatistics");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Predictions");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Teams");
        }
    }
}

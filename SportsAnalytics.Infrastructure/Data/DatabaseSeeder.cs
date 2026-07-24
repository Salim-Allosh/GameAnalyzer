using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SportsAnalytics.Domain.Entities;

namespace SportsAnalytics.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(SqliteDbContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // If teams already exist, do not seed
        if (await context.Teams.AnyAsync())
            return;

        // 1. Seed Teams
        var team1 = new Team { Name = "Real Madrid", Country = "Spain", League = "La Liga" };
        var team2 = new Team { Name = "Barcelona", Country = "Spain", League = "La Liga" };
        var team3 = new Team { Name = "Manchester City", Country = "England", League = "Premier League" };
        var team4 = new Team { Name = "Arsenal", Country = "England", League = "Premier League" };
        var team5 = new Team { Name = "Bayern Munich", Country = "Germany", League = "Bundesliga" };
        var team6 = new Team { Name = "Dortmund", Country = "Germany", League = "Bundesliga" };

        context.Teams.AddRange(team1, team2, team3, team4, team5, team6);
        await context.SaveChangesAsync();

        // 2. Seed some past matches (for model training if needed)
        var match1 = new Match { HomeTeamId = team1.Id, AwayTeamId = team2.Id, MatchDate = DateTime.UtcNow.AddDays(-10), HomeGoals = 2, AwayGoals = 1, League = "La Liga", Season = "2023-2024" };
        var match2 = new Match { HomeTeamId = team3.Id, AwayTeamId = team4.Id, MatchDate = DateTime.UtcNow.AddDays(-5), HomeGoals = 1, AwayGoals = 1, League = "Premier League", Season = "2023-2024" };
        
        // 3. Seed some upcoming matches (for the user to analyze)
        var upcomingMatch1 = new Match { HomeTeamId = team1.Id, AwayTeamId = team3.Id, MatchDate = DateTime.UtcNow.AddDays(2), League = "Champions League", Season = "2023-2024" };
        var upcomingMatch2 = new Match { HomeTeamId = team2.Id, AwayTeamId = team5.Id, MatchDate = DateTime.UtcNow.AddDays(3), League = "Champions League", Season = "2023-2024" };
        var upcomingMatch3 = new Match { HomeTeamId = team4.Id, AwayTeamId = team6.Id, MatchDate = DateTime.UtcNow.AddDays(4), League = "Champions League", Season = "2023-2024" };

        context.Matches.AddRange(match1, match2, upcomingMatch1, upcomingMatch2, upcomingMatch3);
        await context.SaveChangesAsync();
    }
}

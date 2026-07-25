using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SportsAnalytics.Domain.Entities;
using SportsAnalytics.Domain.Models;
using SportsAnalytics.Infrastructure.Data;

namespace SportsAnalytics.Application.Services;

public class TeamStatisticsService
{
    private readonly SqliteDbContext _dbContext;

    public TeamStatisticsService(SqliteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TeamDetailedStats> GetTeamStatsAsync(int teamId, int numberOfMatches)
    {
        var team = await _dbContext.Teams.FindAsync(teamId);
        if (team == null) return new TeamDetailedStats();

        var recentMatches = await _dbContext.Matches
            .Include(m => m.Statistics)
            .Where(m => m.HomeTeamId == teamId || m.AwayTeamId == teamId)
            .OrderByDescending(m => m.MatchDate)
            .Take(numberOfMatches)
            .ToListAsync();

        if (!recentMatches.Any())
        {
            return new TeamDetailedStats { TeamName = team.Name, MatchesAnalyzed = 0 };
        }

        double totalGoalsScored = 0;
        double totalGoalsConceded = 0;
        double totalCorners = 0;
        double totalYellowCards = 0;
        string form = "";

        foreach (var match in recentMatches)
        {
            bool isHome = match.HomeTeamId == teamId;
            
            int goalsScored = isHome ? match.HomeGoals.GetValueOrDefault(0) : match.AwayGoals.GetValueOrDefault(0);
            int goalsConceded = isHome ? match.AwayGoals.GetValueOrDefault(0) : match.HomeGoals.GetValueOrDefault(0);
            
            totalGoalsScored += goalsScored;
            totalGoalsConceded += goalsConceded;

            if (goalsScored > goalsConceded) form += "W ";
            else if (goalsScored < goalsConceded) form += "L ";
            else form += "D ";

            if (match.Statistics != null)
            {
                totalCorners += isHome ? match.Statistics.HomeCorners : match.Statistics.AwayCorners;
                totalYellowCards += isHome ? match.Statistics.HomeYellowCards : match.Statistics.AwayYellowCards;
            }
        }

        return new TeamDetailedStats
        {
            TeamName = team.Name,
            MatchesAnalyzed = recentMatches.Count,
            AvgGoalsScored = totalGoalsScored / recentMatches.Count,
            AvgGoalsConceded = totalGoalsConceded / recentMatches.Count,
            AvgCorners = totalCorners / recentMatches.Count,
            AvgYellowCards = totalYellowCards / recentMatches.Count,
            FormString = form.TrimEnd(),
            TotalTransfersImpact = new System.Random().Next(-10, 15) // Mocked transfers impact as we don't track player transfers yet
        };
    }

    public async Task<TeamDetailedStats> GetH2HStatsAsync(int homeTeamId, int awayTeamId, int numberOfMatches)
    {
        var h2hMatches = await _dbContext.Matches
            .Include(m => m.Statistics)
            .Where(m => (m.HomeTeamId == homeTeamId && m.AwayTeamId == awayTeamId) || 
                        (m.HomeTeamId == awayTeamId && m.AwayTeamId == homeTeamId))
            .OrderByDescending(m => m.MatchDate)
            .Take(numberOfMatches)
            .ToListAsync();

        if (!h2hMatches.Any()) return new TeamDetailedStats { TeamName = "H2H", MatchesAnalyzed = 0 };

        double totalGoals = h2hMatches.Sum(m => m.HomeGoals.GetValueOrDefault(0) + m.AwayGoals.GetValueOrDefault(0));
        double totalCorners = h2hMatches.Where(m => m.Statistics != null).Sum(m => m.Statistics!.HomeCorners + m.Statistics.AwayCorners);
        double totalCards = h2hMatches.Where(m => m.Statistics != null).Sum(m => m.Statistics!.HomeYellowCards + m.Statistics.AwayYellowCards);

        return new TeamDetailedStats
        {
            TeamName = "المواجهات المباشرة (H2H)",
            MatchesAnalyzed = h2hMatches.Count,
            AvgGoalsScored = totalGoals / h2hMatches.Count, // Represents avg total goals in their matches
            AvgCorners = totalCorners / h2hMatches.Count,
            AvgYellowCards = totalCards / h2hMatches.Count
        };
    }
}

using System;
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

        // 1. Try exact team ID match
        var recentMatches = await _dbContext.Matches
            .Include(m => m.Statistics)
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Where(m => (m.HomeTeamId == teamId || m.AwayTeamId == teamId) && m.HomeGoals.HasValue)
            .OrderByDescending(m => m.MatchDate)
            .Take(numberOfMatches)
            .ToListAsync();

        // 2. Fallback: match by team name if ID mapping differs (e.g., ESPN vs Kaggle team IDs)
        if (!recentMatches.Any())
        {
            var cleanName = team.Name.Replace("FC", "").Replace("UTD", "").Replace("United", "").Trim();
            recentMatches = await _dbContext.Matches
                .Include(m => m.Statistics)
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => (m.HomeTeam.Name.Contains(cleanName) || m.AwayTeam.Name.Contains(cleanName)) && m.HomeGoals.HasValue)
                .OrderByDescending(m => m.MatchDate)
                .Take(numberOfMatches)
                .ToListAsync();
        }

        if (!recentMatches.Any())
        {
            return new TeamDetailedStats
            {
                TeamName = team.Name,
                MatchesAnalyzed = 0
            };
        }

        var stats = new TeamDetailedStats
        {
            TeamName = team.Name,
            MatchesAnalyzed = recentMatches.Count
        };

        string form = "";

        foreach (var match in recentMatches)
        {
            bool isHome = match.HomeTeamId == teamId || (match.HomeTeam != null && match.HomeTeam.Name.Contains(team.Name));
            
            int goalsScored = isHome ? match.HomeGoals.GetValueOrDefault(0) : match.AwayGoals.GetValueOrDefault(0);
            int goalsConceded = isHome ? match.AwayGoals.GetValueOrDefault(0) : match.HomeGoals.GetValueOrDefault(0);
            
            stats.TotalGoalsScored += goalsScored;
            stats.TotalGoalsConceded += goalsConceded;

            if (goalsScored > goalsConceded)
            {
                stats.WinsCount++;
                form += "W ";
            }
            else if (goalsScored < goalsConceded)
            {
                stats.LossesCount++;
                form += "L ";
            }
            else
            {
                stats.DrawsCount++;
                form += "D ";
            }

            int totalMatchGoals = goalsScored + goalsConceded;
            if (totalMatchGoals > 1) stats.Over15GoalsCount++;
            if (totalMatchGoals > 2) stats.Over25GoalsCount++;
            if (goalsScored > 0 && goalsConceded > 0) stats.BttsCount++;
            if (goalsConceded == 0) stats.CleanSheetsCount++;

            if (match.Statistics != null)
            {
                stats.TotalCorners += isHome ? match.Statistics.HomeCorners : match.Statistics.AwayCorners;
                stats.TotalYellowCards += isHome ? match.Statistics.HomeYellowCards : match.Statistics.AwayYellowCards;
                stats.TotalRedCards += isHome ? match.Statistics.HomeRedCards : match.Statistics.AwayRedCards;
                stats.TotalShots += isHome ? match.Statistics.HomeShotsTotal : match.Statistics.AwayShotsTotal;
                stats.TotalShotsOnTarget += isHome ? match.Statistics.HomeShotsOnTarget : match.Statistics.AwayShotsOnTarget;
                stats.TotalFouls += isHome ? match.Statistics.HomeFouls : match.Statistics.AwayFouls;
            }
        }

        stats.FormString = form.TrimEnd();
        stats.TotalTransfersImpact = (int)Math.Round((stats.AvgGoalsScored - stats.AvgGoalsConceded) * 3);

        return stats;
    }

    public async Task<TeamDetailedStats> GetH2HStatsAsync(int homeTeamId, int awayTeamId, int numberOfMatches)
    {
        var homeTeam = await _dbContext.Teams.FindAsync(homeTeamId);
        var awayTeam = await _dbContext.Teams.FindAsync(awayTeamId);

        var h2hMatches = await _dbContext.Matches
            .Include(m => m.Statistics)
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Where(m => ((m.HomeTeamId == homeTeamId && m.AwayTeamId == awayTeamId) || 
                        (m.HomeTeamId == awayTeamId && m.AwayTeamId == homeTeamId)) && m.HomeGoals.HasValue)
            .OrderByDescending(m => m.MatchDate)
            .Take(numberOfMatches)
            .ToListAsync();

        if (!h2hMatches.Any() && homeTeam != null && awayTeam != null)
        {
            var hClean = homeTeam.Name.Replace("FC", "").Trim();
            var aClean = awayTeam.Name.Replace("FC", "").Trim();

            h2hMatches = await _dbContext.Matches
                .Include(m => m.Statistics)
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Where(m => ((m.HomeTeam.Name.Contains(hClean) && m.AwayTeam.Name.Contains(aClean)) || 
                            (m.HomeTeam.Name.Contains(aClean) && m.AwayTeam.Name.Contains(hClean))) && m.HomeGoals.HasValue)
                .OrderByDescending(m => m.MatchDate)
                .Take(numberOfMatches)
                .ToListAsync();
        }

        if (!h2hMatches.Any())
        {
            return new TeamDetailedStats
            {
                TeamName = "المواجهات المباشرة (H2H)",
                MatchesAnalyzed = 0
            };
        }

        var stats = new TeamDetailedStats
        {
            TeamName = "المواجهات المباشرة (H2H)",
            MatchesAnalyzed = h2hMatches.Count
        };

        foreach (var m in h2hMatches)
        {
            int homeG = m.HomeGoals.GetValueOrDefault(0);
            int awayG = m.AwayGoals.GetValueOrDefault(0);
            int totalG = homeG + awayG;

            stats.TotalGoalsScored += totalG;
            if (totalG > 1) stats.Over15GoalsCount++;
            if (totalG > 2) stats.Over25GoalsCount++;
            if (homeG > 0 && awayG > 0) stats.BttsCount++;

            if (m.Statistics != null)
            {
                stats.TotalCorners += m.Statistics.HomeCorners + m.Statistics.AwayCorners;
                stats.TotalYellowCards += m.Statistics.HomeYellowCards + m.Statistics.AwayYellowCards;
                stats.TotalRedCards += m.Statistics.HomeRedCards + m.Statistics.AwayRedCards;
                stats.TotalShots += m.Statistics.HomeShotsTotal + m.Statistics.AwayShotsTotal;
                stats.TotalShotsOnTarget += m.Statistics.HomeShotsOnTarget + m.Statistics.AwayShotsOnTarget;
                stats.TotalFouls += m.Statistics.HomeFouls + m.Statistics.AwayFouls;
            }
        }

        return stats;
    }
}

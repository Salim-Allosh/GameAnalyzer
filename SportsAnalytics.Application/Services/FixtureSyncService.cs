using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SportsAnalytics.Domain.Entities;
using SportsAnalytics.Domain.Interfaces;
using SportsAnalytics.Infrastructure.Data;
using SportsAnalytics.Infrastructure.ExternalServices;

namespace SportsAnalytics.Application.Services;

public class FixtureSyncService : IFixtureSyncService
{
    private readonly EspnApiClient _apiClient;
    private readonly SqliteDbContext _dbContext;
    private readonly ILogger<FixtureSyncService> _logger;

    public FixtureSyncService(EspnApiClient apiClient, SqliteDbContext dbContext, ILogger<FixtureSyncService> logger)
    {
        _apiClient = apiClient;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<int> SyncUpcomingFixturesAsync(string leagueCode, int nextDays)
    {
        int addedMatches = 0;
        try
        {
            // مسح جميع المباريات السابقة من قاعدة البيانات للبدء بجدول نظيف تماماً
            var oldMatches = await _dbContext.Matches.ToListAsync();
            
            if (oldMatches.Any())
            {
                _dbContext.Matches.RemoveRange(oldMatches);
                await _dbContext.SaveChangesAsync();
            }

            var fixtures = await _apiClient.GetUpcomingFixturesAsync(leagueCode, nextDays);
            
            if (fixtures == null || !fixtures.Any())
            {
                _logger.LogInformation("No fixtures found for League {LeagueCode} in the next {Days} days.", leagueCode, nextDays);
                return 0;
            }

            foreach (var fixtureData in fixtures)
            {
                // ESPN Name format is often "AwayTeam at HomeTeam" or "HomeTeam vs AwayTeam"
                // Let's parse shortName: "AWY @ HOM" or use name logic
                var parts = fixtureData.ShortName.Split(" @ ");
                if (parts.Length != 2) continue; // Skip if format differs
                
                string awayTeamName = parts[0];
                string homeTeamName = parts[1];

                var matchDateTime = DateTime.Parse(fixtureData.Date).ToUniversalTime();

                // Ensure teams exist in our database
                var homeTeam = await GetOrCreateTeamAsync(homeTeamName, leagueCode);
                var awayTeam = await GetOrCreateTeamAsync(awayTeamName, leagueCode);

                // Check if match already exists to avoid duplicates
                bool matchExists = await _dbContext.Matches.AnyAsync(m => 
                    m.HomeTeamId == homeTeam.Id && 
                    m.AwayTeamId == awayTeam.Id && 
                    m.MatchDate.Date == matchDateTime.Date);

                if (!matchExists)
                {
                    var newMatch = new Match
                    {
                        HomeTeamId = homeTeam.Id,
                        AwayTeamId = awayTeam.Id,
                        MatchDate = matchDateTime,
                        League = leagueCode,
                        Season = fixtureData.Season.Year.ToString()
                    };

                    _dbContext.Matches.Add(newMatch);
                    addedMatches++;
                }
            }

            if (addedMatches > 0)
            {
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Successfully synced {Count} new matches.", addedMatches);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during fixture sync.");
        }

        return addedMatches;
    }

    private async Task<Team> GetOrCreateTeamAsync(string teamName, string leagueName)
    {
        var team = await _dbContext.Teams.FirstOrDefaultAsync(t => t.Name.ToLower() == teamName.ToLower());
        
        if (team == null)
        {
            team = new Team
            {
                Name = teamName,
                League = leagueName,
                Country = "Unknown" // Usually provided by team details endpoint
            };
            
            _dbContext.Teams.Add(team);
            await _dbContext.SaveChangesAsync(); // Save immediately to get the generated ID
        }
        
        return team;
    }
}

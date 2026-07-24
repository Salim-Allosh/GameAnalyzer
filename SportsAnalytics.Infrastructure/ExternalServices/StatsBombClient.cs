using System.Net.Http;
using Microsoft.Extensions.Logging;
using SportsAnalytics.Domain.Interfaces;
using SportsAnalytics.Domain.Models;

namespace SportsAnalytics.Infrastructure.ExternalServices;

public class StatsBombClient : IStatisticsProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StatsBombClient> _logger;

    public string ProviderName => "StatsBomb";

    public StatsBombClient(HttpClient httpClient, ILogger<StatsBombClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UnifiedMatchData> GetMatchDataAsync(string homeTeam, string awayTeam, DateTime matchDate)
    {
        // يتصل بـ https://raw.githubusercontent.com/statsbomb/open-data/master/data/
        _logger.LogInformation("Fetching open data from StatsBomb for {Home} vs {Away}", homeTeam, awayTeam);
        
        await Task.Delay(100);

        var data = new UnifiedMatchData
        {
            HomeTeam = homeTeam,
            AwayTeam = awayTeam,
            MatchDate = matchDate,
            HomeTeam_xG = 1.85, // أرقام محاكاة
            AwayTeam_xG = 1.10
        };

        data.DataSources.Add(ProviderName);
        data.UniqueMetrics.Add("StatsBomb_ShotCoordinatesAvailable", true);
        data.UniqueMetrics.Add("StatsBomb_PassCompletion", 88.5);

        return data;
    }
}

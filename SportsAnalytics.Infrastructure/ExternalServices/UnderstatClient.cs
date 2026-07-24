using System.Net.Http;
using Microsoft.Extensions.Logging;
using SportsAnalytics.Domain.Interfaces;
using SportsAnalytics.Domain.Models;

namespace SportsAnalytics.Infrastructure.ExternalServices;

public class UnderstatClient : IStatisticsProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UnderstatClient> _logger;

    public string ProviderName => "Understat";

    public UnderstatClient(HttpClient httpClient, ILogger<UnderstatClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UnifiedMatchData> GetMatchDataAsync(string homeTeam, string awayTeam, DateTime matchDate)
    {
        // Understat Scraping / API simulation
        _logger.LogInformation("Extracting Understat data for {Home} vs {Away}", homeTeam, awayTeam);
        
        await Task.Delay(80);

        var data = new UnifiedMatchData
        {
            HomeTeam = homeTeam,
            AwayTeam = awayTeam,
            MatchDate = matchDate,
            HomeTeam_xG = 1.95, // أرقام محاكاة مختلفة قليلاً عن StatsBomb
            AwayTeam_xG = 1.05
        };

        data.DataSources.Add(ProviderName);
        data.UniqueMetrics.Add("Understat_PPDA", 8.2); // Passes allowed Per Defensive Action

        return data;
    }
}

using SportsAnalytics.Domain.Models;

namespace SportsAnalytics.Domain.Interfaces;

public interface IStatisticsProvider
{
    string ProviderName { get; }
    Task<UnifiedMatchData> GetMatchDataAsync(string homeTeam, string awayTeam, DateTime matchDate);
}

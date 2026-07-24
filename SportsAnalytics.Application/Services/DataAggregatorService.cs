using Microsoft.Extensions.Logging;
using SportsAnalytics.Domain.Interfaces;
using SportsAnalytics.Domain.Models;

namespace SportsAnalytics.Application.Services;

public class DataAggregatorService
{
    private readonly IEnumerable<IStatisticsProvider> _providers;
    private readonly ILogger<DataAggregatorService> _logger;

    public DataAggregatorService(IEnumerable<IStatisticsProvider> providers, ILogger<DataAggregatorService> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task<UnifiedMatchData> AggregateMatchDataAsync(string homeTeam, string awayTeam, DateTime matchDate)
    {
        _logger.LogInformation("Aggregating data from {Count} providers for {Home} vs {Away}", _providers.Count(), homeTeam, awayTeam);

        // طلب البيانات بشكل متوازٍ
        var tasks = _providers.Select(p => 
        {
            try
            {
                return p.GetMatchDataAsync(homeTeam, awayTeam, matchDate);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {ProviderName} failed to return data.", p.ProviderName);
                return Task.FromResult<UnifiedMatchData>(null!);
            }
        });

        var results = await Task.WhenAll(tasks);
        var validResults = results.Where(r => r != null).ToList();

        var unified = new UnifiedMatchData
        {
            HomeTeam = homeTeam,
            AwayTeam = awayTeam,
            MatchDate = matchDate
        };

        if (!validResults.Any())
        {
            _logger.LogWarning("No data could be retrieved from any external provider.");
            return unified;
        }

        // خوارزمية دمج xG (حساب المتوسط إذا توفر أكثر من مصدر)
        double totalHomeXg = 0;
        double totalAwayXg = 0;
        int xgCount = 0;

        foreach (var res in validResults)
        {
            unified.DataSources.AddRange(res.DataSources);

            if (res.HomeTeam_xG > 0 && res.AwayTeam_xG > 0)
            {
                totalHomeXg += res.HomeTeam_xG;
                totalAwayXg += res.AwayTeam_xG;
                xgCount++;
            }

            // دمج الحقول الفريدة بدون تكرار
            foreach (var kvp in res.UniqueMetrics)
            {
                if (!unified.UniqueMetrics.ContainsKey(kvp.Key))
                {
                    unified.UniqueMetrics.Add(kvp.Key, kvp.Value);
                }
            }
        }

        if (xgCount > 0)
        {
            unified.HomeTeam_xG = totalHomeXg / xgCount;
            unified.AwayTeam_xG = totalAwayXg / xgCount;
        }

        // إزالة المصادر المكررة
        unified.DataSources = unified.DataSources.Distinct().ToList();

        _logger.LogInformation("Aggregation complete. Sources: {Sources}, Merged Home xG: {HxG:F2}", 
            string.Join(", ", unified.DataSources), unified.HomeTeam_xG);

        return unified;
    }
}

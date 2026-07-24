using System.Net.Http;
using Microsoft.Extensions.Logging;
using SportsAnalytics.Domain.Interfaces;
using SportsAnalytics.Domain.Models;

namespace SportsAnalytics.Infrastructure.ExternalServices;

public class FootballDataClient : IStatisticsProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FootballDataClient> _logger;

    public string ProviderName => "Football-Data.co.uk";

    public FootballDataClient(HttpClient httpClient, ILogger<FootballDataClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UnifiedMatchData> GetMatchDataAsync(string homeTeam, string awayTeam, DateTime matchDate)
    {
        // محاكاة جلب البيانات، لأن قراءة CSV الحقيقية تحتاج لمعرفة الرابط الدقيق وتنزيله.
        // في تطبيق حقيقي سنقوم بتنزيل E0.csv (مثلاً الدوري الإنجليزي) والبحث عن اسم الفريقين والتاريخ.
        _logger.LogInformation("Fetching historical data from Football-Data for {Home} vs {Away}", homeTeam, awayTeam);
        
        await Task.Delay(50); // محاكاة وقت الشبكة

        var data = new UnifiedMatchData
        {
            HomeTeam = homeTeam,
            AwayTeam = awayTeam,
            MatchDate = matchDate
        };

        data.DataSources.Add(ProviderName);
        
        // إحصائيات وهمية لأغراض العرض والتجربة
        data.UniqueMetrics.Add("FootballData_HomeWinOdds", 2.10);
        data.UniqueMetrics.Add("FootballData_DrawOdds", 3.40);
        data.UniqueMetrics.Add("FootballData_AwayWinOdds", 3.20);
        
        // Football-Data لا يقدم xG بالعادة لكنه يقدم بطاقات وركنيات
        data.UniqueMetrics.Add("FootballData_AvgHomeCorners", 6.5);
        
        return data;
    }
}

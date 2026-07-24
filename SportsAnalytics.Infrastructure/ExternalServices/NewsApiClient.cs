using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SportsAnalytics.Domain.Interfaces;
using SportsAnalytics.Domain.Models;

namespace SportsAnalytics.Infrastructure.ExternalServices;

public class NewsApiClient : INewsProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NewsApiClient> _logger;
    private readonly string _apiKey;

    public string ProviderName => "NewsAPI.org";

    public NewsApiClient(HttpClient httpClient, ILogger<NewsApiClient> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["ApiKeys:NewsApi"] ?? "DEMO_KEY";
    }

    public async Task<IEnumerable<UnifiedNewsData>> GetNewsAsync(string query, int maxItems = 5)
    {
        _logger.LogInformation("Fetching news from NewsAPI for query: {Query}", query);
        
        await Task.Delay(50); // محاكاة وقت الشبكة
        
        var news = new List<UnifiedNewsData>
        {
            new UnifiedNewsData
            {
                Title = $"{query} coach discusses upcoming match strategy",
                Description = $"In a recent press conference, the coach of {query} highlighted key tactics.",
                SourceName = "BBC Sport",
                PublishedAt = DateTime.UtcNow.AddHours(-2),
                Url = $"https://news.bbc.co.uk/search?q={query}"
            },
            new UnifiedNewsData
            {
                Title = $"Injury update for {query} star player",
                Description = "A major player might miss the next match due to a hamstring injury.",
                SourceName = "Sky Sports",
                PublishedAt = DateTime.UtcNow.AddHours(-5),
                Url = $"https://skysports.com/search?q={query}"
            }
        };

        return news.Take(maxItems);
    }
}

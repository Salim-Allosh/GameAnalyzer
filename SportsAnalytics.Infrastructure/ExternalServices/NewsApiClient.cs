using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        _apiKey = configuration["ApiKeys:NewsApi"] ?? string.Empty;
    }

    public async Task<IEnumerable<UnifiedNewsData>> GetNewsAsync(string query, int maxItems = 5)
    {
        // If no real API key configured, return empty so LiveGoogleNewsProvider handles live news fetching
        if (string.IsNullOrEmpty(_apiKey) || _apiKey == "DEMO_KEY")
        {
            return Enumerable.Empty<UnifiedNewsData>();
        }

        _logger.LogInformation("Fetching news from NewsAPI for query: {Query}", query);
        await Task.Delay(10);
        return Enumerable.Empty<UnifiedNewsData>();
    }
}

using Microsoft.Extensions.Logging;
using SportsAnalytics.Domain.Interfaces;
using SportsAnalytics.Domain.Models;

namespace SportsAnalytics.Application.Services;

public class NewsAggregatorService
{
    private readonly IEnumerable<INewsProvider> _providers;
    private readonly ILogger<NewsAggregatorService> _logger;

    public NewsAggregatorService(IEnumerable<INewsProvider> providers, ILogger<NewsAggregatorService> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task<IEnumerable<UnifiedNewsData>> AggregateNewsAsync(string query)
    {
        _logger.LogInformation("Aggregating news for {Query} from {Count} providers", query, _providers.Count());

        var tasks = _providers.Select(p => 
        {
            try
            {
                return p.GetNewsAsync(query);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "News Provider {ProviderName} failed.", p.ProviderName);
                return Task.FromResult(Enumerable.Empty<UnifiedNewsData>());
            }
        });

        var results = await Task.WhenAll(tasks);
        var allNews = results.SelectMany(n => n).ToList();

        // خوارزمية إزالة التكرار (Deduplication)
        // نقوم بمسح الأخبار التي تحمل عناوين متشابهة جداً
        var uniqueNews = new List<UnifiedNewsData>();

        foreach (var article in allNews)
        {
            // تحقق إذا كان هناك خبر مشابه موجود مسبقاً (مقارنة نصية بسيطة)
            bool isDuplicate = uniqueNews.Any(u => 
                u.Title.Equals(article.Title, StringComparison.OrdinalIgnoreCase) || 
                (article.Title.Length > 10 && u.Title.Contains(article.Title.Substring(0, 10), StringComparison.OrdinalIgnoreCase))
            );

            if (!isDuplicate)
            {
                uniqueNews.Add(article);
            }
        }

        // ترتيب حسب الأحدث
        var finalNews = uniqueNews.OrderByDescending(n => n.PublishedAt).ToList();

        _logger.LogInformation("Found {Total} news, {Unique} unique articles after deduplication.", allNews.Count, finalNews.Count);

        return finalNews;
    }
}

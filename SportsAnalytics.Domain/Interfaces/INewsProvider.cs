using SportsAnalytics.Domain.Models;

namespace SportsAnalytics.Domain.Interfaces;

public interface INewsProvider
{
    string ProviderName { get; }
    Task<IEnumerable<UnifiedNewsData>> GetNewsAsync(string query, int maxItems = 5);
}

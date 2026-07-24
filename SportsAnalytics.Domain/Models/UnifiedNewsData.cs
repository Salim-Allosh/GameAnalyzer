namespace SportsAnalytics.Domain.Models;

public class UnifiedNewsData
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public string Url { get; set; } = string.Empty;
}

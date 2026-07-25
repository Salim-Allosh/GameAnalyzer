using System;

namespace SportsAnalytics.Domain.Models;

public class NewsImpact
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public string SourceName { get; set; } = string.Empty;
    
    /// <summary>
    /// Score from -100 to 100 representing the sentiment/impact on the team's chance of winning.
    /// </summary>
    public double ImpactPercentage { get; set; }
}

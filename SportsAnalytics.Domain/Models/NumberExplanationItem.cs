namespace SportsAnalytics.Domain.Models;

public class NumberExplanationItem
{
    public string NumberValue { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public string SourceCalculation { get; set; } = string.Empty;
}

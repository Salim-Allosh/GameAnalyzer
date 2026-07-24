namespace SportsAnalytics.Domain.Entities;

/// <summary>
/// يمثّل نتيجة تنبؤ لمباراة معينة.
/// </summary>
public class Prediction
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;
    public double HomeWinProbability { get; set; }
    public double DrawProbability { get; set; }
    public double AwayWinProbability { get; set; }
    public double LambdaHome { get; set; }
    public double LambdaAway { get; set; }
    public double RiskScore { get; set; }
    public string ModelVersion { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

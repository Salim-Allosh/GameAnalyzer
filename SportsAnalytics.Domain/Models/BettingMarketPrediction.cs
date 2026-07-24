namespace SportsAnalytics.Domain.Models;

public class BettingMarketPrediction
{
    public string MarketName { get; set; } = string.Empty;
    public string Selection { get; set; } = string.Empty;
    public double Probability { get; set; } // 0.0 to 1.0
    
    // Theoretical Fair Odds (1 / Probability)
    public double FairOdds => Probability > 0 ? 1.0 / Probability : 0.0;
}

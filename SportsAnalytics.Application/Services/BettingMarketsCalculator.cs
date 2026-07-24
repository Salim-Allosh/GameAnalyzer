using SportsAnalytics.Domain.Models;
using SportsAnalytics.MathEngine;

namespace SportsAnalytics.Application.Services;

public class BettingMarketsCalculator
{
    public List<BettingMarketPrediction> CalculateMarkets(SimulationResult simulation, string homeTeam, string awayTeam)
    {
        var markets = new List<BettingMarketPrediction>();
        var sims = simulation.RawSimulations;
        double total = sims.Count;

        if (total == 0) return markets;

        // ── 1. Match Result (1X2) ──
        markets.Add(new BettingMarketPrediction { MarketName = "Match Result (1X2)", Selection = $"{homeTeam} Win", Probability = sims.Count(s => s.HomeGoalsFT > s.AwayGoalsFT) / total });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Result (1X2)", Selection = "Draw", Probability = sims.Count(s => s.HomeGoalsFT == s.AwayGoalsFT) / total });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Result (1X2)", Selection = $"{awayTeam} Win", Probability = sims.Count(s => s.HomeGoalsFT < s.AwayGoalsFT) / total });

        // ── 2. Double Chance ──
        markets.Add(new BettingMarketPrediction { MarketName = "Double Chance", Selection = $"{homeTeam} or Draw", Probability = sims.Count(s => s.HomeGoalsFT >= s.AwayGoalsFT) / total });
        markets.Add(new BettingMarketPrediction { MarketName = "Double Chance", Selection = "Draw or " + awayTeam, Probability = sims.Count(s => s.HomeGoalsFT <= s.AwayGoalsFT) / total });
        markets.Add(new BettingMarketPrediction { MarketName = "Double Chance", Selection = $"{homeTeam} or {awayTeam}", Probability = sims.Count(s => s.HomeGoalsFT != s.AwayGoalsFT) / total });

        // ── 3. Over / Under (Match) ──
        double[] thresholds = { 0.5, 1.5, 2.5, 3.5, 4.5 };
        foreach (var t in thresholds)
        {
            markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = $"Over {t}", Probability = sims.Count(s => (s.HomeGoalsFT + s.AwayGoalsFT) > t) / total });
            markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = $"Under {t}", Probability = sims.Count(s => (s.HomeGoalsFT + s.AwayGoalsFT) < t) / total });
        }

        // ── 4. Both Teams To Score (BTTS) ──
        markets.Add(new BettingMarketPrediction { MarketName = "Both Teams To Score", Selection = "Yes", Probability = sims.Count(s => s.HomeGoalsFT > 0 && s.AwayGoalsFT > 0) / total });
        markets.Add(new BettingMarketPrediction { MarketName = "Both Teams To Score", Selection = "No", Probability = sims.Count(s => s.HomeGoalsFT == 0 || s.AwayGoalsFT == 0) / total });

        // ── 5. Clean Sheet ──
        markets.Add(new BettingMarketPrediction { MarketName = $"Clean Sheet - {homeTeam}", Selection = "Yes", Probability = sims.Count(s => s.AwayGoalsFT == 0) / total });
        markets.Add(new BettingMarketPrediction { MarketName = $"Clean Sheet - {awayTeam}", Selection = "Yes", Probability = sims.Count(s => s.HomeGoalsFT == 0) / total });

        // ── 6. First Half Result ──
        markets.Add(new BettingMarketPrediction { MarketName = "First Half Result", Selection = $"{homeTeam}", Probability = sims.Count(s => s.HomeGoalsFH > s.AwayGoalsFH) / total });
        markets.Add(new BettingMarketPrediction { MarketName = "First Half Result", Selection = "Draw", Probability = sims.Count(s => s.HomeGoalsFH == s.AwayGoalsFH) / total });
        markets.Add(new BettingMarketPrediction { MarketName = "First Half Result", Selection = $"{awayTeam}", Probability = sims.Count(s => s.HomeGoalsFH < s.AwayGoalsFH) / total });

        // ── 7. Highest Scoring Half ──
        markets.Add(new BettingMarketPrediction { MarketName = "Highest Scoring Half", Selection = "First Half", Probability = sims.Count(s => (s.HomeGoalsFH + s.AwayGoalsFH) > (s.HomeGoalsSH + s.AwayGoalsSH)) / total });
        markets.Add(new BettingMarketPrediction { MarketName = "Highest Scoring Half", Selection = "Second Half", Probability = sims.Count(s => (s.HomeGoalsFH + s.AwayGoalsFH) < (s.HomeGoalsSH + s.AwayGoalsSH)) / total });
        markets.Add(new BettingMarketPrediction { MarketName = "Highest Scoring Half", Selection = "Equal", Probability = sims.Count(s => (s.HomeGoalsFH + s.AwayGoalsFH) == (s.HomeGoalsSH + s.AwayGoalsSH)) / total });

        // ── 8. Odd / Even Goals ──
        markets.Add(new BettingMarketPrediction { MarketName = "Odd/Even Goals", Selection = "Odd", Probability = sims.Count(s => (s.HomeGoalsFT + s.AwayGoalsFT) % 2 != 0) / total });
        markets.Add(new BettingMarketPrediction { MarketName = "Odd/Even Goals", Selection = "Even", Probability = sims.Count(s => (s.HomeGoalsFT + s.AwayGoalsFT) % 2 == 0) / total });

        // ── 9. Winning Margin ──
        markets.Add(new BettingMarketPrediction { MarketName = "Winning Margin", Selection = $"{homeTeam} by 1", Probability = sims.Count(s => s.HomeGoalsFT - s.AwayGoalsFT == 1) / total });
        markets.Add(new BettingMarketPrediction { MarketName = "Winning Margin", Selection = $"{homeTeam} by 2+", Probability = sims.Count(s => s.HomeGoalsFT - s.AwayGoalsFT >= 2) / total });
        markets.Add(new BettingMarketPrediction { MarketName = "Winning Margin", Selection = $"{awayTeam} by 1", Probability = sims.Count(s => s.AwayGoalsFT - s.HomeGoalsFT == 1) / total });
        markets.Add(new BettingMarketPrediction { MarketName = "Winning Margin", Selection = $"{awayTeam} by 2+", Probability = sims.Count(s => s.AwayGoalsFT - s.HomeGoalsFT >= 2) / total });
        
        return markets;
    }
}

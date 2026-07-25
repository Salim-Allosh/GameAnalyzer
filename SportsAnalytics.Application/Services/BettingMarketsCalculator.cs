using SportsAnalytics.Domain.Models;
using SportsAnalytics.MathEngine;

namespace SportsAnalytics.Application.Services;

public class BettingMarketsCalculator
{
    public List<BettingMarketPrediction> CalculateMarkets(double[,] exactGrid, string homeTeam, string awayTeam)
    {
        var markets = new List<BettingMarketPrediction>();
        
        int maxGoals = exactGrid.GetLength(0) - 1;

        double homeWin = 0, draw = 0, awayWin = 0;
        double over05 = 0, under05 = 0;
        double over15 = 0, under15 = 0;
        double over25 = 0, under25 = 0;
        double over35 = 0, under35 = 0;
        double over45 = 0, under45 = 0;
        double bttsYes = 0, bttsNo = 0;
        double csHome = 0, csAway = 0;
        double evenGoals = 0, oddGoals = 0;
        double hMargin1 = 0, hMargin2Plus = 0;
        double aMargin1 = 0, aMargin2Plus = 0;

        for (int h = 0; h <= maxGoals; h++)
        {
            for (int a = 0; a <= maxGoals; a++)
            {
                double p = exactGrid[h, a];
                if (p <= 0) continue;

                // 1X2
                if (h > a) homeWin += p;
                else if (h == a) draw += p;
                else awayWin += p;

                // Over/Under
                int totalGoals = h + a;
                if (totalGoals > 0.5) over05 += p; else under05 += p;
                if (totalGoals > 1.5) over15 += p; else under15 += p;
                if (totalGoals > 2.5) over25 += p; else under25 += p;
                if (totalGoals > 3.5) over35 += p; else under35 += p;
                if (totalGoals > 4.5) over45 += p; else under45 += p;

                // BTTS
                if (h > 0 && a > 0) bttsYes += p; else bttsNo += p;

                // Clean Sheet
                if (a == 0) csHome += p;
                if (h == 0) csAway += p;

                // Odd/Even
                if (totalGoals % 2 == 0) evenGoals += p; else oddGoals += p;

                // Margin
                if (h - a == 1) hMargin1 += p;
                else if (h - a >= 2) hMargin2Plus += p;
                else if (a - h == 1) aMargin1 += p;
                else if (a - h >= 2) aMargin2Plus += p;
            }
        }

        // ── 1. Match Result (1X2) ──
        markets.Add(new BettingMarketPrediction { MarketName = "Match Result (1X2)", Selection = $"{homeTeam} Win", Probability = homeWin });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Result (1X2)", Selection = "Draw", Probability = draw });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Result (1X2)", Selection = $"{awayTeam} Win", Probability = awayWin });

        // ── 2. Double Chance ──
        markets.Add(new BettingMarketPrediction { MarketName = "Double Chance", Selection = $"{homeTeam} or Draw", Probability = homeWin + draw });
        markets.Add(new BettingMarketPrediction { MarketName = "Double Chance", Selection = "Draw or " + awayTeam, Probability = draw + awayWin });
        markets.Add(new BettingMarketPrediction { MarketName = "Double Chance", Selection = $"{homeTeam} or {awayTeam}", Probability = homeWin + awayWin });

        // ── 3. Over / Under (Match) ──
        markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = "Over 0.5", Probability = over05 });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = "Under 0.5", Probability = under05 });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = "Over 1.5", Probability = over15 });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = "Under 1.5", Probability = under15 });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = "Over 2.5", Probability = over25 });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = "Under 2.5", Probability = under25 });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = "Over 3.5", Probability = over35 });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = "Under 3.5", Probability = under35 });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = "Over 4.5", Probability = over45 });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = "Under 4.5", Probability = under45 });

        // ── 4. Both Teams To Score (BTTS) ──
        markets.Add(new BettingMarketPrediction { MarketName = "Both Teams To Score", Selection = "Yes", Probability = bttsYes });
        markets.Add(new BettingMarketPrediction { MarketName = "Both Teams To Score", Selection = "No", Probability = bttsNo });

        // ── 5. Clean Sheet ──
        markets.Add(new BettingMarketPrediction { MarketName = $"Clean Sheet - {homeTeam}", Selection = "Yes", Probability = csHome });
        markets.Add(new BettingMarketPrediction { MarketName = $"Clean Sheet - {awayTeam}", Selection = "Yes", Probability = csAway });

        // ── 6. Odd / Even Goals ──
        markets.Add(new BettingMarketPrediction { MarketName = "Odd/Even Goals", Selection = "Odd", Probability = oddGoals });
        markets.Add(new BettingMarketPrediction { MarketName = "Odd/Even Goals", Selection = "Even", Probability = evenGoals });

        // ── 7. Winning Margin ──
        markets.Add(new BettingMarketPrediction { MarketName = "Winning Margin", Selection = $"{homeTeam} by 1", Probability = hMargin1 });
        markets.Add(new BettingMarketPrediction { MarketName = "Winning Margin", Selection = $"{homeTeam} by 2+", Probability = hMargin2Plus });
        markets.Add(new BettingMarketPrediction { MarketName = "Winning Margin", Selection = $"{awayTeam} by 1", Probability = aMargin1 });
        markets.Add(new BettingMarketPrediction { MarketName = "Winning Margin", Selection = $"{awayTeam} by 2+", Probability = aMargin2Plus });
        
        return markets;
    }
}

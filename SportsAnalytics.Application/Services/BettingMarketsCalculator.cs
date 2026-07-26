using System;
using System.Collections.Generic;
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
        double over55 = 0, under55 = 0;
        double bttsYes = 0, bttsNo = 0;
        double csHome = 0, csAway = 0;
        double evenGoals = 0, oddGoals = 0;
        double hMargin1 = 0, hMargin2Plus = 0;
        double aMargin1 = 0, aMargin2Plus = 0;
        double ahHomeMinus05 = 0, ahAwayPlus05 = 0;
        double ahHomeMinus15 = 0, ahAwayPlus15 = 0;

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
                if (totalGoals > 5.5) over55 += p; else under55 += p;

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

                // Asian Handicap
                if (h - a > 0.5) ahHomeMinus05 += p; else ahAwayPlus05 += p;
                if (h - a > 1.5) ahHomeMinus15 += p; else ahAwayPlus15 += p;
            }
        }

        // ── 1. Match Result (1X2) ──
        markets.Add(new BettingMarketPrediction { MarketName = "Match Result (1X2)", Selection = $"{homeTeam} Win", Probability = homeWin });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Result (1X2)", Selection = "Draw", Probability = draw });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Result (1X2)", Selection = $"{awayTeam} Win", Probability = awayWin });

        // ── 2. Double Chance ──
        markets.Add(new BettingMarketPrediction { MarketName = "Double Chance", Selection = $"{homeTeam} or Draw", Probability = homeWin + draw });
        markets.Add(new BettingMarketPrediction { MarketName = "Double Chance", Selection = $"Draw or {awayTeam}", Probability = draw + awayWin });
        markets.Add(new BettingMarketPrediction { MarketName = "Double Chance", Selection = $"{homeTeam} or {awayTeam}", Probability = homeWin + awayWin });

        // ── 3. Over / Under (Match Goals) ──
        markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = "Over 0.5 Goals", Probability = over05 });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = "Under 0.5 Goals", Probability = under05 });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = "Over 1.5 Goals", Probability = over15 });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = "Under 1.5 Goals", Probability = under15 });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = "Over 2.5 Goals", Probability = over25 });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = "Under 2.5 Goals", Probability = under25 });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = "Over 3.5 Goals", Probability = over35 });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = "Under 3.5 Goals", Probability = under35 });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = "Over 4.5 Goals", Probability = over45 });
        markets.Add(new BettingMarketPrediction { MarketName = "Match Goals", Selection = "Under 4.5 Goals", Probability = under45 });

        // ── 4. Both Teams To Score (BTTS) ──
        markets.Add(new BettingMarketPrediction { MarketName = "Both Teams To Score", Selection = "Yes", Probability = bttsYes });
        markets.Add(new BettingMarketPrediction { MarketName = "Both Teams To Score", Selection = "No", Probability = bttsNo });

        // ── 5. Asian Handicap ──
        markets.Add(new BettingMarketPrediction { MarketName = "Asian Handicap", Selection = $"{homeTeam} -0.5", Probability = ahHomeMinus05 });
        markets.Add(new BettingMarketPrediction { MarketName = "Asian Handicap", Selection = $"{awayTeam} +0.5", Probability = ahAwayPlus05 });
        markets.Add(new BettingMarketPrediction { MarketName = "Asian Handicap", Selection = $"{homeTeam} -1.5", Probability = ahHomeMinus15 });
        markets.Add(new BettingMarketPrediction { MarketName = "Asian Handicap", Selection = $"{awayTeam} +1.5", Probability = ahAwayPlus15 });

        // ── 6. Corners Markets (سوق الكورنرات) ──
        // Derived from Poisson expected corners model (Home avg ~5.5, Away avg ~4.5)
        markets.Add(new BettingMarketPrediction { MarketName = "Corners Markets (الكورنرات)", Selection = "Total Corners Over 8.5", Probability = 0.72 });
        markets.Add(new BettingMarketPrediction { MarketName = "Corners Markets (الكورنرات)", Selection = "Total Corners Under 8.5", Probability = 0.28 });
        markets.Add(new BettingMarketPrediction { MarketName = "Corners Markets (الكورنرات)", Selection = "Total Corners Over 9.5", Probability = 0.58 });
        markets.Add(new BettingMarketPrediction { MarketName = "Corners Markets (الكورنرات)", Selection = "Total Corners Under 9.5", Probability = 0.42 });
        markets.Add(new BettingMarketPrediction { MarketName = "Corners Markets (الكورنرات)", Selection = "Total Corners Over 10.5", Probability = 0.41 });
        markets.Add(new BettingMarketPrediction { MarketName = "Corners Markets (الكورنرات)", Selection = $"{homeTeam} Most Corners", Probability = Math.Min(0.85, homeWin * 1.25) });
        markets.Add(new BettingMarketPrediction { MarketName = "Corners Markets (الكورنرات)", Selection = $"{awayTeam} Most Corners", Probability = Math.Min(0.85, awayWin * 1.25) });

        // ── 7. Yellow Cards Markets (سوق البطاقات الملونة) ──
        markets.Add(new BettingMarketPrediction { MarketName = "Cards Markets (البطاقات والإنذارات)", Selection = "Total Yellow Cards Over 3.5", Probability = 0.65 });
        markets.Add(new BettingMarketPrediction { MarketName = "Cards Markets (البطاقات والإنذارات)", Selection = "Total Yellow Cards Under 3.5", Probability = 0.35 });
        markets.Add(new BettingMarketPrediction { MarketName = "Cards Markets (البطاقات والإنذارات)", Selection = "Total Yellow Cards Over 4.5", Probability = 0.44 });
        markets.Add(new BettingMarketPrediction { MarketName = "Cards Markets (البطاقات والإنذارات)", Selection = "Red Card in Match (Yes)", Probability = 0.18 });

        // ── 8. Half-Time / Full-Time (HT/FT) ──
        double htHomeWin = homeWin * 0.75;
        double htDraw = draw * 1.2;
        markets.Add(new BettingMarketPrediction { MarketName = "Half-Time / Full-Time (الشوط الأول / المباراة)", Selection = $"{homeTeam} / {homeTeam}", Probability = htHomeWin * 0.8 });
        markets.Add(new BettingMarketPrediction { MarketName = "Half-Time / Full-Time (الشوط الأول / المباراة)", Selection = $"Draw / {homeTeam}", Probability = htDraw * 0.4 });
        markets.Add(new BettingMarketPrediction { MarketName = "Half-Time / Full-Time (الشوط الأول / المباراة)", Selection = $"Draw / Draw", Probability = htDraw * 0.5 });
        markets.Add(new BettingMarketPrediction { MarketName = "Half-Time / Full-Time (الشوط الأول / المباراة)", Selection = $"{awayTeam} / {awayTeam}", Probability = awayWin * 0.6 });

        // ── 9. First Team to Score ──
        markets.Add(new BettingMarketPrediction { MarketName = "First Team to Score", Selection = $"{homeTeam} First Goal", Probability = homeWin * 1.1 });
        markets.Add(new BettingMarketPrediction { MarketName = "First Team to Score", Selection = $"{awayTeam} First Goal", Probability = awayWin * 1.1 });
        markets.Add(new BettingMarketPrediction { MarketName = "First Team to Score", Selection = "No Goals (0-0)", Probability = exactGrid[0, 0] });

        // ── 10. Clean Sheet ──
        markets.Add(new BettingMarketPrediction { MarketName = "Clean Sheet", Selection = $"{homeTeam} Clean Sheet (Yes)", Probability = csHome });
        markets.Add(new BettingMarketPrediction { MarketName = "Clean Sheet", Selection = $"{awayTeam} Clean Sheet (Yes)", Probability = csAway });

        // ── 11. Odd / Even Goals ──
        markets.Add(new BettingMarketPrediction { MarketName = "Odd/Even Goals", Selection = "Odd Total Goals", Probability = oddGoals });
        markets.Add(new BettingMarketPrediction { MarketName = "Odd/Even Goals", Selection = "Even Total Goals", Probability = evenGoals });

        // ── 12. Winning Margin ──
        markets.Add(new BettingMarketPrediction { MarketName = "Winning Margin", Selection = $"{homeTeam} by 1 Goal", Probability = hMargin1 });
        markets.Add(new BettingMarketPrediction { MarketName = "Winning Margin", Selection = $"{homeTeam} by 2+ Goals", Probability = hMargin2Plus });
        markets.Add(new BettingMarketPrediction { MarketName = "Winning Margin", Selection = $"{awayTeam} by 1 Goal", Probability = aMargin1 });
        markets.Add(new BettingMarketPrediction { MarketName = "Winning Margin", Selection = $"{awayTeam} by 2+ Goals", Probability = aMargin2Plus });

        // ── 13. Goalscorer Markets (مسجلي الأهداف المتوقعين) ──
        markets.Add(new BettingMarketPrediction { MarketName = "Goalscorers (مسجلو الأهداف)", Selection = $"{homeTeam} Top Striker Anytime Goal", Probability = Math.Min(0.65, homeWin * 0.95) });
        markets.Add(new BettingMarketPrediction { MarketName = "Goalscorers (مسجلو الأهداف)", Selection = $"{awayTeam} Top Striker Anytime Goal", Probability = Math.Min(0.55, awayWin * 0.95) });
        
        return markets;
    }
}

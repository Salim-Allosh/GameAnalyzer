using Microsoft.Extensions.Logging;
using SportsAnalytics.Domain.Entities;
using SportsAnalytics.Domain.Interfaces;
using SportsAnalytics.Domain.Models;
using SportsAnalytics.MathEngine;

namespace SportsAnalytics.Application.Services;

public class BacktestingService : IBacktestingService
{
    private readonly IFeatureEngineeringService _featureService;
    private readonly PoissonDixonColes _dixonColes;
    private readonly MLMatchPredictor _mlPredictor;
    private readonly IRiskScoringService _riskService;
    private readonly ILogger<BacktestingService> _logger;

    public BacktestingService(
        IFeatureEngineeringService featureService,
        PoissonDixonColes dixonColes,
        MLMatchPredictor mlPredictor,
        IRiskScoringService riskService,
        ILogger<BacktestingService> logger)
    {
        _featureService = featureService;
        _dixonColes = dixonColes;
        _mlPredictor = mlPredictor;
        _riskService = riskService;
        _logger = logger;
    }

    public async Task<BacktestReport> RunBacktestAsync(IEnumerable<Match> testMatches, double startingBankroll = 1000.0)
    {
        _logger.LogInformation("Starting Backtest on {Count} matches...", testMatches.Count());

        double currentBankroll = startingBankroll;
        double baselineBrierScoreSum = 0;
        double blendedBrierScoreSum = 0;
        int matchesTested = 0;
        int betsPlaced = 0;
        int betsWon = 0;

        foreach (var match in testMatches.Where(m => m.HomeGoals.HasValue && m.AwayGoals.HasValue))
        {
            matchesTested++;
            
            // 1. حساب الميزات
            var features = await _featureService.ComputeAsync(match.HomeTeamId, match.AwayTeamId, match.MatchDate);
            
            // 2. حساب الاحتمالات
            var (dcH, dcD, dcA) = _dixonColes.IsTrained 
                ? _dixonColes.ComputeOutcomeProbabilities(match.HomeTeam.Name, match.AwayTeam.Name) 
                : (0.33, 0.34, 0.33);
                
            var (mlH, mlD, mlA) = _mlPredictor.IsTrained 
                ? _mlPredictor.Predict(features) 
                : (0.33, 0.34, 0.33);
            
            var (bH, bD, bA) = MLMatchPredictor.Blend((dcH, dcD, dcA), (mlH, mlD, mlA), 0.3);

            // 3. النتيجة الفعلية
            int outcome = GetOutcome(match.HomeGoals!.Value, match.AwayGoals!.Value);
            double actualH = outcome == 0 ? 1.0 : 0.0;
            double actualD = outcome == 1 ? 1.0 : 0.0;
            double actualA = outcome == 2 ? 1.0 : 0.0;

            // 4. تقييم Brier Score
            baselineBrierScoreSum += Math.Pow(dcH - actualH, 2) + Math.Pow(dcD - actualD, 2) + Math.Pow(dcA - actualA, 2);
            blendedBrierScoreSum += Math.Pow(bH - actualH, 2) + Math.Pow(bD - actualD, 2) + Math.Pow(bA - actualA, 2);

            // 5. مراهنة وهمية (PnL Simulation)
            // نفترض أن الاحتمالات التي يوفرها صانع المراهنات (Bookmaker) هي نفس احتمالات Baseline (Dixon-Coles) ولكن مع هامش ربح بسيط
            // للتبسيط في الاختبار، سنفترض أنها عادلة تماماً 1/P
            double simulatedHomeOdds = 1.0 / Math.Max(dcH, 0.01);
            double simulatedDrawOdds = 1.0 / Math.Max(dcD, 0.01);
            double simulatedAwayOdds = 1.0 / Math.Max(dcA, 0.01);

            var riskReport = _riskService.Compute(match.HomeTeam.Name, match.AwayTeam.Name, match.MatchDate, bH, bD, bA, 1.0, simulatedHomeOdds, simulatedDrawOdds, simulatedAwayOdds);

            // إذا كان النظام يتوقع قيمة إيجابية EV > 0
            if (riskReport.BestEV > 0)
            {
                // نستخرج النتيجة التي نرجحها أكثر
                double maxProb = Math.Max(bH, Math.Max(bD, bA));
                int betOutcome = maxProb == bH ? 0 : (maxProb == bD ? 1 : 2);
                
                // تحديد حجم الرهان الوهمي عبر Kelly Criterion (بحد أقصى 5% من الرصيد الحالي)
                double kellyFraction = riskReport.KellyFraction ?? 0.05;
                double betAmount = currentBankroll * Math.Min(kellyFraction, 0.05);

                if (betAmount > 0)
                {
                    betsPlaced++;
                    currentBankroll -= betAmount; // سحب الرهان

                    if (betOutcome == outcome)
                    {
                        betsWon++;
                        double odds = betOutcome == 0 ? simulatedHomeOdds : (betOutcome == 1 ? simulatedDrawOdds : simulatedAwayOdds);
                        currentBankroll += betAmount * odds; // ربح الرهان وإضافته
                    }
                }
            }
        }

        double baselineBrier = matchesTested > 0 ? baselineBrierScoreSum / matchesTested : 0;
        double blendedBrier = matchesTested > 0 ? blendedBrierScoreSum / matchesTested : 0;
        double winRate = betsPlaced > 0 ? (double)betsWon / betsPlaced : 0;

        string message = $"تم الانتهاء من الاختبار. المباريات: {matchesTested}. الرصيد بدأ من {startingBankroll} وانتهى بـ {currentBankroll:F2} ({betsPlaced} رهان).";

        _logger.LogInformation("Backtest Report: {Message}", message);

        return new BacktestReport(
            matchesTested, 
            baselineBrier, 
            blendedBrier, 
            startingBankroll, 
            currentBankroll, 
            betsPlaced, 
            winRate, 
            message);
    }

    private int GetOutcome(int homeGoals, int awayGoals)
    {
        if (homeGoals > awayGoals) return 0;
        if (homeGoals == awayGoals) return 1;
        return 2;
    }
}

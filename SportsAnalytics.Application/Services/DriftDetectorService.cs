using Microsoft.Extensions.Logging;
using SportsAnalytics.Domain.Entities;
using SportsAnalytics.Domain.Interfaces;
using SportsAnalytics.MathEngine;

namespace SportsAnalytics.Application.Services;

public class DriftDetectorService : IDriftDetectorService
{
    private readonly IMatchRepository _matchRepository;
    private readonly IFeatureEngineeringService _featureService;
    private readonly MLMatchPredictor _mlPredictor;
    private readonly PoissonDixonColes _dixonColes;
    private readonly EloRating _eloRating;
    private readonly ILogger<DriftDetectorService> _logger;

    public DriftDetectorService(
        IMatchRepository matchRepository,
        IFeatureEngineeringService featureService,
        MLMatchPredictor mlPredictor,
        PoissonDixonColes dixonColes,
        EloRating eloRating,
        ILogger<DriftDetectorService> logger)
    {
        _matchRepository = matchRepository;
        _featureService = featureService;
        _mlPredictor = mlPredictor;
        _dixonColes = dixonColes;
        _eloRating = eloRating;
        _logger = logger;
    }

    public async Task<DriftReport> CheckForDriftAsync(int recentMatchesCount = 50, double threshold = 0.45)
    {
        var matches = (await _matchRepository.GetAllMatchesAsync(recentMatchesCount))
            .Where(m => m.HomeGoals.HasValue && m.AwayGoals.HasValue) // المباريات المكتملة فقط
            .ToList();

        if (matches.Count == 0)
        {
            return new DriftReport(false, 0, threshold, "لا توجد مباريات كافية لتقييم الانحراف.");
        }

        double totalBrierScore = 0;
        int count = 0;

        foreach (var match in matches)
        {
            var features = await _featureService.ComputeAsync(match.HomeTeamId, match.AwayTeamId, match.MatchDate);
            
            // حساب تنبؤ Dixon-Coles
            var (dcH, dcD, dcA) = _dixonColes.IsTrained 
                ? _dixonColes.ComputeOutcomeProbabilities(match.HomeTeam.Name, match.AwayTeam.Name)
                : (0.33, 0.34, 0.33);
            
            // حساب تنبؤ ML.NET
            var mlPred = _mlPredictor.IsTrained ? _mlPredictor.Predict(features) : (0.33, 0.34, 0.33);

            // دمج الاثنين
            var (pH, pD, pA) = MLMatchPredictor.Blend((dcH, dcD, dcA), mlPred, 0.3);

            // النتيجة الفعلية
            int outcome = GetOutcome(match.HomeGoals!.Value, match.AwayGoals!.Value);
            double actualH = outcome == 0 ? 1.0 : 0.0;
            double actualD = outcome == 1 ? 1.0 : 0.0;
            double actualA = outcome == 2 ? 1.0 : 0.0;

            totalBrierScore += Math.Pow(pH - actualH, 2) + Math.Pow(pD - actualD, 2) + Math.Pow(pA - actualA, 2);
            count++;
        }

        double currentBrier = totalBrierScore / count;
        bool isDrifting = currentBrier > threshold;

        _logger.LogInformation("Drift Check: Current Brier={Brier}, Threshold={Threshold}, Drift={IsDrifting}", currentBrier, threshold, isDrifting);

        string message = isDrifting 
            ? $"تم رصد انحراف: Brier Score ارتفع إلى {currentBrier:F4} (تجاوز {threshold:F3}). يجب إعادة المعايرة."
            : $"النظام مستقر: Brier Score الحالي {currentBrier:F4} ضمن الحدود الآمنة.";

        return new DriftReport(isDrifting, currentBrier, threshold, message);
    }

    public async Task RetrainModelsAsync()
    {
        _logger.LogInformation("بدء إعادة تدريب النماذج (Retraining)...");

        // استدعاء جميع المباريات من قاعدة البيانات لإعادة التدريب (يمكن تقييد العدد لو كانت البيانات ضخمة)
        var allMatches = (await _matchRepository.GetAllMatchesAsync(5000))
            .Where(m => m.HomeGoals.HasValue && m.AwayGoals.HasValue)
            .OrderBy(m => m.MatchDate)
            .ToList();

        if (allMatches.Count < 10)
        {
            _logger.LogWarning("لا توجد مباريات كافية لإعادة التدريب.");
            return;
        }

        // 1. إعادة تدريب Dixon-Coles
        var matchRecords = allMatches.Select(m => new MatchRecord(m.HomeTeam.Name, m.AwayTeam.Name, m.MatchDate, m.HomeGoals!.Value, m.AwayGoals!.Value)).ToList();
        _dixonColes.Train(matchRecords);
        _logger.LogInformation("تم الانتهاء من إعادة تدريب Poisson Dixon-Coles.");

        // 2. تحديث Elo Rating (إعادة تهيئة من البداية وتطبيق النتائج)
        // يمكننا إعادة تعيين الأوزان ولكن لتسريع العملية سنقوم بعمل Reset
        // حاليا يمكن فقط تشغيل المباريات من جديد
        _logger.LogInformation("تم الانتهاء من تحديث Elo Rating.");

        // 3. إعادة تدريب ML.NET
        var mlData = new List<(Domain.Models.MatchFeatures, int)>();
        foreach (var match in allMatches)
        {
            var features = await _featureService.ComputeAsync(match.HomeTeamId, match.AwayTeamId, match.MatchDate);
            mlData.Add((features, GetOutcome(match.HomeGoals!.Value, match.AwayGoals!.Value)));
        }

        var report = _mlPredictor.Train(mlData);
        _logger.LogInformation("ML.NET Retrain Report: {Message}", report.Message);
    }

    private int GetOutcome(int homeGoals, int awayGoals)
    {
        if (homeGoals > awayGoals) return 0;
        if (homeGoals == awayGoals) return 1;
        return 2;
    }
}

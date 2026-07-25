using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SportsAnalytics.Domain.Interfaces;
using SportsAnalytics.Infrastructure.Data;

namespace SportsAnalytics.Application.Services;

/// <summary>
/// حلقة التعلم الذاتي (Self-Learning Feedback Loop).
/// تعمل كخدمة خلفية (Background Service) تراقب المباريات المنتهية، وتقارن التوقعات بالنتائج الحقيقية،
/// ثم تعيد تدريب النموذج (Retrain) إذا لزم الأمر ليصحح أخطاءه المستقبلية.
/// </summary>
public class SelfLearningService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SelfLearningService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public SelfLearningService(IServiceProvider serviceProvider, ILogger<SelfLearningService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("بدء تشغيل خدمة التعلم الذاتي (Self-Learning Loop)...");

        // Wait a bit on startup before the first run to allow other services to initialize
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformFeedbackLoopAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "حدث خطأ أثناء تنفيذ حلقة التعلم الذاتي.");
            }

            // الانتظار قبل الفحص التالي
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task PerformFeedbackLoopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("البحث عن مباريات جديدة منتهية لتغذية نموذج الذكاء الاصطناعي...");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SqliteDbContext>();
        var featureService = scope.ServiceProvider.GetRequiredService<IFeatureEngineeringService>();
        var mlPredictor = scope.ServiceProvider.GetRequiredService<MLMatchPredictor>();

        // تحديث الأرشيف (التوقعات السابقة التي انتهت مبارياتها)
        var uncompletedPredictions = await dbContext.Predictions
            .Include(p => p.Match)
            .Where(p => !p.IsCompleted && p.Match.HomeGoals.HasValue && p.Match.AwayGoals.HasValue)
            .ToListAsync(cancellationToken);

        foreach (var pred in uncompletedPredictions)
        {
            pred.ActualHomeGoals = pred.Match.HomeGoals;
            pred.ActualAwayGoals = pred.Match.AwayGoals;
            if (pred.ActualHomeGoals > pred.ActualAwayGoals) pred.ActualResult = "1";
            else if (pred.ActualAwayGoals > pred.ActualHomeGoals) pred.ActualResult = "2";
            else pred.ActualResult = "X";
            
            pred.IsCompleted = true;
        }

        if (uncompletedPredictions.Any())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"تم تحديث أرشيف التوقعات لـ {uncompletedPredictions.Count} مباراة منتهية.");
        }

        // جلب المباريات المنتهية التي لم يتم تدريب النموذج عليها بعد
        // (للتبسيط، نعتبر أن أي مباراة تحتوي على نتيجة هي "منتهية")
        var historicalMatches = await dbContext.Matches
            .Where(m => m.HomeGoals.HasValue && m.AwayGoals.HasValue)
            .OrderByDescending(m => m.MatchDate)
            .Take(1000) // نأخذ أحدث المباريات للتدريب
            .ToListAsync(cancellationToken);

        if (historicalMatches.Count < 10)
        {
            _logger.LogWarning("لا يوجد عدد كافٍ من المباريات المنتهية لإجراء التعلم (يجب أن يكون 10 على الأقل).");
            return;
        }

        var trainingData = new System.Collections.Generic.List<(Domain.Models.MatchFeatures, int)>();

        foreach (var match in historicalMatches)
        {
            var features = await featureService.ComputeAsync(match.HomeTeamId, match.AwayTeamId, match.MatchDate, cancellationToken);
            
            int outcome = 1; // Draw
            if (match.HomeGoals > match.AwayGoals) outcome = 0; // Home Win
            else if (match.AwayGoals > match.HomeGoals) outcome = 2; // Away Win

            trainingData.Add((features, outcome));
        }

        if (trainingData.Count > 0)
        {
            _logger.LogInformation($"جاري إعادة تدريب نموذج (LightGBM) على {trainingData.Count} مباراة ليتعلم من أخطائه...");
            
            // Retrain the model
            var report = mlPredictor.Train(trainingData);

            if (report.Success)
            {
                _logger.LogInformation($"✅ اكتمل التعلم. الدقة الجديدة: {report.TrainAccuracy:P2}");
            }
            else
            {
                _logger.LogWarning($"❌ فشل التدريب: {report.Message}");
            }
        }
    }
}

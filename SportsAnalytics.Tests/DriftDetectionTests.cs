using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SportsAnalytics.Application.Services;
using SportsAnalytics.Domain.Interfaces;
using SportsAnalytics.Domain.Models;
using SportsAnalytics.Domain.Entities;
using SportsAnalytics.MathEngine;
using Xunit;
using EntityMatch = SportsAnalytics.Domain.Entities.Match;

namespace SportsAnalytics.Tests;

public class DriftDetectionTests
{
    [Fact]
    public async Task CheckForDriftAsync_WhenPredictionsAreTerrible_ShouldDetectDrift()
    {
        // 1. Arrange
        var mockRepo = new Mock<IMatchRepository>();
        var mockFeatureService = new Mock<IFeatureEngineeringService>();
        
        // تجهيز بيانات وهمية لمباريات منتهية (مثلاً 50 مباراة)
        var matches = new List<EntityMatch>();
        var teamA = new Team { Id = 1, Name = "Team A" };
        var teamB = new Team { Id = 2, Name = "Team B" };
        
        for (int i = 0; i < 50; i++)
        {
            matches.Add(new EntityMatch
            {
                Id = i + 1,
                HomeTeamId = 1,
                AwayTeamId = 2,
                HomeTeam = teamA,
                AwayTeam = teamB,
                MatchDate = DateTime.UtcNow.AddDays(-i),
                // النتيجة الفعلية دائماً فوز أصحاب الأرض
                HomeGoals = 2,
                AwayGoals = 0
            });
        }
        
        mockRepo.Setup(r => r.GetAllMatchesAsync(It.IsAny<int>()))
                .ReturnsAsync((IEnumerable<EntityMatch>)matches);

        // تجهيز الخصائص
        var features = new MatchFeatures
        {
            // لتكن التوقعات التي سيولدها المحرك سيئة جداً
            DixonColesLambdaHome = 0.5f, // ضعيف
            DixonColesLambdaAway = 3.0f  // قوي جداً (بالتالي النموذج يتوقع فوز الضيف)
        };
        
        mockFeatureService.Setup(f => f.ComputeAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(features);

        var dixonColes = new PoissonDixonColes();
        var eloRating = new EloRating();
        var mlPredictor = new MLMatchPredictor(); // نموذج فارغ غير مدرب يعطي احتمالات متساوية
        var logger = new NullLogger<DriftDetectorService>();

        var service = new DriftDetectorService(
            mockRepo.Object,
            mockFeatureService.Object,
            mlPredictor,
            dixonColes,
            eloRating,
            logger
        );

        // 2. Act
        // النتائج الفعلية هي 1 (فوز صاحب الأرض) ولكن Dixon-Coles سيتوقع فوز الضيف بنسبة عالية جداً.
        // وبالتالي سيكون الـ Brier Score مرتفع جداً (أكثر من 0.45).
        var report = await service.CheckForDriftAsync(50, 0.45);

        // 3. Assert
        Assert.True(report.DriftDetected, $"Expected drift to be detected. Brier Score was {report.CurrentBrierScore}");
        Assert.True(report.CurrentBrierScore > 0.45, "Brier score should exceed threshold of 0.45.");
    }
}

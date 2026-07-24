using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SportsAnalytics.Application.Services;
using SportsAnalytics.Domain.Entities;
using SportsAnalytics.Domain.Interfaces;
using SportsAnalytics.Domain.Models;
using SportsAnalytics.MathEngine;
using Xunit;
using EntityMatch = SportsAnalytics.Domain.Entities.Match;

namespace SportsAnalytics.Tests;

public class BacktestingTests
{
    [Fact]
    public async Task RunBacktestAsync_ShouldCalculateBrierScoreAndPnL()
    {
        // 1. Arrange
        var mockFeatureService = new Mock<IFeatureEngineeringService>();
        var mockRiskService = new Mock<IRiskScoringService>();

        // نولد 100 مباراة تاريخية
        var matches = new List<EntityMatch>();
        var teamA = new Team { Id = 1, Name = "Team A" };
        var teamB = new Team { Id = 2, Name = "Team B" };

        for (int i = 0; i < 100; i++)
        {
            matches.Add(new EntityMatch
            {
                Id = i + 1,
                HomeTeamId = 1,
                AwayTeamId = 2,
                HomeTeam = teamA,
                AwayTeam = teamB,
                MatchDate = DateTime.UtcNow.AddDays(-100 + i),
                // سنجعل النتيجة فوز صاحب الأرض في أغلب الأحيان لتسهيل التوقع
                HomeGoals = (i % 3 == 0) ? 1 : 2, 
                AwayGoals = (i % 3 == 0) ? 1 : 0
            });
        }

        var features = new MatchFeatures
        {
            DixonColesLambdaHome = 1.5f,
            DixonColesLambdaAway = 1.0f
        };

        mockFeatureService
            .Setup(f => f.ComputeAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(features);

        // جعل محرك المخاطر دائماً يقترح المراهنة لتجربة حسابات البنك
        var riskReport = new RiskScoreResult
        {
            BestEV = 0.10,
            KellyFraction = 0.05
        };

        mockRiskService
            .Setup(r => r.Compute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<double?>()))
            .Returns(riskReport);

        var dixonColes = new PoissonDixonColes();
        var mlPredictor = new MLMatchPredictor();
        var logger = new NullLogger<BacktestingService>();

        var backtestingService = new BacktestingService(
            mockFeatureService.Object,
            dixonColes,
            mlPredictor,
            mockRiskService.Object,
            logger
        );

        // 2. Act
        var report = await backtestingService.RunBacktestAsync(matches, startingBankroll: 1000.0);

        // 3. Assert
        Assert.NotNull(report);
        Assert.Equal(100, report.TotalMatchesTested);
        Assert.True(report.TotalBetsPlaced > 0, "يجب أن يتم وضع رهانات بسبب EV الموجب.");
        Assert.True(report.EndingBankroll > 0, "يجب ألا ينفد الرصيد تماماً.");
        
        // سجلات للتأكد من النتيجة
        Assert.Contains("تم الانتهاء من الاختبار", report.Message);
    }
}

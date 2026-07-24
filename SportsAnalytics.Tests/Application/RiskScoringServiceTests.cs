using SportsAnalytics.Application.Services;
using SportsAnalytics.Domain.Models;

namespace SportsAnalytics.Tests.Application;

/// <summary>اختبارات وحدة لـ RiskScoringService.</summary>
public class RiskScoringServiceTests
{
    private readonly RiskScoringService _svc = new();

    [Fact]
    public void Compute_WithoutOdds_ReturnsValidScore()
    {
        var result = _svc.Compute(
            "TeamA", "TeamB", DateTime.Today,
            homeWinProb: 0.5, drawProb: 0.3, awayWinProb: 0.2,
            dataQuality: 1.0);

        Assert.InRange(result.RiskScore, 0, 100);
        Assert.NotEmpty(result.Explanation);
    }

    [Fact]
    public void Compute_PerfectDataQuality_LowerRisk()
    {
        var goodData = _svc.Compute("A", "B", DateTime.Today, 0.6, 0.25, 0.15, dataQuality: 1.0);
        var badData  = _svc.Compute("A", "B", DateTime.Today, 0.6, 0.25, 0.15, dataQuality: 0.1);

        Assert.True(goodData.RiskScore < badData.RiskScore,
            "جودة بيانات أعلى يجب أن تُنتج Risk Score أقل");
    }

    [Fact]
    public void Compute_UniformProbabilities_HigherRisk()
    {
        // مباراة مجهولة النتيجة (33% لكل نتيجة)
        var uncertain = _svc.Compute("A", "B", DateTime.Today, 1.0/3, 1.0/3, 1.0/3, dataQuality: 1.0);
        // مباراة ذات نتيجة شبه محسومة
        var certain   = _svc.Compute("A", "B", DateTime.Today, 0.85, 0.10, 0.05, dataQuality: 1.0);

        Assert.True(uncertain.RiskScore > certain.RiskScore,
            "مباراة غير محسومة يجب أن تكون Risk أعلى");
    }

    [Fact]
    public void Compute_WithPositiveEV_LowerRisk()
    {
        var goodEV = _svc.Compute("A", "B", DateTime.Today, 0.6, 0.25, 0.15, dataQuality: 1.0,
            homeOdds: 2.20, drawOdds: 3.50, awayOdds: 5.00); // EV = 0.6*2.2-1 = +0.32

        var badEV = _svc.Compute("A", "B", DateTime.Today, 0.6, 0.25, 0.15, dataQuality: 1.0,
            homeOdds: 1.40, drawOdds: 3.50, awayOdds: 5.00); // EV = 0.6*1.4-1 = -0.16

        Assert.True(goodEV.RiskScore < badEV.RiskScore,
            "EV إيجابية يجب أن تُنتج Risk أقل");
    }

    [Fact]
    public void Compute_EVCalculation_Correct()
    {
        // P=0.6, Odds=2.0 → EV = 0.6*2.0 - 1 = 0.2
        var result = _svc.Compute("A", "B", DateTime.Today, 0.6, 0.3, 0.1, dataQuality: 1.0,
            homeOdds: 2.0, drawOdds: 4.0, awayOdds: 8.0);

        Assert.NotNull(result.HomeEV);
        Assert.True(Math.Abs(result.HomeEV!.Value - 0.2) < 0.001,
            $"EV المضيف يجب أن يكون 0.200 (الفعلي: {result.HomeEV:F3})");
    }

    [Fact]
    public void Compute_RiskLevelClassification_Correct()
    {
        // بيانات ضعيفة جداً → Extreme أو High
        var extreme = _svc.Compute("A", "B", DateTime.Today,
            1.0/3, 1.0/3, 1.0/3, dataQuality: 0.05);
        Assert.True(extreme.RiskLevel is RiskLevel.High or RiskLevel.Extreme);

        // احتمالات واضحة + بيانات جيدة → Low أو Medium
        var low = _svc.Compute("A", "B", DateTime.Today,
            0.85, 0.10, 0.05, dataQuality: 1.0);
        Assert.True(low.RiskLevel is RiskLevel.Low or RiskLevel.Medium);
    }

    [Fact]
    public void Compute_KellyFraction_PositiveOnlyWhenEVPositive()
    {
        // EV سلبية → Kelly = 0
        var negEV = _svc.Compute("A", "B", DateTime.Today, 0.4, 0.3, 0.3, dataQuality: 1.0,
            homeOdds: 1.50, drawOdds: 3.50, awayOdds: 5.00);
        Assert.True(negEV.KellyFraction is null or <= 0.001,
            "Kelly يجب أن يكون 0 عند EV سلبية");
    }

    [Theory]
    [InlineData(0.5,  0.3,  0.2)]  // مجموع = 1.0 (عادي)
    [InlineData(0.6,  0.4,  0.3)]  // مجموع > 1.0 (يجب تطبيع)
    [InlineData(0.01, 0.01, 0.01)] // مجموع صغير (يجب تطبيع)
    public void Compute_AlwaysNormalizeProbabilities(double h, double d, double a)
    {
        var result = _svc.Compute("A", "B", DateTime.Today, h, d, a, dataQuality: 0.8);
        var total = result.HomeWinProb + result.DrawProb + result.AwayWinProb;

        Assert.True(Math.Abs(total - 1.0) < 0.001,
            $"الاحتمالات دائماً يجب أن تُطبَّع إلى 1 (الفعلي: {total:F4})");
    }
}

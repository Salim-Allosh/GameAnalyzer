using SportsAnalytics.MathEngine;

namespace SportsAnalytics.Tests.MathEngine;

/// <summary>اختبارات وحدة لـ MonteCarloSimulator.</summary>
public class MonteCarloSimulatorTests
{
    [Theory]
    [InlineData(1.5, 1.0)]
    [InlineData(2.0, 0.8)]
    [InlineData(0.5, 2.5)]
    public void Simulate_ProbabilitiesSumToOne(double lambdaHome, double lambdaAway)
    {
        var sim = new MonteCarloSimulator { MinIterations = 10_000, MaxIterations = 50_000 };
        var result = sim.Simulate(lambdaHome, lambdaAway);

        var total = result.HomeWinProbability + result.DrawProbability + result.AwayWinProbability;
        Assert.True(Math.Abs(total - 1.0) < 0.005, $"المجموع يجب أن يكون ≈ 1 (الفعلي: {total:F4})");
    }

    [Fact]
    public void Simulate_HighLambdaHome_FavorsHome()
    {
        var sim = new MonteCarloSimulator { MinIterations = 50_000 };
        var result = sim.Simulate(lambdaHome: 3.0, lambdaAway: 0.5);

        Assert.True(result.HomeWinProbability > 0.8,
            $"مع λHome=3.0 وλAway=0.5 يجب أن يفوز المضيف بأكثر من 80% (الفعلي: {result.HomeWinProbability:P1})");
    }

    [Fact]
    public void Simulate_SymmetricLambdas_NearEqualHomeAway()
    {
        var sim = new MonteCarloSimulator { MinIterations = 100_000 };
        var result = sim.Simulate(lambdaHome: 1.0, lambdaAway: 1.0);

        // مع lambdas متساوية، Home وAway يجب أن يكونا قريبين جداً
        var diff = Math.Abs(result.HomeWinProbability - result.AwayWinProbability);
        Assert.True(diff < 0.05, $"الفارق يجب أن يكون < 5% (الفعلي: {diff:P2})");
    }

    [Fact]
    public void Simulate_StdErrorBelowTarget()
    {
        var sim = new MonteCarloSimulator
        {
            MinIterations = 10_000,
            MaxIterations = 200_000,
            TargetStdError = 0.002
        };
        var result = sim.Simulate(1.4, 1.1);

        Assert.True(result.StandardError <= sim.TargetStdError + 0.0001,
            $"الخطأ المعياري ({result.StandardError:F5}) يجب أن يكون ≤ {sim.TargetStdError}");
    }

    [Fact]
    public void Simulate_TopScores_SumReasonable()
    {
        var sim = new MonteCarloSimulator { MinIterations = 50_000 };
        var result = sim.Simulate(1.5, 1.0);

        var topSum = result.TopScores.Sum(s => s.Prob);
        Assert.True(topSum > 0.5 && topSum <= 1.0,
            $"أعلى 10 نتائج يجب أن تغطي أكثر من 50% (الفعلي: {topSum:P1})");
    }

    [Fact]
    public void GetScoreProbability_ValidScore_InRange()
    {
        var sim = new MonteCarloSimulator { MinIterations = 50_000 };
        var result = sim.Simulate(1.5, 1.0);

        var p10 = result.GetScoreProbability(1, 0);
        Assert.True(p10 is > 0.05 and < 0.30,
            $"احتمال 1-0 يجب أن يكون بين 5% و30% (الفعلي: {p10:P2})");
    }

    [Fact]
    public void Simulate_InvalidLambda_ThrowsException()
    {
        var sim = new MonteCarloSimulator();
        Assert.Throws<ArgumentException>(() => sim.Simulate(0, 1.0));
        Assert.Throws<ArgumentException>(() => sim.Simulate(1.0, -1.0));
    }

    [Fact]
    public void Simulate_Reproducible_WithSameSeed()
    {
        // مع Seed ثابت في Constructor، نتيجتان للمحاكاة يجب أن تتقاربا
        var sim1 = new MonteCarloSimulator { MinIterations = 50_000, MaxIterations = 50_000 };
        var sim2 = new MonteCarloSimulator { MinIterations = 50_000, MaxIterations = 50_000 };

        var r1 = sim1.Simulate(1.5, 0.8);
        var r2 = sim2.Simulate(1.5, 0.8);

        Assert.True(Math.Abs(r1.HomeWinProbability - r2.HomeWinProbability) < 0.005,
            "نتيجتان بنفس الـ Seed يجب أن تكونا متقاربتين");
    }
}

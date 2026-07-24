using SportsAnalytics.MathEngine;

namespace SportsAnalytics.Tests.MathEngine;

/// <summary>
/// اختبارات وحدة لـ PoissonDixonColes.
/// </summary>
public class PoissonDixonColesTests
{
    private static List<MatchRecord> BuildSampleData()
    {
        // 30 مباراة اصطناعية: TeamA مهيمن، TeamB متوسط، TeamC ضعيف
        var records = new List<MatchRecord>();
        var baseDate = new DateTime(2023, 1, 1);

        int idx = 0;
        string[] teams = ["TeamA", "TeamB", "TeamC", "TeamD", "TeamE"];

        // TeamA يفوز دائماً بـ 3-0
        for (int i = 0; i < 10; i++)
            records.Add(new MatchRecord("TeamA", teams[(i % 4) + 1], baseDate.AddDays(idx++), 3, 0));

        // TeamB يفوز بـ 2-1
        for (int i = 0; i < 10; i++)
            records.Add(new MatchRecord("TeamB", teams[i % 3 == 0 ? 4 : i % 3], baseDate.AddDays(idx++), 2, 1));

        // مباريات متنوعة
        records.Add(new MatchRecord("TeamC", "TeamD", baseDate.AddDays(idx++), 0, 0));
        records.Add(new MatchRecord("TeamD", "TeamE", baseDate.AddDays(idx++), 1, 1));
        records.Add(new MatchRecord("TeamE", "TeamC", baseDate.AddDays(idx++), 0, 2));
        records.Add(new MatchRecord("TeamC", "TeamA", baseDate.AddDays(idx++), 0, 4));
        records.Add(new MatchRecord("TeamD", "TeamA", baseDate.AddDays(idx++), 1, 3));
        records.Add(new MatchRecord("TeamB", "TeamA", baseDate.AddDays(idx++), 1, 2));
        records.Add(new MatchRecord("TeamE", "TeamB", baseDate.AddDays(idx++), 0, 2));
        records.Add(new MatchRecord("TeamC", "TeamB", baseDate.AddDays(idx++), 0, 1));

        return records;
    }

    [Fact]
    public void Train_WithValidData_SetsIsTrained()
    {
        var model = new PoissonDixonColes();
        model.Train(BuildSampleData());
        Assert.True(model.IsTrained);
    }

    [Fact]
    public void Train_StrongerTeam_HasHigherAttack()
    {
        var model = new PoissonDixonColes();
        model.Train(BuildSampleData());

        Assert.True(model.AttackParams["TeamA"] > model.AttackParams["TeamC"],
            "TeamA يجب أن يكون أقوى هجوماً من TeamC");
    }

    [Fact]
    public void ComputeLambdas_HomeAdvantage_HomeGreater()
    {
        var model = new PoissonDixonColes();
        model.Train(BuildSampleData());

        var (lHome, lAway) = model.ComputeLambdas("TeamA", "TeamC");

        Assert.True(lHome > 0, "λ Home يجب أن يكون > 0");
        Assert.True(lAway > 0, "λ Away يجب أن يكون > 0");
        Assert.True(lHome > lAway, "الفريق الأقوى في المنزل يجب أن يكون λ أكبر");
    }

    [Fact]
    public void ComputeOutcomeProbabilities_SumToOne()
    {
        var model = new PoissonDixonColes();
        model.Train(BuildSampleData());

        var (h, d, a) = model.ComputeOutcomeProbabilities("TeamA", "TeamB");
        var total = h + d + a;

        Assert.True(Math.Abs(total - 1.0) < 0.01,
            $"الاحتمالات يجب أن تكون مجموعها 1، لكنها: {total:F4}");
    }

    [Fact]
    public void ComputeOutcomeProbabilities_StrongerTeam_HigherWinProb()
    {
        var model = new PoissonDixonColes();
        model.Train(BuildSampleData());

        var (teamAWin, _, teamCWin) = model.ComputeOutcomeProbabilities("TeamA", "TeamC");

        Assert.True(teamAWin > teamCWin,
            "TeamA يجب أن تكون نسبة فوزه أعلى من TeamC");
    }

    [Fact]
    public void ComputeBrierScore_IsLowerThanBaseline()
    {
        var model = new PoissonDixonColes();
        var data = BuildSampleData();
        model.Train(data); // تدريب على كل البيانات

        double brierScore = model.ComputeBrierScore(data); // اختبار على نفس البيانات للتأكد من قدرة النموذج على التعلم
        double baseline = 2.0 / 3.0; // عشوائي

        Assert.True(brierScore < baseline,
            $"Brier Score ({brierScore:F4}) يجب أن يكون أقل من Baseline ({baseline:F4})");
    }

    [Fact]
    public void Train_UnknownTeam_ThrowsOnCompute()
    {
        var model = new PoissonDixonColes();
        model.Train(BuildSampleData());

        // فريق غير موجود في بيانات التدريب
        Assert.Throws<KeyNotFoundException>(() =>
            model.ComputeLambdas("Unknown FC", "TeamA"));
    }
}

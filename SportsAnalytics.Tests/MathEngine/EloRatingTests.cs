using SportsAnalytics.MathEngine;

namespace SportsAnalytics.Tests.MathEngine;

/// <summary>اختبارات وحدة لـ EloRating.</summary>
public class EloRatingTests
{
    private static List<MatchRecord> BuildHistory()
    {
        var date = new DateTime(2023, 1, 1);
        return
        [
            new("TeamA", "TeamB", date.AddDays(0),  3, 0),
            new("TeamA", "TeamC", date.AddDays(7),  2, 0),
            new("TeamA", "TeamD", date.AddDays(14), 2, 1),
            new("TeamB", "TeamC", date.AddDays(3),  1, 1),
            new("TeamB", "TeamD", date.AddDays(10), 2, 0),
            new("TeamC", "TeamD", date.AddDays(17), 0, 1),
            new("TeamA", "TeamB", date.AddDays(21), 1, 0),
            new("TeamC", "TeamA", date.AddDays(24), 0, 3),
        ];
    }

    [Fact]
    public void TrainOnHistory_AllTeams_HaveRatings()
    {
        var elo = new EloRating();
        elo.TrainOnHistory(BuildHistory());

        foreach (var team in new[] { "TeamA", "TeamB", "TeamC", "TeamD" })
            Assert.True(elo.Ratings.ContainsKey(team), $"{team} يجب أن يكون له تقييم");
    }

    [Fact]
    public void TrainOnHistory_WinningTeam_RatingIncreases()
    {
        var elo = new EloRating();
        double before = elo.GetRating("TeamA"); // التقييم الافتراضي

        elo.TrainOnHistory(BuildHistory());
        double after = elo.GetRating("TeamA");

        Assert.True(after > before, $"TeamA يجب أن يرتفع تقييمه (كان {before:F0} → صار {after:F0})");
    }

    [Fact]
    public void TrainOnHistory_LosingTeam_RatingDecreases()
    {
        var elo = new EloRating();
        double before = elo.GetRating("TeamD");
        elo.TrainOnHistory(BuildHistory());
        double after = elo.GetRating("TeamD");

        Assert.True(after < before, $"TeamD يجب أن ينخفض تقييمه (كان {before:F0} → صار {after:F0})");
    }

    [Fact]
    public void ComputeOutcomeProbabilities_SumToOne()
    {
        var elo = new EloRating();
        elo.TrainOnHistory(BuildHistory());

        var (h, d, a) = elo.ComputeOutcomeProbabilities("TeamA", "TeamB");

        Assert.True(Math.Abs(h + d + a - 1.0) < 0.001,
            $"الاحتمالات يجب أن تجمع 1، لكنها: {h+d+a:F4}");
    }

    [Fact]
    public void ComputeOutcomeProbabilities_EqualRatings_NearEqual()
    {
        var elo = new EloRating(); // كل الفرق بتقييم افتراضي متساوٍ
        var (h, d, a) = elo.ComputeOutcomeProbabilities("NewTeamX", "NewTeamY");

        // مع تساوي التقييم، Home يجب أن يكون أعلى قليلاً (Home Advantage)
        Assert.True(h > a, "فوز المضيف يجب أن يكون أعلى عند تساوي التقييمات (Home Advantage)");
        Assert.True(h + d + a is > 0.99 and < 1.01, "مجموع يجب أن يكون ≈ 1");
    }

    [Fact]
    public void GetRating_UnknownTeam_ReturnsDefault()
    {
        var elo = new EloRating();
        var rating = elo.GetRating("UnknownFC");
        Assert.True(rating > 0, "التقييم الافتراضي يجب أن يكون > 0");
    }

    [Fact]
    public void TrainOnHistory_TeamA_RankedHighest()
    {
        var elo = new EloRating();
        elo.TrainOnHistory(BuildHistory());

        var topTeam = elo.Ratings.MaxBy(x => x.Value).Key;
        Assert.Equal("TeamA", topTeam);
    }
}

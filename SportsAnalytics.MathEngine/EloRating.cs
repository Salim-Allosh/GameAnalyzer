namespace SportsAnalytics.MathEngine;

/// <summary>
/// نظام تقييم Elo لكرة القدم.
/// كل فريق يبدأ بـ 1500 نقطة، وتُحدَّث بعد كل مباراة حسب النتيجة وقوة الخصم.
/// 
/// المرجع: Hvattum, L.M. & Arntzen, H. (2010). "Using ELO ratings for match result prediction
/// in association football." International Journal of Forecasting, 26(3), 460-470.
/// </summary>
public class EloRating
{
    private readonly Dictionary<string, double> _ratings = new();

    public const double DefaultRating = 1500.0;
    public const double KFactor = 20.0;  // حساسية التحديث
    public const double HomeAdvantagePoints = 50.0; // إضافة ميزة الأرض

    public IReadOnlyDictionary<string, double> Ratings => _ratings;

    /// <summary>يُرجع تقييم فريق (1500 إذا غير موجود).</summary>
    public double GetRating(string team)
        => _ratings.TryGetValue(team, out var r) ? r : DefaultRating;

    /// <summary>
    /// يُحدّث تقييمات الفريقين بعد مباراة.
    /// actualResult: 1 = فاز المضيف، 0.5 = تعادل، 0 = فاز الضيف.
    /// </summary>
    public void UpdateAfterMatch(string homeTeam, string awayTeam, double actualResult)
    {
        var rHome = GetRating(homeTeam);
        var rAway = GetRating(awayTeam);

        // التوقع المسبق
        var expectedHome = ExpectedScore(rHome, rAway, HomeAdvantagePoints);
        var expectedAway = 1.0 - expectedHome;

        // التحديث
        _ratings[homeTeam] = rHome + KFactor * (actualResult - expectedHome);
        _ratings[awayTeam] = rAway + KFactor * ((1 - actualResult) - expectedAway);
    }

    /// <summary>يُدرّب Elo على قائمة مباريات بالترتيب الزمني.</summary>
    public void TrainOnHistory(IEnumerable<MatchRecord> matches)
    {
        foreach (var m in matches.OrderBy(m => m.Date))
        {
            var result = m.HomeGoals > m.AwayGoals ? 1.0
                       : m.HomeGoals == m.AwayGoals ? 0.5
                       : 0.0;
            UpdateAfterMatch(m.HomeTeam, m.AwayTeam, result);
        }
    }

    /// <summary>
    /// يحوّل فارق Elo إلى احتمالات فوز/تعادل/خسارة.
    /// يستخدم التحويل اللوجستي القياسي.
    /// </summary>
    public (double HomeWin, double Draw, double AwayWin) ComputeOutcomeProbabilities(
        string homeTeam, string awayTeam)
    {
        var rHome = GetRating(homeTeam);
        var rAway = GetRating(awayTeam);

        var pHomeWinOrDraw = ExpectedScore(rHome, rAway, HomeAdvantagePoints);

        // تقدير التعادل كنسبة ثابتة ~28% (وسط بين الحدين)
        // يُحسَّن لاحقاً مع Calibration (المرحلة 7)
        const double drawRate = 0.28;
        var pHome = pHomeWinOrDraw * (1 - drawRate);
        var pDraw = drawRate;
        var pAway = (1 - pHomeWinOrDraw) * (1 - drawRate);

        return (pHome, pDraw, pAway);
    }

    // ── دوال مساعدة ──

    private static double ExpectedScore(double ratingA, double ratingB, double homeAdvantage = 0.0)
        => 1.0 / (1.0 + Math.Pow(10.0, (ratingB - (ratingA + homeAdvantage)) / 400.0));
}

namespace SportsAnalytics.Domain.Entities;

/// <summary>
/// إحصائيات تفصيلية لمباراة واحدة (خطوط، تسديدات، استحواذ...).
/// تُستخدم كمدخل لطبقة Feature Engineering (المرحلة 4).
/// </summary>
public class MatchStatistics
{
    public int Id { get; set; }
    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;

    // إحصائيات الفريق المضيف
    public int HomeShotsOnTarget { get; set; }
    public int HomeShotsTotal { get; set; }
    public double HomePossessionPct { get; set; }
    public int HomeCorners { get; set; }
    public int HomeFouls { get; set; }
    public int HomeYellowCards { get; set; }
    public int HomeRedCards { get; set; }

    // إحصائيات الفريق الضيف
    public int AwayShotsOnTarget { get; set; }
    public int AwayShotsTotal { get; set; }
    public double AwayPossessionPct { get; set; }
    public int AwayCorners { get; set; }
    public int AwayFouls { get; set; }
    public int AwayYellowCards { get; set; }
    public int AwayRedCards { get; set; }

    // جودة البيانات (0-1): 1 = بيانات كاملة ومتحقق منها
    public double DataQualityScore { get; set; } = 1.0;
    public string DataSource { get; set; } = string.Empty;
}

namespace SportsAnalytics.Domain.Models;

/// <summary>
/// كائن الخصائص الإضافية لمباراة واحدة — مدخل النموذج التكميلي (ML.NET).
/// كل خاصية هي رقم مشتق من البيانات التاريخية.
/// </summary>
public class MatchFeatures
{
    // ── معرّفات المباراة ──
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public DateTime MatchDate { get; set; }

    // ── راحة الفريق (أيام منذ آخر مباراة) ──
    public float HomeDaysSinceLastMatch { get; set; }   // كلما أكبر = أكثر راحة
    public float AwayDaysSinceLastMatch { get; set; }

    // ── شكل الفريق (آخر 5 مباريات) ──
    // نقاط: فوز=3، تعادل=1، خسارة=0 — مقسومة على 15 للتطبيع (0-1)
    public float HomeFormLast5 { get; set; }
    public float AwayFormLast5 { get; set; }

    // ── نسبة الأهداف (آخر 5 مباريات) ──
    public float HomeAvgGoalsScored { get; set; }
    public float HomeAvgGoalsConceded { get; set; }
    public float AwayAvgGoalsScored { get; set; }
    public float AwayAvgGoalsConceded { get; set; }

    // ── تاريخ المواجهات المباشرة (H2H — آخر 5 لقاءات) ──
    public float H2HHomeWinRate { get; set; }   // نسبة فوز المضيف تاريخياً
    public float H2HDrawRate { get; set; }
    public float H2HAvgTotalGoals { get; set; } // متوسط مجموع الأهداف

    // ── مخرجات Dixon-Coles وElo (تُدمج كـ features) ──
    public float DixonColesLambdaHome { get; set; }
    public float DixonColesLambdaAway { get; set; }
    public float EloHome { get; set; }
    public float EloAway { get; set; }
    public float EloDiff { get; set; }  // فارق Elo (Home - Away)

    // ── مستوى جودة البيانات (0-1) ──
    public float DataQuality { get; set; } = 1.0f;
}

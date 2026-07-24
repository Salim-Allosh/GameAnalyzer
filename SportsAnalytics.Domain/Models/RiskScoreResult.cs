namespace SportsAnalytics.Domain.Models;

/// <summary>
/// نتيجة تقييم المخاطرة لمباراة واحدة.
/// لا توصية بالرهان — فقط تقييم موضوعي للمخاطرة.
/// </summary>
public class RiskScoreResult
{
    // ── معرّف المباراة ──
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public DateTime MatchDate { get; set; }

    // ── احتمالات النموذج المدموج (Blend) ──
    public double HomeWinProb { get; set; }
    public double DrawProb { get; set; }
    public double AwayWinProb { get; set; }

    // ── أوزان السوق (اختيارية — تُدخل يدوياً أو من API) ──
    public double? HomeOdds { get; set; }   // مثال: 1.80
    public double? DrawOdds { get; set; }   // مثال: 3.60
    public double? AwayOdds { get; set; }   // مثال: 4.50

    // ── القيمة المتوقعة EV لكل نتيجة ──
    public double? HomeEV { get; set; }     // EV = P × Odds - 1
    public double? DrawEV { get; set; }
    public double? AwayEV { get; set; }
    public double? BestEV { get; set; }     // أعلى EV بين النتائج الثلاث

    // ── Kelly Criterion مُعدَّل ──
    public double? KellyFraction { get; set; }  // نسبة رأس المال المقترحة (نظرياً فقط)

    // ── مكونات Risk Score ──
    public double ProbabilityVariance { get; set; }  // تباين الاحتمالات (كلما أعلى = أقل يقين)
    public double DataQualityScore { get; set; }     // من MatchFeatures
    public double EntropyScore { get; set; }         // Shannon Entropy للاحتمالات

    // ── Risk Score النهائي ──
    public double RiskScore { get; set; }            // 0-100
    public RiskLevel RiskLevel { get; set; }
    public string Explanation { get; set; } = string.Empty;

    // ── بيانات إضافية ──
    public double DataQuality { get; set; }
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>مستويات المخاطرة.</summary>
public enum RiskLevel
{
    Low     = 0,  // 0-30   — الاحتمالات متقاربة من السوق، بيانات جيدة
    Medium  = 1,  // 30-60  — عدم يقين معتدل
    High    = 2,  // 60-80  — عدم يقين عالٍ أو بيانات ضعيفة
    Extreme = 3   // 80-100 — تجنب الرهان
}

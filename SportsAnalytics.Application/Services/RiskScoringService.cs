using SportsAnalytics.Domain.Interfaces;
using SportsAnalytics.Domain.Models;

namespace SportsAnalytics.Application.Services;

/// <summary>
/// نظام تقييم المخاطرة — يُنتج Risk Score موضوعياً لكل مباراة.
///
/// مكونات الحساب:
///  1. Shannon Entropy (عدم اليقين في الاحتمالات)
///  2. تباين الاحتمالات
///  3. جودة البيانات (DataQuality من MatchFeatures)
///  4. القيمة المتوقعة EV (إذا أُدخلت الأوزان)
///  5. Kelly Criterion مُعدَّل (إذا أُدخلت الأوزان)
///
/// DISCLAIMER: هذا النظام لأغراض بحثية وتعليمية فقط.
/// </summary>
public class RiskScoringService : IRiskScoringService
{
    // ── أوزان مكونات Risk Score (مجموعها 1) ──
    private const double WeightEntropy     = 0.35;  // عدم اليقين
    private const double WeightVariance    = 0.25;  // تباين الاحتمالات
    private const double WeightDataQuality = 0.25;  // جودة البيانات
    private const double WeightEV         = 0.15;  // القيمة المتوقعة (0 إذا لا odds)

    // Kelly تخفيف — يُقلّل من الرهان الكامل (Kelly الكامل محفوف بالمخاطر)
    private const double KellyDivisor = 4.0;

    public RiskScoreResult Compute(
        string homeTeam, string awayTeam, DateTime matchDate,
        double homeWinProb, double drawProb, double awayWinProb,
        double dataQuality,
        double? homeOdds = null, double? drawOdds = null, double? awayOdds = null)
    {
        // تطبيع الاحتمالات
        var total = homeWinProb + drawProb + awayWinProb;
        if (total <= 0) { homeWinProb = drawProb = awayWinProb = 1.0 / 3; total = 1.0; }
        homeWinProb /= total; drawProb /= total; awayWinProb /= total;

        // ── 1. Shannon Entropy (0-1) ──
        // أقصى Entropy = log2(3) ≈ 1.585 (ثلاثة نتائج متساوية = أعلى عدم يقين)
        var entropy = ComputeEntropy(homeWinProb, drawProb, awayWinProb);
        var entropyNorm = entropy / Math.Log(3, 2); // تطبيع إلى [0,1]

        // ── 2. تباين الاحتمالات (0-1) ──
        // التباين منخفض = الفريق المضيف مفضّل بوضوح = مخاطرة أقل
        var mean = (homeWinProb + drawProb + awayWinProb) / 3.0;
        var variance = (Math.Pow(homeWinProb - mean, 2) +
                        Math.Pow(drawProb     - mean, 2) +
                        Math.Pow(awayWinProb  - mean, 2)) / 3.0;
        // التباين الأقصى نظرياً = 2/9 ≈ 0.222 (حالة 0,0,1)
        var varianceNorm = 1.0 - Math.Clamp(variance / 0.222, 0.0, 1.0);
        // varianceNorm=1 → تباين صفري (مؤكد) → مخاطرة منخفضة
        // varianceNorm=0 → تباين أقصى → عدم يقين عالٍ → مخاطرة أعلى

        // ── 3. جودة البيانات (نُعكس: جودة منخفضة = خطر أعلى) ──
        var dataRisk = 1.0 - Math.Clamp(dataQuality, 0.0, 1.0);

        // ── 4. EV Component (اختياري) ──
        double? homeEV = null, drawEV = null, awayEV = null;
        double evRisk = 0.5; // محايد إذا لا odds

        if (homeOdds.HasValue && drawOdds.HasValue && awayOdds.HasValue)
        {
            homeEV = homeWinProb * homeOdds.Value - 1.0;
            drawEV = drawProb    * drawOdds.Value - 1.0;
            awayEV = awayWinProb * awayOdds.Value - 1.0;

            var bestEV = Math.Max(homeEV.Value, Math.Max(drawEV.Value, awayEV.Value));
            // EV إيجابية = فرصة نظرية (لكن ليست ضماناً)
            // EV < 0: خطر أعلى، EV > 0.1: خطر أقل نسبياً
            evRisk = Math.Clamp(0.5 - bestEV * 2.0, 0.0, 1.0);
        }

        // ── Kelly Criterion مُعدَّل ──
        double? kellyFraction = null;
        if (homeOdds.HasValue)
        {
            var b = homeOdds.Value - 1.0;   // صافي الربح لكل وحدة
            var q = 1.0 - homeWinProb;
            var rawKelly = (homeWinProb * b - q) / b;
            kellyFraction = rawKelly > 0
                ? Math.Clamp(rawKelly / KellyDivisor, 0.0, 0.25)  // حد أقصى 25%
                : 0.0;
        }

        // ── Risk Score المُركَّب (0-100) ──
        double rawRisk;
        if (homeOdds.HasValue)
        {
            rawRisk = WeightEntropy     * entropyNorm
                    + WeightVariance    * (1.0 - varianceNorm)
                    + WeightDataQuality * dataRisk
                    + WeightEV          * evRisk;
        }
        else
        {
            // بدون Odds: إعادة توزيع الوزن
            var wE  = WeightEntropy     / (1 - WeightEV);
            var wV  = WeightVariance    / (1 - WeightEV);
            var wDQ = WeightDataQuality / (1 - WeightEV);
            rawRisk = wE * entropyNorm + wV * (1.0 - varianceNorm) + wDQ * dataRisk;
        }

        var riskScore = Math.Clamp(rawRisk * 100, 0.0, 100.0);
        var riskLevel = ClassifyRisk(riskScore);
        var explanation = BuildExplanation(
            riskLevel, riskScore, homeWinProb, drawProb, awayWinProb,
            entropy, dataQuality, homeEV, drawEV, awayEV);

        return new RiskScoreResult
        {
            HomeTeam     = homeTeam,
            AwayTeam     = awayTeam,
            MatchDate    = matchDate,
            HomeWinProb  = homeWinProb,
            DrawProb     = drawProb,
            AwayWinProb  = awayWinProb,
            HomeOdds     = homeOdds,
            DrawOdds     = drawOdds,
            AwayOdds     = awayOdds,
            HomeEV       = homeEV,
            DrawEV       = drawEV,
            AwayEV       = awayEV,
            BestEV       = homeEV.HasValue
                ? Math.Max(homeEV.Value, Math.Max(drawEV!.Value, awayEV!.Value))
                : null,
            KellyFraction        = kellyFraction,
            ProbabilityVariance  = variance,
            DataQualityScore     = dataQuality,
            EntropyScore         = entropy,
            RiskScore            = riskScore,
            RiskLevel            = riskLevel,
            Explanation          = explanation,
            DataQuality          = dataQuality,
        };
    }

    // ── دوال مساعدة ──

    private static double ComputeEntropy(double p1, double p2, double p3)
    {
        static double SafeLog(double p) => p > 0 ? p * Math.Log(p, 2) : 0;
        return -(SafeLog(p1) + SafeLog(p2) + SafeLog(p3));
    }

    private static RiskLevel ClassifyRisk(double score) => score switch
    {
        < 30  => RiskLevel.Low,
        < 60  => RiskLevel.Medium,
        < 80  => RiskLevel.High,
        _     => RiskLevel.Extreme
    };

    private static string BuildExplanation(
        RiskLevel level, double score,
        double pH, double pD, double pA,
        double entropy, double dataQuality,
        double? hEV, double? dEV, double? aEV)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"مستوى المخاطرة: {RiskLevelArabic(level)} ({score:F1}/100)");
        sb.AppendLine($"الاحتمالات: فوز {pH:P0} | تعادل {pD:P0} | خسارة {pA:P0}");
        sb.AppendLine($"Shannon Entropy: {entropy:F3} (الأقصى=1.585 → عدم يقين تام)");
        sb.AppendLine($"جودة البيانات: {dataQuality:P0}");

        if (hEV.HasValue)
        {
            sb.AppendLine($"EV المضيف: {hEV:+0.000;-0.000}");
            sb.AppendLine($"EV التعادل: {dEV:+0.000;-0.000}");
            sb.AppendLine($"EV الضيف: {aEV:+0.000;-0.000}");
        }
        else
            sb.AppendLine("أوزان السوق: غير مُدخلة (لا يمكن حساب EV)");

        sb.Append(level switch
        {
            RiskLevel.Low     => "⚠️ الاحتمالات واضحة نسبياً — لكن لا ضمان في كرة القدم.",
            RiskLevel.Medium  => "⚠️ عدم يقين معتدل — تحليل إضافي مُوصى به.",
            RiskLevel.High    => "🔴 عدم يقين عالٍ أو بيانات ضعيفة — توخَّ الحذر.",
            RiskLevel.Extreme => "🚫 مخاطرة قصوى — يُنصح بتجنب الرهان.",
            _                 => string.Empty
        });

        return sb.ToString();
    }

    private static string RiskLevelArabic(RiskLevel l) => l switch
    {
        RiskLevel.Low     => "منخفض",
        RiskLevel.Medium  => "متوسط",
        RiskLevel.High    => "عالٍ",
        RiskLevel.Extreme => "قصوى",
        _                 => "غير محدد"
    };
}

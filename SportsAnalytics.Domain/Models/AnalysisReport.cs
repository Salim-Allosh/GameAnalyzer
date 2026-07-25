using SportsAnalytics.Domain.Models;

namespace SportsAnalytics.Domain.Models;

/// <summary>
/// التقرير الكامل لتحليل مباراة — مخرج Orchestrator الموحّد.
/// يُعرض مباشرةً في الواجهة (WPF Dashboard).
/// </summary>
public class AnalysisReport
{
    // ── معلومات المباراة ──
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public DateTime MatchDate { get; set; }
    public string League { get; set; } = string.Empty;

    // ── النتيجة الحقيقية (إن وُجدت) ──
    public int? ActualHomeGoals { get; set; }
    public int? ActualAwayGoals { get; set; }
    public bool HasActualResult => ActualHomeGoals.HasValue && ActualAwayGoals.HasValue;

    // ── احتمالات كل نموذج ──
    public double DcHomeWin { get; set; }
    public double DcDraw { get; set; }
    public double DcAwayWin { get; set; }

    public double EloHomeWin { get; set; }
    public double EloDraw { get; set; }
    public double EloAwayWin { get; set; }

    public double MlHomeWin { get; set; }
    public double MlDraw { get; set; }
    public double MlAwayWin { get; set; }

    // ── الاحتمالات المدموجة (الرأي النهائي) ──
    public double BlendHomeWin { get; set; }
    public double BlendDraw { get; set; }
    public double BlendAwayWin { get; set; }
    public double BlendAlpha { get; set; } = 0.3;

    // ── Monte Carlo ──
    public double McHomeWin { get; set; }
    public double McDraw { get; set; }
    public double McAwayWin { get; set; }
    public int McIterations { get; set; }
    public double McStdError { get; set; }
    public List<(int Home, int Away, double Prob)> TopScores { get; set; } = [];
    public double[,] ExactProbabilityMatrix { get; set; } = new double[0, 0];

    // ── Feature Engineering ──
    public MatchFeatures Features { get; set; } = new();

    // ── Risk Scoring ──
    public RiskScoreResult Risk { get; set; } = new();

    // ── Betting Markets ──
    public List<BettingMarketPrediction> BettingMarkets { get; set; } = new();

    // ── Elo Ratings ──
    public double EloRatingHome { get; set; }
    public double EloRatingAway { get; set; }

    // ── λ Dixon-Coles ──
    public double LambdaHome { get; set; }
    public double LambdaAway { get; set; }

    // ── مؤشر ثقة عام (0-100) ──
    public double ConfidenceScore => Math.Max(0, 100 - Risk.RiskScore);

    // ── وقت التحليل ──
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public double ComputationMs { get; set; }

    // ── النتيجة الأكثر احتمالاً ──
    public string MostLikelyOutcome => BlendHomeWin >= BlendDraw && BlendHomeWin >= BlendAwayWin
        ? $"فوز {HomeTeam} ({BlendHomeWin:P0})"
        : BlendDraw >= BlendAwayWin
            ? $"تعادل ({BlendDraw:P0})"
            : $"فوز {AwayTeam} ({BlendAwayWin:P0})";
}

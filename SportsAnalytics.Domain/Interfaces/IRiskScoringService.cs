using SportsAnalytics.Domain.Models;

namespace SportsAnalytics.Domain.Interfaces;

/// <summary>
/// عقد خدمة تقييم المخاطرة.
/// </summary>
public interface IRiskScoringService
{
    /// <summary>
    /// يحسب Risk Score لمباراة بناءً على احتمالات النموذج وأوزان السوق (اختيارية).
    /// </summary>
    RiskScoreResult Compute(
        string homeTeam,
        string awayTeam,
        DateTime matchDate,
        double homeWinProb,
        double drawProb,
        double awayWinProb,
        double dataQuality,
        double? homeOdds = null,
        double? drawOdds = null,
        double? awayOdds = null);
}

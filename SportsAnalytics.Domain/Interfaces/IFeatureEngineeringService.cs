using SportsAnalytics.Domain.Models;

namespace SportsAnalytics.Domain.Interfaces;

/// <summary>
/// عقد خدمة حساب الخصائص الإضافية لأي مباراة.
/// يُنفَّذ في Application — يقرأ من قاعدة البيانات ويُرجع MatchFeatures.
/// </summary>
public interface IFeatureEngineeringService
{
    /// <summary>
    /// يحسب MatchFeatures كاملاً لمباراة قادمة بين فريقين.
    /// </summary>
    Task<MatchFeatures> ComputeAsync(
        int homeTeamId,
        int awayTeamId,
        DateTime matchDate,
        CancellationToken ct = default);
}

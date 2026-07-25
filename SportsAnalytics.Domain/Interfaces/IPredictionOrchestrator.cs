using SportsAnalytics.Domain.Models;

namespace SportsAnalytics.Domain.Interfaces;

/// <summary>
/// المنسّق المركزي — واجهة موحّدة لكل طبقات التحليل.
/// الواجهة (WPF) تستدعي هذا الـ Interface فقط.
/// </summary>
public interface IPredictionOrchestrator
{
    /// <summary>
    /// يُشغّل التحليل الكامل لمباراة: Features → DC → Elo → MC → ML → Blend → Risk.
    /// </summary>
    Task<AnalysisReport> AnalyzeAsync(
        int homeTeamId,
        int awayTeamId,
        DateTime matchDate,
        double? homeOdds = null,
        double? drawOdds = null,
        double? awayOdds = null,
        IProgress<string>? statusProgress = null,
        IProgress<int>? percentProgress = null,
        CancellationToken ct = default);
}

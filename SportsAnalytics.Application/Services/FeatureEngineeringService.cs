using Microsoft.EntityFrameworkCore;
using SportsAnalytics.Domain.Interfaces;
using SportsAnalytics.Domain.Models;
using SportsAnalytics.Infrastructure.Data;
using SportsAnalytics.MathEngine;

namespace SportsAnalytics.Application.Services;

/// <summary>
/// يحسب MatchFeatures كاملاً لمباراة قادمة:
/// - راحة الفريق (أيام منذ آخر مباراة)
/// - شكل الفريق (آخر 5 مباريات)
/// - متوسط الأهداف
/// - H2H (تاريخ المواجهات المباشرة)
/// - مخرجات Dixon-Coles وElo
/// </summary>
public class FeatureEngineeringService : IFeatureEngineeringService
{
    private readonly SqliteDbContext _db;
    private readonly PoissonDixonColes _dixonColes;
    private readonly EloRating _elo;

    public FeatureEngineeringService(
        SqliteDbContext db,
        PoissonDixonColes dixonColes,
        EloRating elo)
    {
        _db = db;
        _dixonColes = dixonColes;
        _elo = elo;
    }

    public async Task<MatchFeatures> ComputeAsync(
        int homeTeamId, int awayTeamId,
        DateTime matchDate,
        CancellationToken ct = default)
    {
        var homeTeam = await _db.Teams.FindAsync([homeTeamId], ct)
            ?? throw new ArgumentException($"الفريق {homeTeamId} غير موجود.");
        var awayTeam = await _db.Teams.FindAsync([awayTeamId], ct)
            ?? throw new ArgumentException($"الفريق {awayTeamId} غير موجود.");

        // ── جلب آخر 10 مباريات لكل فريق قبل تاريخ المباراة ──
        var homeMatches = await GetRecentMatchesAsync(homeTeamId, matchDate, 10, ct);
        var awayMatches = await GetRecentMatchesAsync(awayTeamId, matchDate, 10, ct);
        var h2hMatches  = await GetH2HMatchesAsync(homeTeamId, awayTeamId, matchDate, 5, ct);

        var features = new MatchFeatures
        {
            HomeTeam  = homeTeam.Name,
            AwayTeam  = awayTeam.Name,
            MatchDate = matchDate,

            // ── راحة الفريق ──
            HomeDaysSinceLastMatch = ComputeDaysSinceLastMatch(homeMatches, matchDate),
            AwayDaysSinceLastMatch = ComputeDaysSinceLastMatch(awayMatches, matchDate),

            // ── شكل الفريق (آخر 5) ──
            HomeFormLast5 = ComputeForm(homeMatches.Take(5).ToList(), homeTeamId),
            AwayFormLast5 = ComputeForm(awayMatches.Take(5).ToList(), awayTeamId),

            // ── متوسط الأهداف (آخر 5) ──
            HomeAvgGoalsScored    = ComputeAvgGoalsScored(homeMatches.Take(5).ToList(), homeTeamId),
            HomeAvgGoalsConceded  = ComputeAvgGoalsConceded(homeMatches.Take(5).ToList(), homeTeamId),
            AwayAvgGoalsScored    = ComputeAvgGoalsScored(awayMatches.Take(5).ToList(), awayTeamId),
            AwayAvgGoalsConceded  = ComputeAvgGoalsConceded(awayMatches.Take(5).ToList(), awayTeamId),

            // ── H2H ──
            H2HHomeWinRate    = ComputeH2HWinRate(h2hMatches, homeTeamId),
            H2HDrawRate       = ComputeH2HDrawRate(h2hMatches),
            H2HAvgTotalGoals  = ComputeH2HAvgGoals(h2hMatches),

            // ── Elo ──
            EloHome = (float)_elo.GetRating(homeTeam.Name),
            EloAway = (float)_elo.GetRating(awayTeam.Name),
            EloDiff = (float)(_elo.GetRating(homeTeam.Name) - _elo.GetRating(awayTeam.Name)),

            // ── جودة البيانات ──
            DataQuality = ComputeDataQuality(homeMatches, awayMatches),
        };

        // ── مخرجات Dixon-Coles (إذا مُدرَّب) ──
        if (_dixonColes.IsTrained &&
            _dixonColes.AttackParams.ContainsKey(homeTeam.Name) &&
            _dixonColes.AttackParams.ContainsKey(awayTeam.Name))
        {
            var (lH, lA) = _dixonColes.ComputeLambdas(homeTeam.Name, awayTeam.Name);
            features.DixonColesLambdaHome = (float)lH;
            features.DixonColesLambdaAway = (float)lA;
        }

        return features;
    }

    // ────────────────────────────────────────────────────────────
    // دوال الحساب المساعدة
    // ────────────────────────────────────────────────────────────

    private async Task<List<Domain.Entities.Match>> GetRecentMatchesAsync(
        int teamId, DateTime before, int count, CancellationToken ct)
    {
        return await _db.Matches
            .AsNoTracking()
            .Where(m => (m.HomeTeamId == teamId || m.AwayTeamId == teamId)
                     && m.MatchDate < before
                     && m.HomeGoals != null)
            .OrderByDescending(m => m.MatchDate)
            .Take(count)
            .ToListAsync(ct);
    }

    private async Task<List<Domain.Entities.Match>> GetH2HMatchesAsync(
        int homeId, int awayId, DateTime before, int count, CancellationToken ct)
    {
        return await _db.Matches
            .AsNoTracking()
            .Where(m => ((m.HomeTeamId == homeId && m.AwayTeamId == awayId) ||
                         (m.HomeTeamId == awayId && m.AwayTeamId == homeId))
                     && m.MatchDate < before
                     && m.HomeGoals != null)
            .OrderByDescending(m => m.MatchDate)
            .Take(count)
            .ToListAsync(ct);
    }

    private static float ComputeDaysSinceLastMatch(
        List<Domain.Entities.Match> matches, DateTime matchDate)
    {
        if (matches.Count == 0) return 30f; // افتراضي: 30 يوم
        var days = (matchDate - matches[0].MatchDate).TotalDays;
        return Math.Clamp((float)days, 1f, 60f);
    }

    private static float ComputeForm(
        List<Domain.Entities.Match> matches, int teamId)
    {
        if (matches.Count == 0) return 0.5f; // محايد
        float points = 0;
        foreach (var m in matches)
        {
            bool isHome = m.HomeTeamId == teamId;
            int scored   = isHome ? m.HomeGoals!.Value : m.AwayGoals!.Value;
            int conceded = isHome ? m.AwayGoals!.Value : m.HomeGoals!.Value;

            if (scored > conceded) points += 3;
            else if (scored == conceded) points += 1;
        }
        // تطبيع: أقصى نقاط = 5*3 = 15
        return Math.Clamp(points / 15f, 0f, 1f);
    }

    private static float ComputeAvgGoalsScored(
        List<Domain.Entities.Match> matches, int teamId)
    {
        if (matches.Count == 0) return 1.5f;
        var goals = matches.Select(m =>
            m.HomeTeamId == teamId ? m.HomeGoals!.Value : m.AwayGoals!.Value);
        return (float)goals.Average();
    }

    private static float ComputeAvgGoalsConceded(
        List<Domain.Entities.Match> matches, int teamId)
    {
        if (matches.Count == 0) return 1.5f;
        var goals = matches.Select(m =>
            m.HomeTeamId == teamId ? m.AwayGoals!.Value : m.HomeGoals!.Value);
        return (float)goals.Average();
    }

    private static float ComputeH2HWinRate(
        List<Domain.Entities.Match> h2h, int homeTeamId)
    {
        if (h2h.Count == 0) return 0.33f;
        var wins = h2h.Count(m =>
            (m.HomeTeamId == homeTeamId && m.HomeGoals > m.AwayGoals) ||
            (m.AwayTeamId == homeTeamId && m.AwayGoals > m.HomeGoals));
        return (float)wins / h2h.Count;
    }

    private static float ComputeH2HDrawRate(List<Domain.Entities.Match> h2h)
    {
        if (h2h.Count == 0) return 0.28f;
        return (float)h2h.Count(m => m.HomeGoals == m.AwayGoals) / h2h.Count;
    }

    private static float ComputeH2HAvgGoals(List<Domain.Entities.Match> h2h)
    {
        if (h2h.Count == 0) return 2.5f;
        return (float)h2h.Average(m => m.HomeGoals!.Value + m.AwayGoals!.Value);
    }

    private static float ComputeDataQuality(
        List<Domain.Entities.Match> home,
        List<Domain.Entities.Match> away)
    {
        // كلما كان عدد المباريات التاريخية أكبر = جودة أعلى
        var hQ = Math.Min(home.Count / 10.0f, 1.0f);
        var aQ = Math.Min(away.Count / 10.0f, 1.0f);
        return (hQ + aQ) / 2f;
    }
}

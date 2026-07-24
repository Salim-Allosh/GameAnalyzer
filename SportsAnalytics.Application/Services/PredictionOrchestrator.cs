using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SportsAnalytics.Application.Services;
using SportsAnalytics.Domain.Interfaces;
using SportsAnalytics.Domain.Models;
using SportsAnalytics.Infrastructure.Data;
using SportsAnalytics.MathEngine;

namespace SportsAnalytics.Application.Services;

/// <summary>
/// المنسّق المركزي — يستدعي كل الخدمات بالترتيب الصحيح:
///
///  1. FeatureEngineeringService  → MatchFeatures
///  2. PoissonDixonColes          → DC Probabilities + λ
///  3. EloRating                  → Elo Probabilities
///  4. MonteCarloSimulator        → Score Distribution
///  5. MLMatchPredictor           → ML Probabilities
///  6. Blend                      → Weighted Ensemble
///  7. RiskScoringService         → Risk Score + Explanation
///
/// الواجهة (WPF) لا تعرف شيئاً عن الخطوات الداخلية.
/// </summary>
public class PredictionOrchestrator : IPredictionOrchestrator
{
    private readonly SqliteDbContext           _db;
    private readonly IFeatureEngineeringService _features;
    private readonly PoissonDixonColes         _dixonColes;
    private readonly EloRating                 _elo;
    private readonly MonteCarloSimulator       _monteCarlo;
    private readonly MLMatchPredictor          _mlPredictor;
    private readonly IRiskScoringService       _riskScoring;
    private readonly BettingMarketsCalculator  _bettingMarketsCalculator;

    public PredictionOrchestrator(
        SqliteDbContext           db,
        IFeatureEngineeringService features,
        PoissonDixonColes         dixonColes,
        EloRating                 elo,
        MonteCarloSimulator       monteCarlo,
        MLMatchPredictor          mlPredictor,
        IRiskScoringService       riskScoring,
        BettingMarketsCalculator  bettingMarketsCalculator)
    {
        _db          = db;
        _features    = features;
        _dixonColes  = dixonColes;
        _elo         = elo;
        _monteCarlo  = monteCarlo;
        _mlPredictor = mlPredictor;
        _riskScoring = riskScoring;
        _bettingMarketsCalculator = bettingMarketsCalculator;
    }

    public async Task<AnalysisReport> AnalyzeAsync(
        int homeTeamId, int awayTeamId,
        DateTime matchDate,
        double? homeOdds = null,
        double? drawOdds = null,
        double? awayOdds = null,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        // ── جلب بيانات الفريقين ──
        var homeTeam = await _db.Teams.AsNoTracking().FirstAsync(t => t.Id == homeTeamId, ct);
        var awayTeam = await _db.Teams.AsNoTracking().FirstAsync(t => t.Id == awayTeamId, ct);

        var report = new AnalysisReport
        {
            HomeTeamId = homeTeamId,
            AwayTeamId = awayTeamId,
            HomeTeam   = homeTeam.Name,
            AwayTeam   = awayTeam.Name,
            MatchDate  = matchDate,
            League     = homeTeam.League,
        };

        // ── 1. Feature Engineering ──
        report.Features = await _features.ComputeAsync(homeTeamId, awayTeamId, matchDate, ct);

        // ── 2. Dixon-Coles ──
        if (_dixonColes.IsTrained &&
            _dixonColes.AttackParams.ContainsKey(homeTeam.Name) &&
            _dixonColes.AttackParams.ContainsKey(awayTeam.Name))
        {
            var (lH, lA) = _dixonColes.ComputeLambdas(homeTeam.Name, awayTeam.Name);
            report.LambdaHome = lH;
            report.LambdaAway = lA;

            var dc = _dixonColes.ComputeOutcomeProbabilities(homeTeam.Name, awayTeam.Name);
            report.DcHomeWin = dc.HomeWin;
            report.DcDraw    = dc.Draw;
            report.DcAwayWin = dc.AwayWin;

            // ── 4. Monte Carlo ──
            var mcResult = _monteCarlo.Simulate(lH, lA);
            report.McHomeWin   = mcResult.HomeWinProbability;
            report.McDraw      = mcResult.DrawProbability;
            report.McAwayWin   = mcResult.AwayWinProbability;
            report.McIterations = mcResult.TotalIterations;
            report.McStdError  = mcResult.StandardError;
            report.TopScores   = mcResult.TopScores;

            // ── Betting Markets ──
            report.BettingMarkets = _bettingMarketsCalculator.CalculateMarkets(mcResult, homeTeam.Name, awayTeam.Name);
        }
        else
        {
            // Fallback بدون تدريب
            report.DcHomeWin = report.McHomeWin = 1.0 / 3;
            report.DcDraw    = report.McDraw    = 1.0 / 3;
            report.DcAwayWin = report.McAwayWin = 1.0 / 3;
        }

        // ── 3. Elo ──
        report.EloRatingHome = _elo.GetRating(homeTeam.Name);
        report.EloRatingAway = _elo.GetRating(awayTeam.Name);
        var eloOut = _elo.ComputeOutcomeProbabilities(homeTeam.Name, awayTeam.Name);
        report.EloHomeWin = eloOut.HomeWin;
        report.EloDraw    = eloOut.Draw;
        report.EloAwayWin = eloOut.AwayWin;

        // ── 5. ML.NET ──
        if (_mlPredictor.IsTrained)
        {
            var ml = _mlPredictor.Predict(report.Features);
            report.MlHomeWin = ml.HomeWin;
            report.MlDraw    = ml.Draw;
            report.MlAwayWin = ml.AwayWin;
        }
        else
        {
            report.MlHomeWin = report.DcHomeWin;
            report.MlDraw    = report.DcDraw;
            report.MlAwayWin = report.DcAwayWin;
        }

        // ── 6. Blend (DC × ML) ──
        var blended = MLMatchPredictor.Blend(
            (report.DcHomeWin, report.DcDraw, report.DcAwayWin),
            (report.MlHomeWin, report.MlDraw, report.MlAwayWin),
            report.BlendAlpha);

        report.BlendHomeWin = blended.HomeWin;
        report.BlendDraw    = blended.Draw;
        report.BlendAwayWin = blended.AwayWin;

        // ── 7. Risk Scoring ──
        report.Risk = _riskScoring.Compute(
            homeTeam.Name, awayTeam.Name, matchDate,
            report.BlendHomeWin, report.BlendDraw, report.BlendAwayWin,
            report.Features.DataQuality,
            homeOdds, drawOdds, awayOdds);

        sw.Stop();
        report.ComputationMs = sw.Elapsed.TotalMilliseconds;
        return report;
    }
}

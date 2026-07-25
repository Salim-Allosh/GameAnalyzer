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
        IProgress<string>? statusProgress = null,
        IProgress<int>? percentProgress = null,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        statusProgress?.Report("جلب بيانات الفريقين...");
        percentProgress?.Report(10);
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

        statusProgress?.Report("حساب ميزات المباراة (Feature Engineering)...");
        percentProgress?.Report(20);
        // ── 1. Feature Engineering ──
        report.Features = await _features.ComputeAsync(homeTeamId, awayTeamId, matchDate, ct);

        statusProgress?.Report("حساب احتمالات Dixon-Coles...");
        percentProgress?.Report(40);
        // ── 2. Dixon-Coles ──
        if (_dixonColes.IsTrained &&
            _dixonColes.AttackParams.ContainsKey(homeTeam.Name) &&
            _dixonColes.AttackParams.ContainsKey(awayTeam.Name))
        {
            var (lH, lA) = _dixonColes.ComputeLambdas(homeTeam.Name, awayTeam.Name);
            report.LambdaHome = lH;
            report.LambdaAway = lA;

            var dc = _dixonColes.ComputeOutcomeProbabilities(homeTeam.Name, awayTeam.Name);
            report.DcHomeWin = double.IsNaN(dc.HomeWin) ? 1.0 / 3 : dc.HomeWin;
            report.DcDraw    = double.IsNaN(dc.Draw) ? 1.0 / 3 : dc.Draw;
            report.DcAwayWin = double.IsNaN(dc.AwayWin) ? 1.0 / 3 : dc.AwayWin;

            var exactGrid = _dixonColes.ComputeExactGrid(homeTeam.Name, awayTeam.Name);
            report.ExactProbabilityMatrix = exactGrid;

            var topScores = new List<(int Home, int Away, double Prob)>();
            for (int h = 0; h < exactGrid.GetLength(0); h++)
            {
                for (int a = 0; a < exactGrid.GetLength(1); a++)
                {
                    topScores.Add((h, a, exactGrid[h, a]));
                }
            }
            report.TopScores = topScores.OrderByDescending(x => x.Prob).Take(10).ToList();

            statusProgress?.Report("تشغيل محاكاة Monte Carlo...");
            percentProgress?.Report(60);
            // ── 4. Monte Carlo ──
            var mcResult = _monteCarlo.Simulate(lH, lA);
            report.McHomeWin   = double.IsNaN(mcResult.HomeWinProbability) ? 1.0 / 3 : mcResult.HomeWinProbability;
            report.McDraw      = double.IsNaN(mcResult.DrawProbability) ? 1.0 / 3 : mcResult.DrawProbability;
            report.McAwayWin   = double.IsNaN(mcResult.AwayWinProbability) ? 1.0 / 3 : mcResult.AwayWinProbability;
            report.McIterations = mcResult.TotalIterations;
            report.McStdError  = double.IsNaN(mcResult.StandardError) ? 0 : mcResult.StandardError;

            // ── Betting Markets ──
            report.BettingMarkets = _bettingMarketsCalculator.CalculateMarkets(exactGrid, homeTeam.Name, awayTeam.Name);
        }
        else
        {
            // Fallback: استخدم قيم Lambda افتراضية (معدل الدوري) حتى يمكن توليد أسواق الرهان والأرقام التقديرية
            double lH = 1.4;
            double lA = 1.0;
            report.LambdaHome = lH;
            report.LambdaAway = lA;
            
            var exactGrid = PoissonDixonColes.ComputeOutcomesFromLambdas(lH, lA, -0.1);
            report.ExactProbabilityMatrix = exactGrid;
            
            var topScores = new List<(int Home, int Away, double Prob)>();
            for (int h = 0; h < exactGrid.GetLength(0); h++)
            {
                for (int a = 0; a < exactGrid.GetLength(1); a++)
                {
                    topScores.Add((h, a, exactGrid[h, a]));
                }
            }
            report.TopScores = topScores.OrderByDescending(x => x.Prob).Take(10).ToList();
            
            double hW = 0, dr = 0, aW = 0;
            for (int h = 0; h < exactGrid.GetLength(0); h++)
            {
                for (int a = 0; a < exactGrid.GetLength(1); a++)
                {
                    if (h > a) hW += exactGrid[h, a];
                    else if (h == a) dr += exactGrid[h, a];
                    else aW += exactGrid[h, a];
                }
            }
            report.DcHomeWin = report.McHomeWin = hW;
            report.DcDraw    = report.McDraw    = dr;
            report.DcAwayWin = report.McAwayWin = aW;
            
            report.BettingMarkets = _bettingMarketsCalculator.CalculateMarkets(exactGrid, homeTeam.Name, awayTeam.Name);
        }

        statusProgress?.Report("حساب تصنيف Elo...");
        percentProgress?.Report(75);
        // ── 3. Elo ──
        report.EloRatingHome = _elo.GetRating(homeTeam.Name);
        report.EloRatingAway = _elo.GetRating(awayTeam.Name);
        var eloOut = _elo.ComputeOutcomeProbabilities(homeTeam.Name, awayTeam.Name);
        report.EloHomeWin = eloOut.HomeWin;
        report.EloDraw    = eloOut.Draw;
        report.EloAwayWin = eloOut.AwayWin;

        statusProgress?.Report("تطبيق نماذج تعلم الآلة (ML.NET)...");
        percentProgress?.Report(85);
        // ── 5. ML.NET ──
        if (_mlPredictor.IsTrained)
        {
            var ml = _mlPredictor.Predict(report.Features);
            report.MlHomeWin = double.IsNaN(ml.HomeWin) ? report.DcHomeWin : ml.HomeWin;
            report.MlDraw    = double.IsNaN(ml.Draw) ? report.DcDraw : ml.Draw;
            report.MlAwayWin = double.IsNaN(ml.AwayWin) ? report.DcAwayWin : ml.AwayWin;
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

        double sumBlend = blended.HomeWin + blended.Draw + blended.AwayWin;
        if (double.IsNaN(sumBlend) || sumBlend <= 0)
        {
            report.BlendHomeWin = 1.0 / 3;
            report.BlendDraw    = 1.0 / 3;
            report.BlendAwayWin = 1.0 / 3;
        }
        else
        {
            report.BlendHomeWin = blended.HomeWin / sumBlend;
            report.BlendDraw    = blended.Draw / sumBlend;
            report.BlendAwayWin = blended.AwayWin / sumBlend;
        }

        statusProgress?.Report("حساب مؤشر المخاطرة...");
        percentProgress?.Report(95);
        // ── 7. Risk Scoring ──
        report.Risk = _riskScoring.Compute(
            homeTeam.Name, awayTeam.Name, matchDate,
            report.BlendHomeWin, report.BlendDraw, report.BlendAwayWin,
            report.Features.DataQuality,
            homeOdds, drawOdds, awayOdds);

        sw.Stop();
        report.ComputationMs = sw.Elapsed.TotalMilliseconds;
        
        statusProgress?.Report("اكتمل التحليل.");
        percentProgress?.Report(100);

        report.NumberExplanations = BuildNumberExplanations(report);

        try
        {
            SaveDetailedLog(report);

            // Save to Database for Archive
            var match = await _db.Matches.FirstOrDefaultAsync(m => 
                m.HomeTeamId == homeTeamId && 
                m.AwayTeamId == awayTeamId && 
                m.MatchDate.Date == matchDate.Date, ct);
            
            if (match == null)
            {
                match = new SportsAnalytics.Domain.Entities.Match
                {
                    HomeTeamId = homeTeamId,
                    AwayTeamId = awayTeamId,
                    MatchDate = matchDate,
                    League = homeTeam.League ?? "Unknown",
                    Season = matchDate.Year.ToString()
                };
                _db.Matches.Add(match);
                await _db.SaveChangesAsync(ct);
            }
            else
            {
                if (match.HomeGoals.HasValue && match.AwayGoals.HasValue)
                {
                    report.ActualHomeGoals = match.HomeGoals.Value;
                    report.ActualAwayGoals = match.AwayGoals.Value;
                }
            }

            var prediction = new SportsAnalytics.Domain.Entities.Prediction
            {
                MatchId = match.Id,
                HomeWinProbability = report.BlendHomeWin,
                DrawProbability = report.BlendDraw,
                AwayWinProbability = report.BlendAwayWin,
                LambdaHome = report.LambdaHome,
                LambdaAway = report.LambdaAway,
                RiskScore = report.Risk.RiskScore,
                ModelVersion = "LightGBM_Ensemble_1.0",
                CreatedAt = DateTime.UtcNow,
                IsCompleted = false
            };
            
            _db.Predictions.Add(prediction);
            await _db.SaveChangesAsync(ct);
        }
        catch { /* تجاهل الأخطاء لعدم إيقاف التحليل */ }

        return report;
    }

    private void SaveDetailedLog(AnalysisReport r)
    {
        var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        Directory.CreateDirectory(logDir);
        
        var fileName = $"Analysis_{r.HomeTeam}_vs_{r.AwayTeam}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        var path = Path.Combine(logDir, fileName);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("==================================================");
        sb.AppendLine($"تفاصيل تحليل مباراة: {r.HomeTeam} ضد {r.AwayTeam}");
        sb.AppendLine($"التاريخ: {r.MatchDate:yyyy-MM-dd}");
        sb.AppendLine($"الدوري: {r.League}");
        sb.AppendLine($"وقت التحليل: {r.GeneratedAt}");
        sb.AppendLine($"زمن الحساب: {r.ComputationMs:F2} ms");
        sb.AppendLine("==================================================");
        sb.AppendLine();
        
        sb.AppendLine("[1] هندسة الميزات (Feature Engineering)");
        sb.AppendLine($"  - أيام الراحة: المضيف ({r.Features.HomeDaysSinceLastMatch:F1} أيام) | الضيف ({r.Features.AwayDaysSinceLastMatch:F1} أيام)");
        sb.AppendLine($"  - الفورمة (آخر 5): المضيف ({r.Features.HomeFormLast5:P1}) | الضيف ({r.Features.AwayFormLast5:P1})");
        sb.AppendLine($"  - أهداف مسجلة (متوسط): المضيف ({r.Features.HomeAvgGoalsScored:F2}) | الضيف ({r.Features.AwayAvgGoalsScored:F2})");
        sb.AppendLine($"  - أهداف مستقبلة (متوسط): المضيف ({r.Features.HomeAvgGoalsConceded:F2}) | الضيف ({r.Features.AwayAvgGoalsConceded:F2})");
        sb.AppendLine($"  - المواجهات المباشرة (H2H): نسبة فوز المضيف ({r.Features.H2HHomeWinRate:P1}) | التعادل ({r.Features.H2HDrawRate:P1})");
        sb.AppendLine($"  - جودة البيانات التاريخية: {r.Features.DataQuality:P1}");
        sb.AppendLine();

        sb.AppendLine("[2] نموذج Poisson / Dixon-Coles");
        sb.AppendLine($"  - قوة الهجوم المتوقعة (Lambda): المضيف = {r.LambdaHome:F3} | الضيف = {r.LambdaAway:F3}");
        sb.AppendLine($"  - الاحتمالات المستنتجة: فوز المضيف ({r.DcHomeWin:P2}) | تعادل ({r.DcDraw:P2}) | فوز الضيف ({r.DcAwayWin:P2})");
        sb.AppendLine();

        sb.AppendLine("[3] نموذج Elo Rating");
        sb.AppendLine($"  - تصنيف Elo الحالي: المضيف ({r.EloRatingHome:F1}) | الضيف ({r.EloRatingAway:F1})");
        sb.AppendLine($"  - الاحتمالات المستنتجة: فوز المضيف ({r.EloHomeWin:P2}) | تعادل ({r.EloDraw:P2}) | فوز الضيف ({r.EloAwayWin:P2})");
        sb.AppendLine();

        sb.AppendLine("[4] محاكاة Monte Carlo");
        sb.AppendLine($"  - عدد التكرارات: {r.McIterations:N0}");
        sb.AppendLine($"  - هامش الخطأ القياسي: {r.McStdError:F5}");
        sb.AppendLine($"  - الاحتمالات المستنتجة: فوز المضيف ({r.McHomeWin:P2}) | تعادل ({r.McDraw:P2}) | فوز الضيف ({r.McAwayWin:P2})");
        sb.AppendLine("  - النتائج الأكثر تكراراً:");
        foreach (var score in r.TopScores.Take(5))
            sb.AppendLine($"      * {score.Home}-{score.Away} ({score.Prob:P2})");
        sb.AppendLine();

        sb.AppendLine("[5] تعلم الآلة (ML.NET)");
        sb.AppendLine($"  - الاحتمالات المستنتجة: فوز المضيف ({r.MlHomeWin:P2}) | تعادل ({r.MlDraw:P2}) | فوز الضيف ({r.MlAwayWin:P2})");
        sb.AppendLine();

        sb.AppendLine("[6] الدمج النهائي (Blend Ensembling)");
        sb.AppendLine($"  - معامل الدمج (Alpha = ML vs Stat): {r.BlendAlpha:F2}");
        sb.AppendLine($"  - الاحتمالات النهائية: فوز المضيف ({r.BlendHomeWin:P2}) | تعادل ({r.BlendDraw:P2}) | فوز الضيف ({r.BlendAwayWin:P2})");
        sb.AppendLine();

        sb.AppendLine("[7] تقييم المخاطرة (Risk Scoring)");
        sb.AppendLine($"  - النتيجة (Risk Score): {r.Risk.RiskScore:F1} / 100");
        sb.AppendLine($"  - مستوى المخاطرة: {r.Risk.RiskLevel}");
        sb.AppendLine("  - المبررات والملاحظات:");
        sb.AppendLine($"      * {r.Risk.Explanation}");
        sb.AppendLine();

        sb.AppendLine("[8] أسواق الرهان المقترحة (Betting Markets)");
        foreach (var m in r.BettingMarkets.OrderByDescending(x => x.Probability))
            sb.AppendLine($"  - {m.MarketName}: الاحتمال ({m.Probability:P2}) | الخيار ({m.Selection})");
        sb.AppendLine();
        
        sb.AppendLine("==================================================");
        sb.AppendLine($"الخلاصة: {r.MostLikelyOutcome}");
        sb.AppendLine($"مؤشر الثقة العام: {r.ConfidenceScore:F1}%");
        sb.AppendLine("==================================================");

        File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
    }

    private static List<NumberExplanationItem> BuildNumberExplanations(AnalysisReport r)
    {
        var list = new List<NumberExplanationItem>();

        // 1. Blended Home Win
        list.Add(new NumberExplanationItem
        {
            NumberValue = $"{r.BlendHomeWin:P1}",
            TeamName = $"{r.HomeTeam} (المضيف)",
            MetricName = "الاحتمالية النهائية المدمجة - فوز المضيف",
            Meaning = "احتمالية فوز فريق المضيف بعد دمج نتائج كافة نماذج الذكاء الاصطناعي والإحصاء",
            SourceCalculation = $"دمج مرجّح (Alpha={r.BlendAlpha:P0}) بين نموذج Dixon-Coles المزدوج ونماذج ML.NET"
        });

        // 2. Blended Draw
        list.Add(new NumberExplanationItem
        {
            NumberValue = $"{r.BlendDraw:P1}",
            TeamName = "المباراة (تعادل)",
            MetricName = "الاحتمالية النهائية المدمجة - التعادل",
            Meaning = "احتمالية انتهاء المباراة بالتعادل بين الفريقين",
            SourceCalculation = "تجميع احتمالات التعادل (0-0, 1-1, 2-2...) عبر مصفوفة Dixon-Coles وتحسين ML.NET"
        });

        // 3. Blended Away Win
        list.Add(new NumberExplanationItem
        {
            NumberValue = $"{r.BlendAwayWin:P1}",
            TeamName = $"{r.AwayTeam} (الضيف)",
            MetricName = "الاحتمالية النهائية المدمجة - فوز الضيف",
            Meaning = "احتمالية فوز فريق الضيف بعد الدمج النهائي للنماذج",
            SourceCalculation = "دمج مرجّح بين نماذج Dixon-Coles ونموذج التعلم الآلي ML.NET"
        });

        // 4. Dixon-Coles Home Lambda
        list.Add(new NumberExplanationItem
        {
            NumberValue = $"{r.LambdaHome:F2}",
            TeamName = $"{r.HomeTeam} (المضيف)",
            MetricName = "معدل الأهداف المتوقعة λ (Lambda Home)",
            Meaning = "عدد الأهداف القادمة المتوقعة إحصائياً للمضيف بناءً على قوته الهجومية وضعف دفاع المنافس",
            SourceCalculation = $"معادلة exp(Atk_{r.HomeTeam} + Def_{r.AwayTeam} + HomeAdvantage)"
        });

        // 5. Dixon-Coles Away Lambda
        list.Add(new NumberExplanationItem
        {
            NumberValue = $"{r.LambdaAway:F2}",
            TeamName = $"{r.AwayTeam} (الضيف)",
            MetricName = "معدل الأهداف المتوقعة λ (Lambda Away)",
            Meaning = "عدد الأهداف المتوقعة للضيف بناءً على قوته الهجومية ودفاع المضيف",
            SourceCalculation = $"معادلة exp(Atk_{r.AwayTeam} + Def_{r.HomeTeam})"
        });

        // 6. Elo Rating Home
        list.Add(new NumberExplanationItem
        {
            NumberValue = $"{r.EloRatingHome:F0}",
            TeamName = $"{r.HomeTeam} (المضيف)",
            MetricName = "تصنيف Elo القوة الدولية",
            Meaning = "مؤشر القوة التراكمية للفريق بناءً على نتائج مبارياته التاريخية السابقة وحجم المنافسين",
            SourceCalculation = "خوارزمية Elo Rating (K-Factor = 32) المطبقة على نتائج آخر موسمين"
        });

        // 7. Elo Rating Away
        list.Add(new NumberExplanationItem
        {
            NumberValue = $"{r.EloRatingAway:F0}",
            TeamName = $"{r.AwayTeam} (الضيف)",
            MetricName = "تصنيف Elo القوة الدولية",
            Meaning = "مؤشر القوة التراكمية لفريق الضيف",
            SourceCalculation = "خوارزمية Elo Rating التراكمية بناءً على نتائج المباريات الرسمية"
        });

        // 8. Risk Score
        list.Add(new NumberExplanationItem
        {
            NumberValue = $"{r.Risk.RiskScore:F1} / 100",
            TeamName = "المباراة ككل",
            MetricName = "مؤشر المخاطرة (Risk Score)",
            Meaning = "درجة خطورة وعدم يقين التوقع (كلما ارتفع الرقم زادت درجة المخاطرة في الرهان)",
            SourceCalculation = "حساب انحراف Shannon Entropy + تشتت النماذج + تباين القيمة المتوقعة EV"
        });

        // 9. Data Quality
        list.Add(new NumberExplanationItem
        {
            NumberValue = $"{r.Features.DataQuality:P0}",
            TeamName = "قاعدة البيانات",
            MetricName = "جودة واكتمال البيانات (Data Quality)",
            Meaning = "نسبة اكتمال البيانات التاريخية والإحصائيات المتاحة للفريقين",
            SourceCalculation = "نسبة المباريات المكتملة المتوفرة للفريقين مقارنة بـ 10 مباريات سابقة لكل فريق"
        });

        return list;
    }
}

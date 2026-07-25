using Microsoft.ML;
using Microsoft.ML.Data;
using SportsAnalytics.Application.ML;
using SportsAnalytics.Domain.Models;

namespace SportsAnalytics.Application.Services;

/// <summary>
/// النموذج التكميلي — ML.NET Multiclass Classification.
/// يأخذ MatchFeatures كمدخل ويُنتج احتمالات (HomeWin/Draw/AwayWin).
///
/// الخوارزمية: SDCA (Stochastic Dual Coordinate Ascent) Multiclass
/// — دقيقة للبيانات الصغيرة ولا تحتاج GPU.
/// </summary>
public class MLMatchPredictor
{
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private PredictionEngine<MatchPredictionInput, MatchPredictionOutput>? _predEngine;

    public bool IsTrained => _model is not null;
    public double TrainAccuracy { get; private set; }
    public double TrainLogLoss { get; private set; }

    public MLMatchPredictor()
    {
        _mlContext = new MLContext(seed: 42);
    }

    /// <summary>
    /// يُدرّب النموذج من قائمة (MatchFeatures, outcome) التاريخية.
    /// outcome: 0=HomeWin, 1=Draw, 2=AwayWin
    /// </summary>
    public TrainingReport Train(
        IEnumerable<(MatchFeatures Features, int Outcome)> trainingData,
        IEnumerable<(MatchFeatures Features, int Outcome)>? validationData = null)
    {
        var rows = trainingData
            .Select(d => ToInput(d.Features, d.Outcome))
            .ToList();

        if (rows.Count < 5)
            return new TrainingReport(false, 0, 0, rows.Count,
                "بيانات غير كافية — يحتاج 5 مباريات على الأقل.");

        var trainView = _mlContext.Data.LoadFromEnumerable(rows);

        // ── Pipeline ──
        // 1. جمع كل الأعمدة في vector واحد
        var featureNames = new[]
        {
            "HomeDaysSinceLastMatch", "AwayDaysSinceLastMatch",
            "HomeFormLast5", "AwayFormLast5",
            "HomeAvgGoalsScored", "HomeAvgGoalsConceded",
            "AwayAvgGoalsScored", "AwayAvgGoalsConceded",
            "H2HHomeWinRate", "H2HDrawRate", "H2HAvgTotalGoals",
            "DixonColesLambdaHome", "DixonColesLambdaAway",
            "EloDiff"
        };

        var pipeline = _mlContext.Transforms
            .Concatenate("Features", featureNames)
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            // تحويل Label من UInt32 إلى Key<UInt32> — مطلوب لـ SDCA
            .Append(_mlContext.Transforms.Conversion
                .MapValueToKey("Label", "Label"))
            .Append(_mlContext.MulticlassClassification.Trainers
                .LightGbm(
                    labelColumnName: "Label",
                    featureColumnName: "Features",
                    numberOfLeaves: 31,
                    minimumExampleCountPerLeaf: 1))
            .Append(_mlContext.Transforms.Conversion
                .MapKeyToValue("PredictedLabel"));

        _model = pipeline.Fit(trainView);

        // ── تقييم على بيانات التدريب ──
        var trainPred = _model.Transform(trainView);
        var metrics = _mlContext.MulticlassClassification
            .Evaluate(trainPred, labelColumnName: "Label");

        TrainAccuracy = metrics.MacroAccuracy;
        TrainLogLoss  = metrics.LogLoss;

        // ── تقييم على بيانات التحقق (إذا وُجدت) ──
        double valAccuracy = 0, valLogLoss = 0;
        if (validationData is not null)
        {
            var valRows = validationData
                .Select(d => ToInput(d.Features, d.Outcome)).ToList();
            var valView = _mlContext.Data.LoadFromEnumerable(valRows);
            var valPred = _model.Transform(valView);
            var valMetrics = _mlContext.MulticlassClassification
                .Evaluate(valPred, labelColumnName: "Label");
            valAccuracy = valMetrics.MacroAccuracy;
            valLogLoss  = valMetrics.LogLoss;
        }

        // ── إنشاء Prediction Engine ──
        _predEngine = _mlContext.Model
            .CreatePredictionEngine<MatchPredictionInput, MatchPredictionOutput>(_model);

        return new TrainingReport(
            true, TrainAccuracy, valAccuracy,
            rows.Count,
            $"✅ SDCA مُدرَّب | دقة التدريب: {TrainAccuracy:P1} | Log-Loss: {TrainLogLoss:F4}");
    }

    /// <summary>يُنتج احتمالات من MatchFeatures.</summary>
    public (double HomeWin, double Draw, double AwayWin) Predict(MatchFeatures features)
    {
        if (_predEngine is null)
            throw new InvalidOperationException("النموذج لم يُدرَّب بعد.");

        var input = ToInput(features, 0);
        var output = _predEngine.Predict(input);

        // تطبيع لضمان المجموع = 1
        var total = output.HomeWinProb + output.DrawProb + output.AwayWinProb;
        if (total <= 0) return (0.33, 0.34, 0.33);

        return (output.HomeWinProb / total,
                output.DrawProb / total,
                output.AwayWinProb / total);
    }

    /// <summary>
    /// يدمج ML.NET مع Dixon-Coles بوزن تكيفي α.
    /// α يعكس الثقة النسبية بكل نموذج (مبنية على Brier Score التاريخي).
    /// α=0 → Dixon-Coles فقط، α=1 → ML.NET فقط.
    /// </summary>
    public static (double HomeWin, double Draw, double AwayWin) Blend(
        (double H, double D, double A) dixonColes,
        (double H, double D, double A) mlNet,
        double alpha = 0.3)
    {
        alpha = Math.Clamp(alpha, 0.0, 1.0);
        return (
            (1 - alpha) * dixonColes.H + alpha * mlNet.H,
            (1 - alpha) * dixonColes.D + alpha * mlNet.D,
            (1 - alpha) * dixonColes.A + alpha * mlNet.A
        );
    }

    /// <summary>يحسب Brier Score لنموذج ML.NET على بيانات اختبار.</summary>
    public double ComputeBrierScore(
        IEnumerable<(MatchFeatures Features, int Outcome)> testData)
    {
        if (_predEngine is null) return double.NaN;

        double total = 0;
        int count = 0;

        foreach (var (f, outcome) in testData)
        {
            var (pH, pD, pA) = Predict(f);
            double aH = outcome == 0 ? 1.0 : 0.0;
            double aD = outcome == 1 ? 1.0 : 0.0;
            double aA = outcome == 2 ? 1.0 : 0.0;
            total += Math.Pow(pH - aH, 2) + Math.Pow(pD - aD, 2) + Math.Pow(pA - aA, 2);
            count++;
        }

        return count > 0 ? total / count : double.NaN;
    }

    /// <summary>حفظ النموذج على القرص للاستخدام اللاحق.</summary>
    public void SaveModel(string path)
    {
        if (_model is null) throw new InvalidOperationException("لا يوجد نموذج للحفظ.");
        var schema = _mlContext.Data
            .LoadFromEnumerable(new List<MatchPredictionInput>()).Schema;
        _mlContext.Model.Save(_model, schema, path);
    }

    /// <summary>تحميل نموذج محفوظ.</summary>
    public void LoadModel(string path)
    {
        _model = _mlContext.Model.Load(path, out _);
        _predEngine = _mlContext.Model
            .CreatePredictionEngine<MatchPredictionInput, MatchPredictionOutput>(_model);
    }

    // ── دوال مساعدة ──

    private static MatchPredictionInput ToInput(MatchFeatures f, int outcome) =>
        new()
        {
            HomeDaysSinceLastMatch = f.HomeDaysSinceLastMatch,
            AwayDaysSinceLastMatch = f.AwayDaysSinceLastMatch,
            HomeFormLast5          = f.HomeFormLast5,
            AwayFormLast5          = f.AwayFormLast5,
            HomeAvgGoalsScored     = f.HomeAvgGoalsScored,
            HomeAvgGoalsConceded   = f.HomeAvgGoalsConceded,
            AwayAvgGoalsScored     = f.AwayAvgGoalsScored,
            AwayAvgGoalsConceded   = f.AwayAvgGoalsConceded,
            H2HHomeWinRate         = f.H2HHomeWinRate,
            H2HDrawRate            = f.H2HDrawRate,
            H2HAvgTotalGoals       = f.H2HAvgTotalGoals,
            DixonColesLambdaHome   = f.DixonColesLambdaHome,
            DixonColesLambdaAway   = f.DixonColesLambdaAway,
            EloDiff                = f.EloDiff,
            Label                  = (uint)outcome
        };
}

/// <summary>تقرير التدريب.</summary>
public record TrainingReport(
    bool Success,
    double TrainAccuracy,
    double ValidationAccuracy,
    int SamplesUsed,
    string Message);

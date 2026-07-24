using Microsoft.ML.Data;

namespace SportsAnalytics.Application.ML;

/// <summary>
/// مدخل نموذج ML.NET — يُبنى من MatchFeatures.
/// كل خاصية Single (float) لأن ML.NET يتطلب ذلك.
/// </summary>
public class MatchPredictionInput
{
    // ── راحة الفريق ──
    [ColumnName("HomeDaysSinceLastMatch")]
    public float HomeDaysSinceLastMatch { get; set; }

    [ColumnName("AwayDaysSinceLastMatch")]
    public float AwayDaysSinceLastMatch { get; set; }

    // ── شكل الفريق ──
    [ColumnName("HomeFormLast5")]
    public float HomeFormLast5 { get; set; }

    [ColumnName("AwayFormLast5")]
    public float AwayFormLast5 { get; set; }

    // ── متوسط الأهداف ──
    [ColumnName("HomeAvgGoalsScored")]
    public float HomeAvgGoalsScored { get; set; }

    [ColumnName("HomeAvgGoalsConceded")]
    public float HomeAvgGoalsConceded { get; set; }

    [ColumnName("AwayAvgGoalsScored")]
    public float AwayAvgGoalsScored { get; set; }

    [ColumnName("AwayAvgGoalsConceded")]
    public float AwayAvgGoalsConceded { get; set; }

    // ── H2H ──
    [ColumnName("H2HHomeWinRate")]
    public float H2HHomeWinRate { get; set; }

    [ColumnName("H2HDrawRate")]
    public float H2HDrawRate { get; set; }

    [ColumnName("H2HAvgTotalGoals")]
    public float H2HAvgTotalGoals { get; set; }

    // ── Dixon-Coles + Elo ──
    [ColumnName("DixonColesLambdaHome")]
    public float DixonColesLambdaHome { get; set; }

    [ColumnName("DixonColesLambdaAway")]
    public float DixonColesLambdaAway { get; set; }

    [ColumnName("EloDiff")]
    public float EloDiff { get; set; }

    // ── الهدف (0=HomeWin, 1=Draw, 2=AwayWin) ──
    [ColumnName("Label")]
    public uint Label { get; set; }
}

/// <summary>مخرج نموذج ML.NET.</summary>
public class MatchPredictionOutput
{
    [ColumnName("PredictedLabel")]
    public uint PredictedLabel { get; set; }

    [ColumnName("Score")]
    public float[] Score { get; set; } = [];

    /// <summary>احتمال فوز المضيف (Score[0]).</summary>
    public float HomeWinProb => Score.Length > 0 ? Score[0] : 0f;

    /// <summary>احتمال التعادل (Score[1]).</summary>
    public float DrawProb => Score.Length > 1 ? Score[1] : 0f;

    /// <summary>احتمال فوز الضيف (Score[2]).</summary>
    public float AwayWinProb => Score.Length > 2 ? Score[2] : 0f;
}

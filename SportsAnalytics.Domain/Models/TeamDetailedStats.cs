namespace SportsAnalytics.Domain.Models;

public class TeamDetailedStats
{
    public string TeamName { get; set; } = string.Empty;
    public int MatchesAnalyzed { get; set; }
    public string FormString { get; set; } = string.Empty; // e.g. "W D L W W"
    public int TotalTransfersImpact { get; set; }

    // النتائج والانتصارات
    public int WinsCount { get; set; }
    public int DrawsCount { get; set; }
    public int LossesCount { get; set; }
    public double WinPercentage => MatchesAnalyzed > 0 ? Math.Round((double)WinsCount / MatchesAnalyzed * 100, 1) : 0;

    // الأهداف المسجلة والمستقبلة
    public int TotalGoalsScored { get; set; }
    public double AvgGoalsScored => MatchesAnalyzed > 0 ? Math.Round((double)TotalGoalsScored / MatchesAnalyzed, 2) : 0;

    public int TotalGoalsConceded { get; set; }
    public double AvgGoalsConceded => MatchesAnalyzed > 0 ? Math.Round((double)TotalGoalsConceded / MatchesAnalyzed, 2) : 0;

    // الركنيات
    public int TotalCorners { get; set; }
    public double AvgCorners => MatchesAnalyzed > 0 ? Math.Round((double)TotalCorners / MatchesAnalyzed, 1) : 0;

    // البطاقات
    public int TotalYellowCards { get; set; }
    public double AvgYellowCards => MatchesAnalyzed > 0 ? Math.Round((double)TotalYellowCards / MatchesAnalyzed, 1) : 0;

    public int TotalRedCards { get; set; }
    public double AvgRedCards => MatchesAnalyzed > 0 ? Math.Round((double)TotalRedCards / MatchesAnalyzed, 2) : 0;

    // التسديدات والتسديدات على المرمى
    public int TotalShots { get; set; }
    public double AvgShots => MatchesAnalyzed > 0 ? Math.Round((double)TotalShots / MatchesAnalyzed, 1) : 0;

    public int TotalShotsOnTarget { get; set; }
    public double AvgShotsOnTarget => MatchesAnalyzed > 0 ? Math.Round((double)TotalShotsOnTarget / MatchesAnalyzed, 1) : 0;

    // الأخطاء
    public int TotalFouls { get; set; }
    public double AvgFouls => MatchesAnalyzed > 0 ? Math.Round((double)TotalFouls / MatchesAnalyzed, 1) : 0;

    // أسواق الرهان الخاصة
    public int Over15GoalsCount { get; set; }
    public double Over15Percentage => MatchesAnalyzed > 0 ? Math.Round((double)Over15GoalsCount / MatchesAnalyzed * 100, 1) : 0;

    public int Over25GoalsCount { get; set; }
    public double Over25Percentage => MatchesAnalyzed > 0 ? Math.Round((double)Over25GoalsCount / MatchesAnalyzed * 100, 1) : 0;

    public int BttsCount { get; set; }
    public double BttsPercentage => MatchesAnalyzed > 0 ? Math.Round((double)BttsCount / MatchesAnalyzed * 100, 1) : 0;

    public int CleanSheetsCount { get; set; }
    public double CleanSheetPercentage => MatchesAnalyzed > 0 ? Math.Round((double)CleanSheetsCount / MatchesAnalyzed * 100, 1) : 0;
}

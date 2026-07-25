namespace SportsAnalytics.Domain.Models;

public class TeamDetailedStats
{
    public string TeamName { get; set; } = string.Empty;
    public int MatchesAnalyzed { get; set; }
    
    // Betting axes
    public double AvgGoalsScored { get; set; }
    public double AvgGoalsConceded { get; set; }
    public double AvgCorners { get; set; }
    public double AvgYellowCards { get; set; }
    
    // Other infos
    public int TotalTransfersImpact { get; set; } // + means good transfers, - means bad
    public string FormString { get; set; } = string.Empty; // e.g. "W D L W W"
}

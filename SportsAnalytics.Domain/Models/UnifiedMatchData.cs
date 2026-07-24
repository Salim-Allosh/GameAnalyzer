namespace SportsAnalytics.Domain.Models;

public class UnifiedMatchData
{
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public DateTime MatchDate { get; set; }

    /// <summary>
    /// Average xG calculated from multiple sources (e.g., StatsBomb + Understat)
    /// </summary>
    public double HomeTeam_xG { get; set; }
    
    public double AwayTeam_xG { get; set; }

    /// <summary>
    /// Sources that contributed to this data (e.g., "Understat, StatsBomb")
    /// </summary>
    public List<string> DataSources { get; set; } = new();

    /// <summary>
    /// Specific unique metrics provided by single sources. 
    /// For example: "StatsBomb_ShotCoordinates", "FootballData_MaxOdds"
    /// </summary>
    public Dictionary<string, object> UniqueMetrics { get; set; } = new();
}

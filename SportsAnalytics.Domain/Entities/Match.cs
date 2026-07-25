namespace SportsAnalytics.Domain.Entities;

/// <summary>
/// يمثّل مباراة كرة القدم في النظام.
/// </summary>
public class Match
{
    public int Id { get; set; }
    public int HomeTeamId { get; set; }
    public int AwayTeamId { get; set; }
    public Team HomeTeam { get; set; } = null!;
    public Team AwayTeam { get; set; } = null!;
    public DateTime MatchDate { get; set; }
    public int? HomeGoals { get; set; }
    public int? AwayGoals { get; set; }
    public string League { get; set; } = string.Empty;
    public string Season { get; set; } = string.Empty;
    public MatchStatistics? Statistics { get; set; }
}

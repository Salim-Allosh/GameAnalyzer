namespace SportsAnalytics.Domain.Entities;

/// <summary>
/// يمثّل لاعب كرة قدم مرتبط بفريق معين.
/// </summary>
public class Player
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty; // GK, DEF, MID, FWD
    public bool IsInjured { get; set; } = false;
    public DateTime? InjuryUntil { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

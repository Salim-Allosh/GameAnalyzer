namespace SportsAnalytics.Domain.Entities;

/// <summary>
/// يمثّل فريق كرة القدم في النظام.
/// </summary>
public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string League { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

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

    public string LogoPath
    {
        get
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // Check output directory first (bin\Debug...)
            var outPath = System.IO.Path.Combine(baseDir, "Assets", "Logos", $"{Name}.png");
            if (System.IO.File.Exists(outPath)) return new Uri(outPath).AbsoluteUri;
            
            // Check project directory (where Python script saved them)
            var projPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "Assets", "Logos", $"{Name}.png"));
            if (System.IO.File.Exists(projPath)) return new Uri(projPath).AbsoluteUri;
            
            return string.Empty;
        }
    }
}

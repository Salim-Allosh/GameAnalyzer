using Microsoft.EntityFrameworkCore;
using SportsAnalytics.Domain.Entities;
using SportsAnalytics.Domain.Interfaces;
using SportsAnalytics.Infrastructure.Data;

namespace SportsAnalytics.Infrastructure.Connectors;

/// <summary>
/// يستورد بيانات المباريات التاريخية من ملف CSV.
/// صيغة الـ CSV المتوقعة:
/// HomeTeam,AwayTeam,Date,HomeGoals,AwayGoals,League,Season
/// </summary>
public class CsvDataConnector : IDataConnector
{
    private readonly SqliteDbContext _db;

    public string SourceName => "CSV File Importer";

    public CsvDataConnector(SqliteDbContext db) => _db = db;

    public async Task<DataIngestionResult> IngestAsync(string csvPath, CancellationToken ct = default)
    {
        if (!File.Exists(csvPath))
            return new DataIngestionResult(0, 0, 0, 1, $"الملف غير موجود: {csvPath}");

        var lines = await File.ReadAllLinesAsync(csvPath, ct);
        if (lines.Length < 2)
            return new DataIngestionResult(0, 0, 0, 1, "الملف فارغ أو لا يحتوي على بيانات.");

        // Cache الفرق الموجودة مسبقاً لتجنب استعلامات متكررة
        var teamCache = await _db.Teams
            .AsNoTracking()
            .ToDictionaryAsync(t => t.Name.ToLowerInvariant(), t => t.Id, ct);

        // تتبع المباريات المستوردة في هذه الجلسة لتجنب التكرار قبل الحفظ
        var sessionMatches = new HashSet<string>();

        int matchesImported = 0, teamsCreated = 0, skipped = 0, errors = 0;

        // تخطي الـ header
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var parts = line.Split(',');
                if (parts.Length < 7) { errors++; continue; }

                var homeTeamName = parts[0].Trim();
                var awayTeamName = parts[1].Trim();

                if (!DateTime.TryParse(parts[2].Trim(), out var matchDate)) { errors++; continue; }
                if (!int.TryParse(parts[3].Trim(), out var homeGoals)) { errors++; continue; }
                if (!int.TryParse(parts[4].Trim(), out var awayGoals)) { errors++; continue; }

                var league = parts[5].Trim();
                var season = parts[6].Trim();

                // جلب أو إنشاء الفريق المضيف
                var homeId = await GetOrCreateTeamAsync(homeTeamName, league, teamCache);
                if (homeId < 0) { teamsCreated++; homeId = Math.Abs(homeId); }

                // جلب أو إنشاء الفريق الضيف
                var awayId = await GetOrCreateTeamAsync(awayTeamName, league, teamCache);
                if (awayId < 0) { teamsCreated++; awayId = Math.Abs(awayId); }

                var matchKey = $"{homeId}_{awayId}_{matchDate:yyyy-MM-dd}";

                // التحقق من التكرار في الجلسة الحالية أو في قاعدة البيانات
                bool exists = sessionMatches.Contains(matchKey);
                if (!exists)
                {
                    exists = await _db.Matches.AnyAsync(m =>
                        m.HomeTeamId == homeId &&
                        m.AwayTeamId == awayId &&
                        m.MatchDate == matchDate, ct);
                }

                if (exists) { skipped++; continue; }

                sessionMatches.Add(matchKey);

                // إضافة المباراة
                _db.Matches.Add(new Match
                {
                    HomeTeamId = homeId,
                    AwayTeamId = awayId,
                    MatchDate = matchDate,
                    HomeGoals = homeGoals,
                    AwayGoals = awayGoals,
                    League = league,
                    Season = season
                });

                matchesImported++;

                // حفظ كل 20 سجل لتحسين الأداء
                if (matchesImported % 20 == 0)
                    await _db.SaveChangesAsync(ct);
            }
            catch
            {
                errors++;
            }
        }

        // حفظ ما تبقى
        await _db.SaveChangesAsync(ct);

        return new DataIngestionResult(
            matchesImported, teamsCreated, skipped, errors,
            $"✅ تم استيراد {matchesImported} مباراة، {teamsCreated} فريق جديد، {skipped} مكرر، {errors} خطأ.");
    }

    /// <summary>يُرجع Id الفريق (موجب إذا موجود، سالب إذا جديد).</summary>
    private async Task<int> GetOrCreateTeamAsync(
        string name, string league,
        Dictionary<string, int> cache)
    {
        var key = name.ToLowerInvariant();
        if (cache.TryGetValue(key, out var existingId))
            return existingId;

        // إنشاء فريق جديد
        var team = new Team
        {
            Name = name,
            League = league,
            Country = DetectCountry(league)
        };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        cache[key] = team.Id;
        return -team.Id; // سالب = جديد
    }

    private static string DetectCountry(string league) => league switch
    {
        "Premier League" => "England",
        "La Liga" => "Spain",
        "Bundesliga" => "Germany",
        "Serie A" => "Italy",
        "Ligue 1" => "France",
        _ => "Unknown"
    };
}

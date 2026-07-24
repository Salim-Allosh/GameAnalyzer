using Microsoft.EntityFrameworkCore;
using SportsAnalytics.Infrastructure.Connectors;
using SportsAnalytics.Infrastructure.Data;

namespace SportsAnalytics.Tests.Infrastructure;

/// <summary>
/// اختبارات وحدة لـ CsvDataConnector باستخدام DB InMemory.
/// </summary>
public class CsvDataConnectorTests : IDisposable
{
    private readonly SqliteDbContext _db;
    private readonly CsvDataConnector _connector;
    private readonly string _tempCsvPath;

    public CsvDataConnectorTests()
    {
        var options = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new SqliteDbContext(options);
        _connector = new CsvDataConnector(_db);

        // CSV مؤقت للاختبار
        _tempCsvPath = Path.GetTempFileName().Replace(".tmp", ".csv");
    }

    [Fact]
    public async Task IngestAsync_ValidFile_ImportsMatches()
    {
        WriteCsv(_tempCsvPath,
        [
            "HomeTeam,AwayTeam,Date,HomeGoals,AwayGoals,League,Season",
            "Arsenal,Chelsea,2023-01-14,2,1,Premier League,2022-2023",
            "Man City,Liverpool,2023-01-21,3,0,Premier League,2022-2023",
        ]);

        var result = await _connector.IngestAsync(_tempCsvPath);

        Assert.Equal(2, result.MatchesImported);
        Assert.Equal(0, result.Errors);
        Assert.Equal(2, await _db.Matches.CountAsync());
        Assert.True(await _db.Teams.CountAsync() >= 4);
    }

    [Fact]
    public async Task IngestAsync_DuplicateRows_SkipsDuplicates()
    {
        WriteCsv(_tempCsvPath,
        [
            "HomeTeam,AwayTeam,Date,HomeGoals,AwayGoals,League,Season",
            "Arsenal,Chelsea,2023-02-10,2,1,Test League,2022-2023",
            "Arsenal,Chelsea,2023-02-10,2,1,Test League,2022-2023", // تكرار
        ]);

        var result = await _connector.IngestAsync(_tempCsvPath);

        Assert.Equal(1, result.MatchesImported);
        Assert.Equal(1, await _db.Matches.CountAsync());
    }

    [Fact]
    public async Task IngestAsync_InvalidGoals_SkipsRow()
    {
        WriteCsv(_tempCsvPath,
        [
            "HomeTeam,AwayTeam,Date,HomeGoals,AwayGoals,League,Season",
            "Arsenal,Chelsea,2023-03-01,2,1,Test League,2022-2023",
            "Arsenal,Chelsea,2023-03-08,X,Y,Test League,2022-2023", // أرقام غير صحيحة
        ]);

        var result = await _connector.IngestAsync(_tempCsvPath);

        Assert.Equal(1, result.MatchesImported);
        Assert.Equal(1, result.Errors);
    }

    [Fact]
    public async Task IngestAsync_TeamCreatedOnce_NoDuplicateTeams()
    {
        WriteCsv(_tempCsvPath,
        [
            "HomeTeam,AwayTeam,Date,HomeGoals,AwayGoals,League,Season",
            "Arsenal,Chelsea,2023-04-01,1,0,Test League,2022-2023",
            "Chelsea,Arsenal,2023-04-08,2,2,Test League,2022-2023",
            "Arsenal,Man City,2023-04-15,1,2,Test League,2022-2023",
        ]);

        await _connector.IngestAsync(_tempCsvPath);

        // Arsenal و Chelsea و Man City — لا تكرار
        var teams = await _db.Teams.ToListAsync();
        var distinct = teams.GroupBy(t => t.Name).All(g => g.Count() == 1);
        Assert.True(distinct, "لا يجب أن تُكرَّر أسماء الفرق في قاعدة البيانات");
    }

    [Fact]
    public async Task IngestAsync_GoalsStoredCorrectly()
    {
        WriteCsv(_tempCsvPath,
        [
            "HomeTeam,AwayTeam,Date,HomeGoals,AwayGoals,League,Season",
            "Arsenal,Chelsea,2023-05-01,3,1,Test League,2022-2023",
        ]);

        await _connector.IngestAsync(_tempCsvPath);

        var match = await _db.Matches
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .FirstAsync();

        Assert.Equal(3, match.HomeGoals);
        Assert.Equal(1, match.AwayGoals);
        Assert.Equal("Arsenal", match.HomeTeam.Name);
        Assert.Equal("Chelsea", match.AwayTeam.Name);
    }

    // ── دوال مساعدة ──
    private static void WriteCsv(string path, string[] lines) =>
        File.WriteAllLines(path, lines);

    public void Dispose()
    {
        _db.Dispose();
        if (File.Exists(_tempCsvPath)) File.Delete(_tempCsvPath);
    }
}

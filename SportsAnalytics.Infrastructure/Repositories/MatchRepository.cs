using Microsoft.EntityFrameworkCore;
using SportsAnalytics.Domain.Entities;
using SportsAnalytics.Domain.Interfaces;
using SportsAnalytics.Infrastructure.Data;

namespace SportsAnalytics.Infrastructure.Repositories;

public class MatchRepository : IMatchRepository
{
    private readonly SqliteDbContext _db;

    public MatchRepository(SqliteDbContext db) => _db = db;

    public async Task<Match?> GetByIdAsync(int id)
        => await _db.Matches
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .FirstOrDefaultAsync(m => m.Id == id);

    public async Task<IEnumerable<Match>> GetByTeamsAsync(int homeTeamId, int awayTeamId)
        => await _db.Matches
                    .AsNoTracking()
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .Where(m => m.HomeTeamId == homeTeamId && m.AwayTeamId == awayTeamId)
                    .OrderByDescending(m => m.MatchDate)
                    .ToListAsync();

    public async Task<IEnumerable<Match>> GetRecentMatchesAsync(int teamId, int count)
        => await _db.Matches
                    .AsNoTracking()
                    .Include(m => m.HomeTeam)
                    .Include(m => m.AwayTeam)
                    .Where(m => m.HomeTeamId == teamId || m.AwayTeamId == teamId)
                    .OrderByDescending(m => m.MatchDate)
                    .Take(count)
                    .ToListAsync();

    public async Task AddAsync(Match match)
    {
        _db.Matches.Add(match);
        await _db.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Match> matches)
    {
        _db.Matches.AddRange(matches);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<Match>> GetAllMatchesAsync(int count = 50)
    {
        return await _db.Matches
            .AsNoTracking()
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .OrderByDescending(m => m.MatchDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<Team>> GetAllTeamsAsync()
    {
        return await _db.Teams
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync();
    }
}

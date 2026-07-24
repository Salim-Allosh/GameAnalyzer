using Microsoft.EntityFrameworkCore;
using SportsAnalytics.Domain.Entities;
using SportsAnalytics.Domain.Interfaces;
using SportsAnalytics.Infrastructure.Data;

namespace SportsAnalytics.Infrastructure.Repositories;

public class TeamRepository : ITeamRepository
{
    private readonly SqliteDbContext _db;

    public TeamRepository(SqliteDbContext db) => _db = db;

    public async Task<Team?> GetByIdAsync(int id)
        => await _db.Teams.FindAsync(id);

    public async Task<IEnumerable<Team>> GetAllAsync()
        => await _db.Teams.AsNoTracking().ToListAsync();

    public async Task AddAsync(Team team)
    {
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Team team)
    {
        _db.Teams.Update(team);
        await _db.SaveChangesAsync();
    }
}

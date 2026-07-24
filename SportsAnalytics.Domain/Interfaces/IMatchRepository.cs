using SportsAnalytics.Domain.Entities;

namespace SportsAnalytics.Domain.Interfaces;

/// <summary>
/// عقد الوصول لبيانات المباريات.
/// </summary>
public interface IMatchRepository
{
    Task<Match?> GetByIdAsync(int id);
    Task<IEnumerable<Match>> GetByTeamsAsync(int homeTeamId, int awayTeamId);
    Task<IEnumerable<Match>> GetRecentMatchesAsync(int teamId, int count);
    Task AddAsync(Match match);
    Task AddRangeAsync(IEnumerable<Match> matches);
    Task<IEnumerable<Match>> GetAllMatchesAsync(int count = 50);
}

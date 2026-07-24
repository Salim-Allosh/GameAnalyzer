using SportsAnalytics.Domain.Entities;

namespace SportsAnalytics.Domain.Interfaces;

/// <summary>
/// عقد الوصول لبيانات الفرق — يُنفَّذ في Infrastructure فقط.
/// </summary>
public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(int id);
    Task<IEnumerable<Team>> GetAllAsync();
    Task AddAsync(Team team);
    Task UpdateAsync(Team team);
}

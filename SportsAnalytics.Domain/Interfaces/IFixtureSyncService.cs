namespace SportsAnalytics.Domain.Interfaces;

public interface IFixtureSyncService
{
    Task<int> SyncUpcomingFixturesAsync(string leagueCode, int nextDays);
}

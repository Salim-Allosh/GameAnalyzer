using SportsAnalytics.Domain.Entities;
using SportsAnalytics.Domain.Models;

namespace SportsAnalytics.Domain.Interfaces;

public interface IBacktestingService
{
    Task<BacktestReport> RunBacktestAsync(IEnumerable<Match> testMatches, double startingBankroll = 1000.0);
}

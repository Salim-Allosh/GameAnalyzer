namespace SportsAnalytics.Domain.Models;

public record BacktestReport(
    int TotalMatchesTested,
    double BaselineBrierScore,
    double BlendedBrierScore,
    double StartingBankroll,
    double EndingBankroll,
    int TotalBetsPlaced,
    double WinRate,
    string Message
);

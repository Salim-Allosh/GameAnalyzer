namespace SportsAnalytics.Domain.Interfaces;

public record DriftReport(
    bool DriftDetected,
    double CurrentBrierScore,
    double BaselineBrierScore,
    string Message
);

public interface IDriftDetectorService
{
    Task<DriftReport> CheckForDriftAsync(int recentMatchesCount = 50, double threshold = 0.45);
    Task RetrainModelsAsync();
}

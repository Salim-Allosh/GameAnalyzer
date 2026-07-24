using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Quartz;
using SportsAnalytics.Domain.Interfaces;

namespace SportsAnalytics.Application.Jobs;

[DisallowConcurrentExecution]
public class WeeklyRecalibrationJob : IJob
{
    private readonly IDriftDetectorService _driftDetector;
    private readonly ILogger<WeeklyRecalibrationJob> _logger;

    public WeeklyRecalibrationJob(
        IDriftDetectorService driftDetector, 
        ILogger<WeeklyRecalibrationJob> logger)
    {
        _driftDetector = driftDetector;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("WeeklyRecalibrationJob started at {Time}", DateTime.UtcNow);
        Debug.WriteLine($"[Quartz] WeeklyRecalibrationJob executed at {DateTime.Now}");

        // فحص وجود انحراف (Drift)
        var report = await _driftDetector.CheckForDriftAsync(recentMatchesCount: 50, threshold: 0.45);
        
        if (report.DriftDetected)
        {
            _logger.LogWarning("Drift Detected! Retraining models...");
            await _driftDetector.RetrainModelsAsync();
            _logger.LogInformation("Retraining completed successfully.");
        }
        else
        {
            _logger.LogInformation("No drift detected. System is stable.");
        }
    }
}

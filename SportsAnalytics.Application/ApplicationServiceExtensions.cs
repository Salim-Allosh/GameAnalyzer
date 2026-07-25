using Microsoft.Extensions.DependencyInjection;
using SportsAnalytics.Application.Services;
using SportsAnalytics.Domain.Interfaces;
using SportsAnalytics.MathEngine;
using SportsAnalytics.Application.Jobs;
using Quartz;

namespace SportsAnalytics.Application;

/// <summary>
/// Extension methods لتسجيل خدمات Application + MathEngine في DI Container.
/// يُستدعى من App.xaml.cs بعد AddInfrastructure().
/// </summary>
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // ── MathEngine (Singleton — مُدرَّب مرة واحدة طوال عمر التطبيق) ──
        services.AddSingleton<PoissonDixonColes>();
        services.AddSingleton<EloRating>();
        services.AddSingleton<MonteCarloSimulator>();

        // ── ML.NET (Singleton — نموذج تكميلي) ──
        services.AddSingleton<MLMatchPredictor>();

        // ── Application Services ──
        services.AddScoped<IFeatureEngineeringService, Services.FeatureEngineeringService>();
        services.AddSingleton<IRiskScoringService, Services.RiskScoringService>();
        services.AddScoped<IPredictionOrchestrator, Services.PredictionOrchestrator>();
        services.AddScoped<IDriftDetectorService, Services.DriftDetectorService>();
        services.AddScoped<IBacktestingService, Services.BacktestingService>();
        services.AddScoped<DataAggregatorService>();
        services.AddScoped<NewsAggregatorService>();
        services.AddScoped<BettingMarketsCalculator>();
        services.AddScoped<IFixtureSyncService, Services.FixtureSyncService>();
        services.AddScoped<TeamStatisticsService>();
        services.AddScoped<NewsImpactAnalyzer>();

        // ── Background Services (Self-Learning Loop & Kaggle Data Ingestion) ──
        services.AddHostedService<SelfLearningService>();
        services.AddHostedService<KaggleDataIngestor>();

        // ── Quartz Scheduling ──
        services.AddQuartz(q =>
        {
            // UpdateDataJob (Every hour)
            var updateDataJobKey = new JobKey("UpdateDataJob");
            q.AddJob<UpdateDataJob>(opts => opts.WithIdentity(updateDataJobKey));
            q.AddTrigger(opts => opts
                .ForJob(updateDataJobKey)
                .WithIdentity("UpdateDataJob-trigger")
                .WithCronSchedule("0 0 * ? * *")); // Every hour at minute 0

            // DailyAnalysisJob (Every day at midnight)
            var dailyAnalysisJobKey = new JobKey("DailyAnalysisJob");
            q.AddJob<DailyAnalysisJob>(opts => opts.WithIdentity(dailyAnalysisJobKey));
            q.AddTrigger(opts => opts
                .ForJob(dailyAnalysisJobKey)
                .WithIdentity("DailyAnalysisJob-trigger")
                .WithCronSchedule("0 0 0 ? * *")); // Midnight

            // WeeklyRecalibrationJob (Every Monday at 2:00 AM)
            var weeklyRecalJobKey = new JobKey("WeeklyRecalibrationJob");
            q.AddJob<WeeklyRecalibrationJob>(opts => opts.WithIdentity(weeklyRecalJobKey));
            q.AddTrigger(opts => opts
                .ForJob(weeklyRecalJobKey)
                .WithIdentity("WeeklyRecalibrationJob-trigger")
                .WithCronSchedule("0 0 2 ? * MON")); // Monday at 2 AM
        });

        return services;
    }
}

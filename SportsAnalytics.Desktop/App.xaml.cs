using System.Configuration;
using System.Data;
using System.Windows;

using SportsAnalytics.Application;
using SportsAnalytics.Infrastructure;
using SportsAnalytics.Infrastructure.Data;
using SportsAnalytics.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Quartz;

// نحدد System.Windows.Application صراحةً لتجنب التعارض مع namespace SportsAnalytics.Application
using WpfApplication = System.Windows.Application;

namespace SportsAnalytics.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : WpfApplication
{
    public IServiceProvider ServiceProvider { get; private set; }

    public App()
    {
        var services = new ServiceCollection();

        // إعداد Configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ApiFootball:ApiKey", "dda479f2971da0ac0eb02790781682b0" } // يمكن تغييره لاحقاً إلى ملف appsettings.json
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Database Paths (Point to the root project directory database, not the bin/Debug one)
        string projectRootDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        string dbPath = System.IO.Path.Combine(projectRootDir, "SportsAnalytics_dev.db");
        string sqliteConn = $"Data Source={dbPath}";
        string liteDbConn = System.IO.Path.Combine(projectRootDir, "SportsAnalytics_dev.db.lite");

        // تسجيل خدمات التطبيق والبنية التحتية
        services.AddInfrastructure(sqliteConn, liteDbConn);
        services.AddApplication();

        // تسجيل ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<MatchSelectionViewModel>();
        services.AddTransient<ProcessingViewModel>();
        services.AddTransient<ReportViewModel>();
        services.AddTransient<PredictionArchiveViewModel>();

        ServiceProvider = services.BuildServiceProvider();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. تجهيز قاعدة البيانات قبل تشغيل الواجهة لتجنب مشكلة الـ ViewModel
        using (var scope = ServiceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SqliteDbContext>();
            var apiClient = scope.ServiceProvider.GetRequiredService<SportsAnalytics.Infrastructure.ExternalServices.ApiFootballClient>();
            await DatabaseSeeder.SeedAsync(dbContext, apiClient);
        }

        // Train Models Asynchronously on Startup
        _ = Task.Run(async () =>
        {
            try
            {
                using (var taskScope = ServiceProvider.CreateScope())
                {
                    var matchRepo = taskScope.ServiceProvider.GetRequiredService<SportsAnalytics.Domain.Interfaces.IMatchRepository>();
                    var allMatches = await matchRepo.GetAllMatchesAsync(5000);
                    var pastMatches = allMatches.Where(m => m.MatchDate <= DateTime.UtcNow).OrderBy(m => m.MatchDate).ToList();

                    if (pastMatches.Count > 0)
                    {
                        var records = pastMatches.Select(m => new SportsAnalytics.MathEngine.MatchRecord(
                            m.HomeTeam?.Name ?? "", m.AwayTeam?.Name ?? "", m.MatchDate, m.HomeGoals ?? 0, m.AwayGoals ?? 0)).ToList();

                        // Train Elo (Singleton)
                        var elo = taskScope.ServiceProvider.GetRequiredService<SportsAnalytics.MathEngine.EloRating>();
                        elo.TrainOnHistory(records);

                        // Train Dixon-Coles (Singleton)
                        var dixon = taskScope.ServiceProvider.GetRequiredService<SportsAnalytics.MathEngine.PoissonDixonColes>();
                        dixon.Train(records);

                        // Train ML Predictor (on a subset to keep startup fast) (Singleton)
                        var featuresSvc = taskScope.ServiceProvider.GetRequiredService<SportsAnalytics.Domain.Interfaces.IFeatureEngineeringService>();
                        var mlPredictor = taskScope.ServiceProvider.GetRequiredService<SportsAnalytics.Application.Services.MLMatchPredictor>();
                        
                        var trainingData = new List<(SportsAnalytics.Domain.Models.MatchFeatures Features, int Outcome)>();
                        foreach (var m in pastMatches.Skip(pastMatches.Count - 150)) // last 150 matches for fast training
                        {
                            var f = await featuresSvc.ComputeAsync(m.HomeTeamId, m.AwayTeamId, m.MatchDate, default);
                            int outcome = m.HomeGoals > m.AwayGoals ? 0 : m.HomeGoals == m.AwayGoals ? 1 : 2;
                            trainingData.Add((f, outcome));
                        }
                        mlPredictor.Train(trainingData);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error training models on startup: {ex.Message}");
            }
        });

        // 2. تشغيل مجدول المهام
        var schedulerFactory = ServiceProvider.GetRequiredService<ISchedulerFactory>();
        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.Start();

        // 3. بناء الواجهة
        var mainWindow = new MainWindow();
        var mainViewModel = ServiceProvider.GetRequiredService<MainViewModel>();
        
        // الشاشة الافتراضية
        mainViewModel.NavigateTo(ServiceProvider.GetRequiredService<MatchSelectionViewModel>());
        
        mainWindow.DataContext = mainViewModel;
        mainWindow.Show();
    }
}

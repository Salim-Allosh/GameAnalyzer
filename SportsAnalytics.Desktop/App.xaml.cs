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

        // Database Paths
        string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SportsAnalytics_dev.db");
        string sqliteConn = $"Data Source={dbPath}";
        string liteDbConn = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SportsAnalytics_dev.db.lite");

        // تسجيل خدمات التطبيق والبنية التحتية
        services.AddInfrastructure(sqliteConn, liteDbConn);
        services.AddApplication();

        // تسجيل ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<MatchSelectionViewModel>();
        services.AddTransient<ProcessingViewModel>();
        services.AddTransient<ReportViewModel>();

        ServiceProvider = services.BuildServiceProvider();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. تجهيز قاعدة البيانات قبل تشغيل الواجهة لتجنب مشكلة الـ ViewModel
        using (var scope = ServiceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SqliteDbContext>();
            await DatabaseSeeder.SeedAsync(dbContext);
        }

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

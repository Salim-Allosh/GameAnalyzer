using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using SportsAnalytics.Desktop.Messages;
using SportsAnalytics.Domain.Models;

namespace SportsAnalytics.Desktop.ViewModels;

public partial class ReportViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private AnalysisReport _report = null!;

    public ISeries[] Series { get; set; } = [];
    public Axis[] XAxes { get; set; } = [];
    public Axis[] YAxes { get; set; } = [];

    // Grouping betting markets by MarketName for easy UI display
    [ObservableProperty]
    private IEnumerable<IGrouping<string, BettingMarketPrediction>> _groupedBettingMarkets = null!;

    public ReportViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Initialize(AnalysisReport report)
    {
        Report = report;
        
        // Group the markets
        if (report.BettingMarkets != null)
        {
            GroupedBettingMarkets = report.BettingMarkets.GroupBy(m => m.MarketName).ToList();
        }

        SetupChart(report);
    }

    private void SetupChart(AnalysisReport report)
    {
        var scores = report.TopScores.Take(10).ToList();
        var labels = scores.Select(s => $"{s.Home}-{s.Away}").ToArray();
        var values = scores.Select(s => s.Prob * 100).ToArray(); // percentage

        Series =
        [
            new ColumnSeries<double>
            {
                Values = values,
                Name = "Probability (%)",
                DataLabelsFormatter = point => $"{point.Model:F1}%"
            }
        ];

        XAxes =
        [
            new Axis
            {
                Labels = labels,
                Name = "Score (Home - Away)"
            }
        ];

        YAxes =
        [
            new Axis
            {
                Name = "Probability (%)",
                Labeler = value => $"{value:F1}%"
            }
        ];
    }

    [RelayCommand]
    private void GoBack()
    {
        var matchSelectionVm = _serviceProvider.GetRequiredService<MatchSelectionViewModel>();
        WeakReferenceMessenger.Default.Send(new NavigationMessage(matchSelectionVm));
    }

    [RelayCommand]
    private void OpenAdvancedStats()
    {
        // Resolve services from DI
        var teamStatsService = _serviceProvider.GetRequiredService<SportsAnalytics.Application.Services.TeamStatisticsService>();
        var newsAggregator = _serviceProvider.GetRequiredService<SportsAnalytics.Application.Services.NewsAggregatorService>();
        var newsImpactAnalyzer = _serviceProvider.GetRequiredService<SportsAnalytics.Application.Services.NewsImpactAnalyzer>();

        // We assume we can get HomeTeamId and AwayTeamId from the Report (if it exists)
        // Note: AnalysisReport needs HomeTeamId and AwayTeamId for this to work correctly.
        // Let's pass 1 and 2 temporarily if we don't have them in the report, or better, we should fetch them.
        // Wait, AnalysisReport only has HomeTeam and AwayTeam (names). 
        // We might need to look up their IDs, or add them to the Report. 
        // For now let's resolve DB and find them.
        var db = _serviceProvider.GetRequiredService<SportsAnalytics.Infrastructure.Data.SqliteDbContext>();
        var homeTeam = db.Teams.FirstOrDefault(t => t.Name == Report.HomeTeam);
        var awayTeam = db.Teams.FirstOrDefault(t => t.Name == Report.AwayTeam);

        if (homeTeam == null || awayTeam == null) return;

        var vm = new AdvancedStatisticsViewModel(
            homeTeam.Id, homeTeam.Name, awayTeam.Id, awayTeam.Name,
            teamStatsService, newsAggregator, newsImpactAnalyzer);

        var window = new SportsAnalytics.Desktop.Views.AdvancedStatisticsWindow(vm);
        window.Show(); // Open as popup
    }
}

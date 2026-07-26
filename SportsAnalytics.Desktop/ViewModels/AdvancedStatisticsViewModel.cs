using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SportsAnalytics.Application.Services;
using SportsAnalytics.Domain.Models;

namespace SportsAnalytics.Desktop.ViewModels;

public partial class AdvancedStatisticsViewModel : ViewModelBase
{
    private readonly TeamStatisticsService _teamStatsService;
    private readonly NewsAggregatorService _newsAggregator;
    private readonly NewsImpactAnalyzer _newsImpactAnalyzer;

    private readonly int _homeTeamId;
    private readonly string _homeTeamName;
    private readonly int _awayTeamId;
    private readonly string _awayTeamName;

    [ObservableProperty] private int _numberOfMatches = 5;

    [ObservableProperty] private TeamDetailedStats _homeStats;
    [ObservableProperty] private TeamDetailedStats _awayStats;
    [ObservableProperty] private TeamDetailedStats _h2hStats;

    public ObservableCollection<NewsImpact> HomeNews { get; } = new();
    public ObservableCollection<NewsImpact> AwayNews { get; } = new();

    [ObservableProperty] private bool _isLoading;

    public ICommand ApplyFilterCommand { get; }

    public AdvancedStatisticsViewModel(
        int homeTeamId, string homeTeamName, int awayTeamId, string awayTeamName,
        TeamStatisticsService teamStatsService,
        NewsAggregatorService newsAggregator,
        NewsImpactAnalyzer newsImpactAnalyzer)
    {
        _homeTeamId = homeTeamId;
        _homeTeamName = homeTeamName;
        _awayTeamId = awayTeamId;
        _awayTeamName = awayTeamName;

        _teamStatsService = teamStatsService;
        _newsAggregator = newsAggregator;
        _newsImpactAnalyzer = newsImpactAnalyzer;

        ApplyFilterCommand = new AsyncRelayCommand(LoadDataAsync);

        // Load initially
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private void OpenNewsDetail(NewsImpact news)
    {
        if (news != null)
        {
            var window = new Views.NewsDetailWindow(news);
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }
    }

    private async Task LoadDataAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            // Load DB Stats
            HomeStats = await _teamStatsService.GetTeamStatsAsync(_homeTeamId, NumberOfMatches);
            AwayStats = await _teamStatsService.GetTeamStatsAsync(_awayTeamId, NumberOfMatches);
            H2hStats = await _teamStatsService.GetH2HStatsAsync(_homeTeamId, _awayTeamId, NumberOfMatches);

            // Load News only once if empty, or refresh if needed. Let's refresh.
            HomeNews.Clear();
            var homeNewsRaw = await _newsAggregator.AggregateNewsAsync(_homeTeamName);
            var homeImpacts = _newsImpactAnalyzer.AnalyzeMultiple(homeNewsRaw);
            foreach (var item in homeImpacts) HomeNews.Add(item);

            AwayNews.Clear();
            var awayNewsRaw = await _newsAggregator.AggregateNewsAsync(_awayTeamName);
            var awayImpacts = _newsImpactAnalyzer.AnalyzeMultiple(awayNewsRaw);
            foreach (var item in awayImpacts) AwayNews.Add(item);
        }
        finally
        {
            IsLoading = false;
        }
    }
}

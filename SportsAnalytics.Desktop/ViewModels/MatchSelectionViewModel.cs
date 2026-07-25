using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SportsAnalytics.Desktop.Messages;
using SportsAnalytics.Domain.Entities;
using SportsAnalytics.Domain.Interfaces;

namespace SportsAnalytics.Desktop.ViewModels;

public partial class MatchSelectionViewModel : ViewModelBase
{
    private readonly IMatchRepository _matchRepo;
    private readonly IServiceProvider _serviceProvider;
    private readonly IFixtureSyncService _fixtureSync;

    [ObservableProperty]
    private ObservableCollection<Match> _matches = new();

    [ObservableProperty]
    private Match? _selectedMatch;

    [ObservableProperty]
    private bool _isSyncing;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private ObservableCollection<string> _availableLeagues = new();

    private string? _selectedLeague;
    public string? SelectedLeague
    {
        get => _selectedLeague;
        set
        {
            SetProperty(ref _selectedLeague, value);
            FilterTeamsByLeague();
            FilterMatchesByLeague();
        }
    }

    [ObservableProperty]
    private ObservableCollection<Match> _filteredMatches = new();

    [ObservableProperty]
    private ObservableCollection<Team> _availableTeams = new();

    [ObservableProperty]
    private Team? _manualHomeTeam;

    [ObservableProperty]
    private Team? _manualAwayTeam;

    private List<Team> _allTeams = new();

    public MatchSelectionViewModel(IMatchRepository matchRepo, IServiceProvider serviceProvider, IFixtureSyncService fixtureSync)
    {
        _matchRepo = matchRepo;
        _serviceProvider = serviceProvider;
        _fixtureSync = fixtureSync;
        _ = LoadInitialDataAsync();
    }

    private async Task LoadInitialDataAsync()
    {
        await LoadTeamsAsync();
        await LoadMatchesAsync();
    }

    private async Task LoadTeamsAsync()
    {
        _allTeams = (await _matchRepo.GetAllTeamsAsync()).ToList();
        var leagues = _allTeams.Select(t => t.League).Where(l => !string.IsNullOrEmpty(l)).Distinct().OrderBy(l => l);
        AvailableLeagues.Clear();
        foreach (var l in leagues) AvailableLeagues.Add(l);
        
        if (string.IsNullOrEmpty(SelectedLeague) && AvailableLeagues.Any())
        {
            SelectedLeague = AvailableLeagues.First();
        }
    }

    private void FilterTeamsByLeague()
    {
        AvailableTeams.Clear();
        if (string.IsNullOrEmpty(SelectedLeague)) return;
        var teams = _allTeams.Where(t => t.League == SelectedLeague).OrderBy(t => t.Name);
        foreach (var t in teams) AvailableTeams.Add(t);
        ManualHomeTeam = null;
        ManualAwayTeam = null;
    }

    public async Task LoadMatchesAsync()
    {
        var matches = await _matchRepo.GetAllMatchesAsync();
        Matches.Clear();
        foreach (var match in matches.OrderBy(m => m.MatchDate))
        {
            Matches.Add(match);
        }
        
        FilterMatchesByLeague();
    }

    private void FilterMatchesByLeague()
    {
        FilteredMatches.Clear();
        var matchesToDisplay = string.IsNullOrEmpty(SelectedLeague) 
            ? Matches 
            : Matches.Where(m => m.League == SelectedLeague);

        foreach (var m in matchesToDisplay)
        {
            FilteredMatches.Add(m);
        }

        IsEmpty = FilteredMatches.Count == 0;
    }

    [RelayCommand]
    private void AnalyzeMatch(Match match)
    {
        if (match == null) return;

        var processingVm = _serviceProvider.GetRequiredService<ProcessingViewModel>();
        processingVm.Initialize(match);
        WeakReferenceMessenger.Default.Send(new NavigationMessage(processingVm));
    }

    [RelayCommand]
    private void AnalyzeManualMatch()
    {
        if (ManualHomeTeam == null || ManualAwayTeam == null || ManualHomeTeam.Id == ManualAwayTeam.Id) return;

        var manualMatch = new Match
        {
            HomeTeamId = ManualHomeTeam.Id,
            AwayTeamId = ManualAwayTeam.Id,
            HomeTeam = ManualHomeTeam,
            AwayTeam = ManualAwayTeam,
            League = SelectedLeague ?? string.Empty,
            MatchDate = DateTime.Now
        };

        var processingVm = _serviceProvider.GetRequiredService<ProcessingViewModel>();
        processingVm.Initialize(manualMatch);
        WeakReferenceMessenger.Default.Send(new NavigationMessage(processingVm));
    }

    [RelayCommand]
    private async Task SyncFixturesAsync()
    {
        if (IsSyncing) return;
        
        IsSyncing = true;
        IsEmpty = false;
        Matches.Clear();
        
        // Sync Premier League ("eng.1") for the next 7 days from ESPN API
        // (In a real app, user selects league)
        await _fixtureSync.SyncUpcomingFixturesAsync("eng.1", 7);
        await LoadMatchesAsync(); // Refresh grid
        
        IsSyncing = false;
    }

    [RelayCommand]
    private void OpenArchive()
    {
        var archiveVm = _serviceProvider.GetRequiredService<PredictionArchiveViewModel>();
        WeakReferenceMessenger.Default.Send(new NavigationMessage(archiveVm));
    }
}

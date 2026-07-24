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

    public MatchSelectionViewModel(IMatchRepository matchRepo, IServiceProvider serviceProvider, IFixtureSyncService fixtureSync)
    {
        _matchRepo = matchRepo;
        _serviceProvider = serviceProvider;
        _fixtureSync = fixtureSync;
        _ = LoadMatchesAsync();
    }

    public async Task LoadMatchesAsync()
    {
        var matches = await _matchRepo.GetAllMatchesAsync();
        Matches.Clear();
        foreach (var match in matches.OrderBy(m => m.MatchDate))
        {
            Matches.Add(match);
        }
        
        IsEmpty = Matches.Count == 0;
    }

    [RelayCommand]
    private void AnalyzeMatch()
    {
        if (SelectedMatch == null) return;

        var processingVm = _serviceProvider.GetRequiredService<ProcessingViewModel>();
        processingVm.Initialize(SelectedMatch);
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
}

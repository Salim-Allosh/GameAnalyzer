using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SportsAnalytics.Domain.Entities;
using SportsAnalytics.Infrastructure.Data;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using SportsAnalytics.Desktop.Messages;

namespace SportsAnalytics.Desktop.ViewModels;

public class PredictionArchiveViewModel : ViewModelBase
{
    private readonly SqliteDbContext _dbContext;
    
    private ObservableCollection<PredictionArchiveItem> _archives = new();
    public ObservableCollection<PredictionArchiveItem> Archives
    {
        get => _archives;
        set => SetProperty(ref _archives, value);
    }
    
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ICommand LoadArchivesCommand { get; }
    public ICommand GoBackCommand { get; }

    public PredictionArchiveViewModel(SqliteDbContext dbContext, IServiceProvider serviceProvider)
    {
        _dbContext = dbContext;
        LoadArchivesCommand = new RelayCommand(async () => await LoadArchivesAsync());
        
        GoBackCommand = new RelayCommand(() => 
        {
            var matchSelectionVm = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<MatchSelectionViewModel>(serviceProvider);
            CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Send(new SportsAnalytics.Desktop.Messages.NavigationMessage(matchSelectionVm));
        });

        // Load initially
        LoadArchivesCommand.Execute(null);
    }

    private async Task LoadArchivesAsync()
    {
        IsLoading = true;
        try
        {
            var predictions = await _dbContext.Predictions
                .Include(p => p.Match)
                .ThenInclude(m => m.HomeTeam)
                .Include(p => p.Match)
                .ThenInclude(m => m.AwayTeam)
                .OrderByDescending(p => p.CreatedAt)
                .Take(100) // Show last 100 predictions
                .ToListAsync();

            Archives.Clear();
            foreach (var p in predictions)
            {
                Archives.Add(new PredictionArchiveItem
                {
                    MatchDate = p.Match.MatchDate,
                    MatchName = $"{p.Match.HomeTeam?.Name ?? "Unknown"} vs {p.Match.AwayTeam?.Name ?? "Unknown"}",
                    PredictedHomeWin = p.HomeWinProbability,
                    PredictedDraw = p.DrawProbability,
                    PredictedAwayWin = p.AwayWinProbability,
                    ActualHomeGoals = p.ActualHomeGoals,
                    ActualAwayGoals = p.ActualAwayGoals,
                    ActualResult = p.ActualResult ?? (p.IsCompleted ? "?" : "لم تلعب"),
                    IsCorrect = IsPredictionCorrect(p)
                });
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool? IsPredictionCorrect(Prediction p)
    {
        if (!p.IsCompleted || string.IsNullOrEmpty(p.ActualResult))
            return null; // Not played yet

        // Determine predicted result based on highest probability
        string predictedResult = "1";
        double maxProb = p.HomeWinProbability;
        
        if (p.DrawProbability > maxProb)
        {
            predictedResult = "X";
            maxProb = p.DrawProbability;
        }
        if (p.AwayWinProbability > maxProb)
        {
            predictedResult = "2";
        }

        return p.ActualResult == predictedResult;
    }
}

public class PredictionArchiveItem
{
    public DateTime MatchDate { get; set; }
    public string MatchName { get; set; } = string.Empty;
    public double PredictedHomeWin { get; set; }
    public double PredictedDraw { get; set; }
    public double PredictedAwayWin { get; set; }
    public int? ActualHomeGoals { get; set; }
    public int? ActualAwayGoals { get; set; }
    public string ActualResult { get; set; } = string.Empty;
    public bool? IsCorrect { get; set; }
}

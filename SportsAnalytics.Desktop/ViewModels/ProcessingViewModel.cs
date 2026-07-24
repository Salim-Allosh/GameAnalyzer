using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SportsAnalytics.Application.Services;
using SportsAnalytics.Desktop.Messages;
using SportsAnalytics.Domain.Entities;
using SportsAnalytics.Domain.Interfaces;

namespace SportsAnalytics.Desktop.ViewModels;

public partial class ProcessingViewModel : ViewModelBase
{
    private readonly IPredictionOrchestrator _orchestrator;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private string _statusMessage = "جاري التحليل...";

    public ProcessingViewModel(IPredictionOrchestrator orchestrator, IServiceProvider serviceProvider)
    {
        _orchestrator = orchestrator;
        _serviceProvider = serviceProvider;
    }

    public void Initialize(Match match)
    {
        StatusMessage = $"جاري تحليل مباراة: {match.HomeTeam.Name} ضد {match.AwayTeam.Name} ...";
        _ = RunAnalysisAsync(match);
    }

    private async Task RunAnalysisAsync(Match match)
    {
        try
        {
            var report = await _orchestrator.AnalyzeAsync(match.HomeTeamId, match.AwayTeamId, match.MatchDate);

            // Navigate to report view
            var reportVm = _serviceProvider.GetRequiredService<ReportViewModel>();
            reportVm.Initialize(report);
            WeakReferenceMessenger.Default.Send(new NavigationMessage(reportVm));
        }
        catch (Exception ex)
        {
            StatusMessage = $"حدث خطأ أثناء التحليل: {ex.Message}";
        }
    }
}

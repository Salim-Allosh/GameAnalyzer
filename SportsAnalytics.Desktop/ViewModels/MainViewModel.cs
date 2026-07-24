using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using SportsAnalytics.Desktop.Messages;

namespace SportsAnalytics.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentViewModel = null!;

    public MainViewModel()
    {
        WeakReferenceMessenger.Default.Register<NavigationMessage>(this, (r, m) =>
        {
            NavigateTo(m.Value);
        });
    }

    public void NavigateTo(ViewModelBase viewModel)
    {
        CurrentViewModel = viewModel;
    }
}

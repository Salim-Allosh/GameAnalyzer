using System.Windows;
using SportsAnalytics.Desktop.ViewModels;

namespace SportsAnalytics.Desktop.Views;

public partial class AdvancedStatisticsWindow : Window
{
    public AdvancedStatisticsWindow(AdvancedStatisticsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

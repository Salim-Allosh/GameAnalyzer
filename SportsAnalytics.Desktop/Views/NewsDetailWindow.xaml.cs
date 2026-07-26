using System;
using System.Diagnostics;
using System.Windows;
using SportsAnalytics.Domain.Models;

namespace SportsAnalytics.Desktop.Views;

public partial class NewsDetailWindow : Window
{
    private string _newsUrl = string.Empty;

    public NewsDetailWindow(NewsImpact news)
    {
        InitializeComponent();

        if (news != null)
        {
            TitleTextBlock.Text = news.Title;
            DescriptionTextBlock.Text = news.Description;
            SourceTextBlock.Text = news.SourceName;
            DateTextBlock.Text = news.PublishedAt.ToString("dd/MM/yyyy HH:mm");
            
            string sign = news.ImpactPercentage >= 0 ? "+" : "";
            ImpactTextBlock.Text = $"{sign}{news.ImpactPercentage:F1}%";
            ImpactTextBlock.Foreground = news.ImpactPercentage >= 0 
                ? (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#4CAF50")! 
                : (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#F44336")!;

            _newsUrl = news.Url;
            if (string.IsNullOrWhiteSpace(_newsUrl))
            {
                OpenUrlButton.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void OpenUrlButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_newsUrl))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _newsUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"تعذر فتح الرابط: {ex.Message}", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

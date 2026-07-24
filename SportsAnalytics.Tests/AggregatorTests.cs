using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SportsAnalytics.Application.Services;
using SportsAnalytics.Domain.Interfaces;
using SportsAnalytics.Domain.Models;
using Xunit;

namespace SportsAnalytics.Tests;

public class AggregatorTests
{
    [Fact]
    public async Task DataAggregatorService_ShouldMergeXgAndKeepUniqueMetrics()
    {
        // Arrange
        var statsBombMock = new Mock<IStatisticsProvider>();
        statsBombMock.Setup(p => p.ProviderName).Returns("StatsBomb");
        statsBombMock.Setup(p => p.GetMatchDataAsync("Team A", "Team B", It.IsAny<DateTime>()))
            .ReturnsAsync(new UnifiedMatchData
            {
                HomeTeam_xG = 2.0,
                AwayTeam_xG = 1.0,
                DataSources = new List<string> { "StatsBomb" },
                UniqueMetrics = new Dictionary<string, object> { { "SB_Passes", 500 } }
            });

        var understatMock = new Mock<IStatisticsProvider>();
        understatMock.Setup(p => p.ProviderName).Returns("Understat");
        understatMock.Setup(p => p.GetMatchDataAsync("Team A", "Team B", It.IsAny<DateTime>()))
            .ReturnsAsync(new UnifiedMatchData
            {
                HomeTeam_xG = 3.0,
                AwayTeam_xG = 2.0,
                DataSources = new List<string> { "Understat" },
                UniqueMetrics = new Dictionary<string, object> { { "US_PPDA", 8.5 } }
            });

        var aggregator = new DataAggregatorService(
            new List<IStatisticsProvider> { statsBombMock.Object, understatMock.Object },
            new NullLogger<DataAggregatorService>()
        );

        // Act
        var result = await aggregator.AggregateMatchDataAsync("Team A", "Team B", DateTime.UtcNow);

        // Assert
        Assert.Equal(2.5, result.HomeTeam_xG); // (2.0 + 3.0) / 2
        Assert.Equal(1.5, result.AwayTeam_xG); // (1.0 + 2.0) / 2
        Assert.Contains("StatsBomb", result.DataSources);
        Assert.Contains("Understat", result.DataSources);
        Assert.Equal(500, result.UniqueMetrics["SB_Passes"]);
        Assert.Equal(8.5, result.UniqueMetrics["US_PPDA"]);
    }

    [Fact]
    public async Task NewsAggregatorService_ShouldDeduplicateNews()
    {
        // Arrange
        var provider1 = new Mock<INewsProvider>();
        provider1.Setup(p => p.GetNewsAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<UnifiedNewsData>
            {
                new UnifiedNewsData { Title = "Team A signs new player", PublishedAt = DateTime.UtcNow }
            });

        var provider2 = new Mock<INewsProvider>();
        provider2.Setup(p => p.GetNewsAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<UnifiedNewsData>
            {
                new UnifiedNewsData { Title = "Team A signs new player today", PublishedAt = DateTime.UtcNow.AddMinutes(-10) }
            });

        var aggregator = new NewsAggregatorService(
            new List<INewsProvider> { provider1.Object, provider2.Object },
            new NullLogger<NewsAggregatorService>()
        );

        // Act
        var result = await aggregator.AggregateNewsAsync("Team A");

        // Assert
        // The titles are very similar (share first 10+ chars), so it should deduplicate and keep 1
        Assert.Single(result);
    }
}

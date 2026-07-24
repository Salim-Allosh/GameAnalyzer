using SportsAnalytics.Application.Services;
using SportsAnalytics.MathEngine;
using Xunit;

namespace SportsAnalytics.Tests;

public class BettingMarketsTests
{
    [Fact]
    public void BettingMarketsCalculator_ShouldCalculateProbabilitiesCorrectly()
    {
        // Arrange
        var simulator = new MonteCarloSimulator();
        simulator.MinIterations = 1000;
        simulator.MaxIterations = 1000; // اجبار المحاكاة على 1000 لسرعة الاختبار
        
        var simulationResult = simulator.Simulate(1.5, 1.0); // Home favored
        var calculator = new BettingMarketsCalculator();

        // Act
        var markets = calculator.CalculateMarkets(simulationResult, "Team A", "Team B");

        // Assert
        Assert.NotEmpty(markets);
        
        var homeWin = markets.First(m => m.MarketName == "Match Result (1X2)" && m.Selection == "Team A Win");
        var draw = markets.First(m => m.MarketName == "Match Result (1X2)" && m.Selection == "Draw");
        var awayWin = markets.First(m => m.MarketName == "Match Result (1X2)" && m.Selection == "Team B Win");

        // مجموع احتمالات 1X2 يجب أن يكون 1.0 أو قريب جداً منه
        Assert.InRange(homeWin.Probability + draw.Probability + awayWin.Probability, 0.99, 1.01);
        
        // الفريق المضيف لديه لامدا أعلى، لذا فرصة فوزه أكبر
        Assert.True(homeWin.Probability > awayWin.Probability);

        // Double Chance
        var homeOrDraw = markets.First(m => m.MarketName == "Double Chance" && m.Selection == "Team A or Draw");
        Assert.Equal(homeWin.Probability + draw.Probability, homeOrDraw.Probability, 2);

        // BTTS
        var bttsYes = markets.First(m => m.MarketName == "Both Teams To Score" && m.Selection == "Yes");
        var bttsNo = markets.First(m => m.MarketName == "Both Teams To Score" && m.Selection == "No");
        Assert.Equal(1.0, bttsYes.Probability + bttsNo.Probability, 2);
    }
}

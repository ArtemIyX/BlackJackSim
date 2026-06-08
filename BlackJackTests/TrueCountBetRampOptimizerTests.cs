using BlackJackData.Rules;
using BlackJackStrategy.Optimization;
using BlackJackStrategy.Models;
using FluentAssertions;

namespace BlackJackTests;

public class TrueCountBetRampOptimizerTests
{
    [Fact]
    public void Optimize_ShouldEvaluateAllMonotonicCandidates()
    {
        var optimizer = new TrueCountBetRampOptimizer();
        var config = new BetRampOptimizationConfig(
            Rules: BlackjackRules.Default,
            RoundsPerCandidate: 5,
            StartingBankroll: 100m,
            MinimumWager: 10m,
            UnitSize: 10m,
            Thresholds: [1d, 2d],
            AllowedUnits: [1m, 2m],
            TopResultsToKeep: 5,
            RandomSeed: 123);

        var result = optimizer.Optimize(config);

        result.CandidatesEvaluated.Should().Be(3);
        result.TopResults.Should().HaveCount(3);
        result.TopResults.Should().OnlyContain(item => item.Candidate.Steps.Count == 2);
    }

    [Fact]
    public void Optimize_ShouldReturnTopResultsOrderedByScore()
    {
        var optimizer = new TrueCountBetRampOptimizer();
        var config = new BetRampOptimizationConfig(
            Rules: BlackjackRules.Default,
            RoundsPerCandidate: 20,
            StartingBankroll: 200m,
            MinimumWager: 10m,
            UnitSize: 10m,
            Thresholds: [1d, 2d, 3d],
            AllowedUnits: [1m, 2m],
            TopResultsToKeep: 2,
            RandomSeed: 456);

        var result = optimizer.Optimize(config);

        result.TopResults.Should().HaveCount(2);
        result.TopResults[0].Score.Should().BeGreaterThanOrEqualTo(result.TopResults[1].Score);
    }
}

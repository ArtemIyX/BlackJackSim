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
            CountingSystemName: "Hi-Lo",
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
            CountingSystemName: "Hi-Lo",
            Thresholds: [1d, 2d, 3d],
            AllowedUnits: [1m, 2m],
            TopResultsToKeep: 2,
            RandomSeed: 456);

        var result = optimizer.Optimize(config);

        result.TopResults.Should().HaveCount(2);
        result.TopResults[0].Score.Should().BeGreaterThanOrEqualTo(result.TopResults[1].Score);
    }

    [Fact]
    public void Optimize_ShouldBeDeterministicAcrossParallelismSettings_WithSameSeed()
    {
        var optimizer = new TrueCountBetRampOptimizer();
        var singleThreadConfig = new BetRampOptimizationConfig(
            Rules: BlackjackRules.Default,
            RoundsPerCandidate: 15,
            StartingBankroll: 200m,
            MinimumWager: 10m,
            UnitSize: 10m,
            CountingSystemName: "Hi-Lo",
            Thresholds: [1d, 2d],
            AllowedUnits: [1m, 2m, 4m],
            TopResultsToKeep: 3,
            RandomSeed: 999,
            MaxDegreeOfParallelism: 1);

        var parallelConfig = singleThreadConfig with { MaxDegreeOfParallelism = 4 };

        var singleThreaded = optimizer.Optimize(singleThreadConfig);
        var parallel = optimizer.Optimize(parallelConfig);

        singleThreaded.TopResults.Should().BeEquivalentTo(parallel.TopResults);
    }

    [Fact]
    public void Optimize_ShouldAllowWongHalvesWithFractionalThresholds()
    {
        var optimizer = new TrueCountBetRampOptimizer();
        var config = new BetRampOptimizationConfig(
            Rules: BlackjackRules.Default,
            RoundsPerCandidate: 10,
            StartingBankroll: 200m,
            MinimumWager: 10m,
            UnitSize: 10m,
            CountingSystemName: "Wong Halves",
            Thresholds: [0.5d, 1.5d, 2.5d],
            AllowedUnits: [1m, 2m, 4m],
            TopResultsToKeep: 2,
            RandomSeed: 321);

        var result = optimizer.Optimize(config);

        result.CandidatesEvaluated.Should().BeGreaterThan(0);
        result.Config.CountingSystemName.Should().Be("Wong Halves");
        result.TopResults.Should().NotBeEmpty();
    }
}

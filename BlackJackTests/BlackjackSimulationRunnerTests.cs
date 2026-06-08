using BlackJackData.Enums;
using BlackJackData.Rules;
using BlackJackStrategy.Contracts;
using BlackJackStrategy.Models;
using BlackJackStrategy.Simulation;
using BlackJackStrategy.Strategies;
using FluentAssertions;

namespace BlackJackTests;

public class BlackjackSimulationRunnerTests
{
    [Fact]
    public void Run_ShouldProduceDeterministicSummary_WhenSeedIsFixed()
    {
        var runner = new BlackjackSimulationRunner();
        var config = new SimulationConfig(
            RoundsToPlay: 25,
            StartingBankroll: 100m,
            Rules: BlackjackRules.Default with { AllowInsurance = false },
            MinimumWager: 5m,
            CaptureRoundRecords: true,
            RandomSeed: 12345);

        var first = runner.Run(new FixedBetFallbackStrategy(5m), config);
        var second = runner.Run(new FixedBetFallbackStrategy(5m), config);

        first.Statistics.Should().BeEquivalentTo(second.Statistics);
        first.RoundRecords.Should().BeEquivalentTo(second.RoundRecords);
    }

    [Fact]
    public void Run_ShouldKeepBankrollConsistentWithNetPayout()
    {
        var runner = new BlackjackSimulationRunner();
        var config = new SimulationConfig(
            RoundsToPlay: 20,
            StartingBankroll: 100m,
            Rules: BlackjackRules.Default with { AllowInsurance = false },
            MinimumWager: 5m,
            CaptureRoundRecords: false,
            RandomSeed: 42);

        var result = runner.Run(new FixedBetFallbackStrategy(5m), config);

        result.Statistics.EndingBankroll.Should().Be(
            result.Statistics.StartingBankroll + result.Statistics.TotalNetPayout);
        result.Statistics.RoundsPlayed.Should().BeGreaterThan(0);
        result.Statistics.HandsPlayed.Should().BeGreaterThanOrEqualTo(result.Statistics.RoundsPlayed);
    }

    [Fact]
    public void Run_ShouldCaptureRoundRecords_WhenRequested()
    {
        var runner = new BlackjackSimulationRunner();
        var config = new SimulationConfig(
            RoundsToPlay: 10,
            StartingBankroll: 100m,
            Rules: BlackjackRules.Default with { AllowInsurance = false },
            MinimumWager: 5m,
            CaptureRoundRecords: true,
            RandomSeed: 7);

        var result = runner.Run(new FixedBetFallbackStrategy(5m), config);

        result.RoundRecords.Should().HaveCount(result.Statistics.RoundsPlayed);
        result.RoundRecords.Should().OnlyContain(record => record.Hands.Count > 0);
    }

    [Fact]
    public void Run_ShouldCallResetAndRoundCompletedHooks()
    {
        var strategy = new RecordingStrategy();
        var runner = new BlackjackSimulationRunner();
        var config = new SimulationConfig(
            RoundsToPlay: 8,
            StartingBankroll: 100m,
            Rules: BlackjackRules.Default with { AllowInsurance = false },
            MinimumWager: 5m,
            RandomSeed: 9);

        var result = runner.Run(strategy, config);

        strategy.ResetCallCount.Should().Be(1);
        strategy.CompletedRounds.Should().Be(result.Statistics.RoundsPlayed);
    }

    [Fact]
    public void Run_ShouldStop_WhenStrategyReturnsZeroWager()
    {
        var runner = new BlackjackSimulationRunner();
        var config = new SimulationConfig(
            RoundsToPlay: 50,
            StartingBankroll: 100m,
            Rules: BlackjackRules.Default,
            MinimumWager: 5m,
            RandomSeed: 1);

        var result = runner.Run(new StopAfterThreeRoundsStrategy(), config);

        result.Statistics.RoundsPlayed.Should().Be(3);
    }

    [Fact]
    public void Run_ShouldThrow_WhenStrategyReturnsIllegalAction()
    {
        var runner = new BlackjackSimulationRunner();
        var config = new SimulationConfig(
            RoundsToPlay: 1,
            StartingBankroll: 100m,
            Rules: BlackjackRules.Default with { AllowInsurance = false },
            MinimumWager: 5m,
            RandomSeed: 1);

        var act = () => runner.Run(new IllegalActionStrategy(), config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*illegal action*");
    }

    private sealed class RecordingStrategy : IBlackjackStrategy
    {
        public int ResetCallCount { get; private set; }

        public int CompletedRounds { get; private set; }

        public decimal GetWager(StrategyWagerContext context) => 5m;

        public PlayerActionType GetAction(StrategyActionContext context)
        {
            if (context.LegalActions.Contains(PlayerActionType.DeclineInsurance))
            {
                return PlayerActionType.DeclineInsurance;
            }

            return context.LegalActions.Contains(PlayerActionType.Stand)
                ? PlayerActionType.Stand
                : context.LegalActions.First();
        }

        public void OnRoundCompleted(StrategyRoundResultContext context)
        {
            CompletedRounds++;
        }

        public void Reset()
        {
            ResetCallCount++;
            CompletedRounds = 0;
        }
    }

    private sealed class StopAfterThreeRoundsStrategy : IBlackjackStrategy
    {
        private int _roundsSeen;

        public decimal GetWager(StrategyWagerContext context)
        {
            _roundsSeen++;
            return _roundsSeen <= 3 ? 5m : 0m;
        }

        public PlayerActionType GetAction(StrategyActionContext context)
        {
            if (context.LegalActions.Contains(PlayerActionType.DeclineInsurance))
            {
                return PlayerActionType.DeclineInsurance;
            }

            return context.LegalActions.Contains(PlayerActionType.Stand)
                ? PlayerActionType.Stand
                : context.LegalActions.First();
        }

        public void OnRoundCompleted(StrategyRoundResultContext context)
        {
        }

        public void Reset()
        {
            _roundsSeen = 0;
        }
    }

    private sealed class IllegalActionStrategy : IBlackjackStrategy
    {
        public decimal GetWager(StrategyWagerContext context) => 5m;

        public PlayerActionType GetAction(StrategyActionContext context) => PlayerActionType.Split;

        public void OnRoundCompleted(StrategyRoundResultContext context)
        {
        }

        public void Reset()
        {
        }
    }
}

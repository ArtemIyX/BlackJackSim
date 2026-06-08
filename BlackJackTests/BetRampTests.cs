using BlackJackStrategy.Betting;
using BlackJackStrategy.Models;
using FluentAssertions;

namespace BlackJackTests;

public class BetRampTests
{
    [Fact]
    public void FlatBetRamp_ShouldReturnConfiguredUnitsTimesUnitSize()
    {
        var ramp = new FlatBetRamp(units: 3m);
        var context = CreateContext(trueCount: 2d, bankroll: 100m, minimumWager: 10m, maximumWager: 100m, unitSize: 5m);

        var wager = ramp.GetWager(context);

        wager.Should().Be(15m);
    }

    [Fact]
    public void TrueCountStepBetRamp_ShouldUseHighestMatchingStep()
    {
        var ramp = new TrueCountStepBetRamp(
        [
            new BetRampStep(1d, 2m),
            new BetRampStep(2d, 4m),
            new BetRampStep(3d, 8m)
        ]);

        var context = CreateContext(trueCount: 2.7d, bankroll: 500m, minimumWager: 10m, maximumWager: 500m, unitSize: 10m);

        var wager = ramp.GetWager(context);

        wager.Should().Be(40m);
    }

    [Fact]
    public void TrueCountStepBetRamp_ShouldFallbackWhenNoTrueCountAvailable()
    {
        var ramp = new TrueCountStepBetRamp(
        [
            new BetRampStep(1d, 2m)
        ],
        fallbackUnits: 1m);

        var snapshot = new CardCountingSnapshot("KO", false, false, 3d, null, 1d, new Dictionary<string, double>());
        var context = new BetRampContext(100m, 10m, 100m, 10m, snapshot);

        var wager = ramp.GetWager(context);

        wager.Should().Be(10m);
    }

    [Fact]
    public void BetRampContext_ShouldClampWagerToBankrollLimits()
    {
        var ramp = new TrueCountStepBetRamp(
        [
            new BetRampStep(1d, 20m)
        ]);

        var context = CreateContext(trueCount: 5d, bankroll: 35m, minimumWager: 10m, maximumWager: 35m, unitSize: 5m);

        var wager = ramp.GetWager(context);

        wager.Should().Be(35m);
    }

    private static BetRampContext CreateContext(
        double? trueCount,
        decimal bankroll,
        decimal minimumWager,
        decimal maximumWager,
        decimal unitSize)
    {
        var snapshot = new CardCountingSnapshot(
            "Hi-Lo",
            true,
            false,
            4d,
            trueCount,
            2d,
            new Dictionary<string, double>());

        return new BetRampContext(bankroll, minimumWager, maximumWager, unitSize, snapshot);
    }
}

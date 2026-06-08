using BlackJackData.Enums;
using BlackJackData.Results;
using BlackJackData.Structs;
using BlackJackData.ValueObjects;
using BlackJackStrategy.Betting;
using BlackJackStrategy.Counting.Systems;
using BlackJackStrategy.Models;
using BlackJackStrategy.Strategies;
using FluentAssertions;

namespace BlackJackTests;

public class CountingStrategyBotTests
{
    [Fact]
    public void GetWager_ShouldUseBetRampWithCurrentCountSnapshot()
    {
        var bot = new CountingStrategyBot(
            new HiLoCountingSystem(),
            new TrueCountStepBetRamp(
            [
                new BetRampStep(1d, 2m),
                new BetRampStep(2d, 4m)
            ]),
            new BasicStrategyBot(10m),
            unitSize: 10m);

        bot.OnRoundCompleted(CreateRoundContext(
            playerCards: [C(CardRank.Two), C(CardRank.Three)],
            dealerCards: [C(CardRank.Four), C(CardRank.Five)]));

        var wager = bot.GetWager(new StrategyWagerContext(
            2,
            500m,
            10m,
            BlackJackData.Rules.BlackjackRules.Default,
            104,
            312,
            78,
            false));

        wager.Should().Be(40m);
    }

    [Fact]
    public void Reset_ShouldClearObservedCount()
    {
        var bot = new CountingStrategyBot(
            new HiLoCountingSystem(),
            new FlatBetRamp(),
            new BasicStrategyBot(10m),
            unitSize: 10m);

        bot.OnRoundCompleted(CreateRoundContext(
            playerCards: [C(CardRank.Two)],
            dealerCards: [C(CardRank.Five)]));

        bot.GetCurrentCountSnapshot(1d).RunningCount.Should().Be(2d);

        bot.Reset();

        bot.GetCurrentCountSnapshot(1d).RunningCount.Should().Be(0d);
    }

    [Fact]
    public void GetWager_ShouldReturnZero_WhenBackCountingEntryUnitsAreNotMet()
    {
        var bot = new CountingStrategyBot(
            new HiLoCountingSystem(),
            new FlatBetRamp(),
            new BasicStrategyBot(10m),
            unitSize: 10m,
            minimumBetUnitsToPlay: 2m);

        var wager = bot.GetWager(new StrategyWagerContext(
            1,
            500m,
            10m,
            BlackJackData.Rules.BlackjackRules.Default,
            104,
            312,
            78,
            false));

        wager.Should().Be(0m);
    }

    private static StrategyRoundResultContext CreateRoundContext(CardDef[] playerCards, CardDef[] dealerCards)
    {
        var handResult = new HandResult(
            new SeatId(1),
            new HandId(1),
            HandOutcomeType.Win,
            10m,
            10m,
            playerCards,
            BlackJackData.ValueObjects.HandValue.FromCards(playerCards),
            dealerCards,
            BlackJackData.ValueObjects.HandValue.FromCards(dealerCards));

        var seatResult = new SeatResult(new SeatId(1), [handResult], 10m);
        var roundResult = new RoundResult(new RoundId(1), [seatResult], dealerCards, BlackJackData.ValueObjects.HandValue.FromCards(dealerCards));

        return new StrategyRoundResultContext(
            1,
            100m,
            110m,
            10m,
            roundResult,
            new SimulationRoundRecord(1, 100m, 10m, 10m, 110m, true, false, 100, Array.Empty<SimulationHandRecord>()));
    }

    private static CardDef C(CardRank rank)
    {
        return new CardDef(CardSuit.Spades, rank);
    }
}

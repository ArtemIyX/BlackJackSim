using BlackJackData.Enums;
using BlackJackData.Rules;
using BlackJackData.States;
using BlackJackData.Structs;
using BlackJackData.ValueObjects;
using BlackJackStrategy.Models;
using BlackJackStrategy.Strategies;
using FluentAssertions;

namespace BlackJackTests;

public class BasicStrategyBotTests
{
    private readonly BasicStrategyBot _bot = new(10m);

    [Fact]
    public void GetAction_ShouldSurrenderHard16AgainstDealer10_WhenAllowed()
    {
        var context = CreateContext(
            playerCards: [C(CardRank.Ten), C(CardRank.Six)],
            dealerUpCard: C(CardRank.King),
            legalActions:
            [
                PlayerActionType.Hit,
                PlayerActionType.Stand,
                PlayerActionType.Surrender
            ]);

        _bot.GetAction(context).Should().Be(PlayerActionType.Surrender);
    }

    [Fact]
    public void GetAction_ShouldHitHard16AgainstDealer10_WhenSurrenderNotAllowed()
    {
        var context = CreateContext(
            playerCards: [C(CardRank.Ten), C(CardRank.Six)],
            dealerUpCard: C(CardRank.King),
            legalActions:
            [
                PlayerActionType.Hit,
                PlayerActionType.Stand
            ]);

        _bot.GetAction(context).Should().Be(PlayerActionType.Hit);
    }

    [Fact]
    public void GetAction_ShouldSplitEightsAgainstDealer10_WhenSplitAllowed()
    {
        var context = CreateContext(
            playerCards: [C(CardRank.Eight), C(CardRank.Eight)],
            dealerUpCard: C(CardRank.King),
            legalActions:
            [
                PlayerActionType.Hit,
                PlayerActionType.Stand,
                PlayerActionType.Split
            ]);

        _bot.GetAction(context).Should().Be(PlayerActionType.Split);
    }

    [Fact]
    public void GetAction_ShouldDoubleSoft18AgainstDealer6_WhenAllowed()
    {
        var context = CreateContext(
            playerCards: [C(CardRank.Ace), C(CardRank.Seven)],
            dealerUpCard: C(CardRank.Six),
            legalActions:
            [
                PlayerActionType.Hit,
                PlayerActionType.Stand,
                PlayerActionType.Double
            ]);

        _bot.GetAction(context).Should().Be(PlayerActionType.Double);
    }

    [Fact]
    public void GetAction_ShouldHitSoft18AgainstDealer9()
    {
        var context = CreateContext(
            playerCards: [C(CardRank.Ace), C(CardRank.Seven)],
            dealerUpCard: C(CardRank.Nine),
            legalActions:
            [
                PlayerActionType.Hit,
                PlayerActionType.Stand
            ]);

        _bot.GetAction(context).Should().Be(PlayerActionType.Hit);
    }

    [Fact]
    public void GetAction_ShouldStandHard12AgainstDealer4()
    {
        var context = CreateContext(
            playerCards: [C(CardRank.Ten), C(CardRank.Two)],
            dealerUpCard: C(CardRank.Four),
            legalActions:
            [
                PlayerActionType.Hit,
                PlayerActionType.Stand
            ]);

        _bot.GetAction(context).Should().Be(PlayerActionType.Stand);
    }

    [Fact]
    public void GetAction_ShouldDoubleHard11AgainstDealerAce_WhenDealerHitsSoft17()
    {
        var rules = BlackjackRules.Default with { DealerHitRule = DealerHitRule.HitSoft17 };
        var bot = new BasicStrategyBot(10m, rules);
        var context = CreateContext(
            playerCards: [C(CardRank.Six), C(CardRank.Five)],
            dealerUpCard: C(CardRank.Ace),
            legalActions:
            [
                PlayerActionType.Hit,
                PlayerActionType.Stand,
                PlayerActionType.Double
            ],
            rules: rules);

        bot.GetAction(context).Should().Be(PlayerActionType.Double);
    }

    [Fact]
    public void GetAction_ShouldDeclineInsurance()
    {
        var context = CreateContext(
            playerCards: [C(CardRank.Ten), C(CardRank.Seven)],
            dealerUpCard: C(CardRank.Ace),
            legalActions:
            [
                PlayerActionType.Insurance,
                PlayerActionType.DeclineInsurance
            ]);

        _bot.GetAction(context).Should().Be(PlayerActionType.DeclineInsurance);
    }

    private static StrategyActionContext CreateContext(
        CardDef[] playerCards,
        CardDef dealerUpCard,
        PlayerActionType[] legalActions,
        BlackjackRules? rules = null)
    {
        var activeHand = new HandState(new HandId(1), 10m, playerCards);
        var seat = new SeatState(new SeatId(1), [activeHand], 100m);
        var dealer = new DealerState([dealerUpCard], HoleCardRevealed: true);
        var state = new RoundState(
            new RoundId(1),
            rules ?? BlackjackRules.Default,
            legalActions.Contains(PlayerActionType.Insurance) ? GamePhase.InsuranceDecision : GamePhase.PlayerTurn,
            [seat],
            dealer,
            100,
            new SeatId(1),
            new HandId(1));

        return new StrategyActionContext(1, 100m, state, legalActions);
    }

    private static CardDef C(CardRank rank)
    {
        return new CardDef(CardSuit.Spades, rank);
    }
}

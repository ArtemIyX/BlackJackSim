using BlackJackData.Actions;
using BlackJackData.Enums;
using BlackJackData.Results;
using BlackJackData.Rules;
using BlackJackData.States;
using BlackJackData.Structs;
using BlackJackData.ValueObjects;
using BlackJackEngine.Engine;
using BlackJackEngine.Models;
using BlackJackEngine.Shoe;
using FluentAssertions;

namespace BlackJackTests;

public class BlackjackRoundEngineTests
{
    private readonly BlackjackRoundEngine _engine = new();

    [Fact]
    public void StartRound_ShouldThrow_WhenNoSeatsAreProvided()
    {
        var act = () => _engine.StartRound(
            new RoundStartOptions(new RoundId(1), BlackjackRules.Default, Array.Empty<SeatBet>()),
            CreateShoe());

        act.Should().Throw<ArgumentException>()
            .WithMessage("*At least one seat*");
    }

    [Fact]
    public void StartRound_ShouldDealInitialCards_AndEnterPlayerTurn()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Seven),
            C(CardSuit.Diamonds, CardRank.Nine));

        var state = _engine.StartRound(CreateDefaultOptions(), shoe);

        state.Phase.Should().Be(GamePhase.PlayerTurn);
        state.ActiveSeatId.Should().Be(new SeatId(1));
        state.ActiveHandId.Should().NotBeNull();
        state.Seats.Should().ContainSingle();
        state.Seats[0].Hands.Should().ContainSingle();
        state.Seats[0].Hands[0].Cards.Should().HaveCount(2);
        state.Dealer.Cards.Should().HaveCount(2);
        state.Dealer.UpCard.Should().Be(C(CardSuit.Hearts, CardRank.Six));
        state.ShoeCardsRemaining.Should().Be(0);
    }

    [Fact]
    public void StartRound_ShouldOfferInsurance_WhenDealerShowsAce()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Ace),
            C(CardSuit.Clubs, CardRank.Seven),
            C(CardSuit.Diamonds, CardRank.Nine));

        var state = _engine.StartRound(CreateDefaultOptions(), shoe);

        state.InsuranceOffered.Should().BeTrue();
        state.Phase.Should().Be(GamePhase.InsuranceDecision);
    }

    [Fact]
    public void StartRound_ShouldKeepDealerHoleCardHidden_WhenPeekRuleIsDisabled()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Ace),
            C(CardSuit.Clubs, CardRank.Nine),
            C(CardSuit.Diamonds, CardRank.King));

        var state = _engine.StartRound(CreateDefaultOptions(rules: BlackjackRules.Default with
        {
            DealerPeeksForBlackjack = false
        }), shoe);

        state.Phase.Should().Be(GamePhase.InsuranceDecision);
        state.Dealer.IsBlackjack.Should().BeTrue();
        state.Dealer.HoleCardRevealed.Should().BeFalse();
    }

    [Fact]
    public void StartRound_ShouldSkipInactiveSeats_WhenDealingAndActivatingTurns()
    {
        var options = new RoundStartOptions(
            new RoundId(1),
            BlackjackRules.Default,
            [
                new SeatBet(new SeatId(1), 10m, IsParticipating: false),
                new SeatBet(new SeatId(2), 15m)
            ]);

        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Seven),
            C(CardSuit.Diamonds, CardRank.Nine));

        var state = _engine.StartRound(options, shoe);

        state.Seats[0].Hands[0].Cards.Should().BeEmpty();
        state.Seats[1].Hands[0].Cards.Should().HaveCount(2);
        state.ActiveSeatId.Should().Be(new SeatId(2));
    }

    [Fact]
    public void GetLegalActions_ShouldIncludeHitStandDoubleAndSurrender_ForOpeningHand()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Five),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Four),
            C(CardSuit.Diamonds, CardRank.Nine));

        var state = _engine.StartRound(CreateDefaultOptions(), shoe);

        var actions = _engine.GetLegalActions(state);

        actions.Should().BeEquivalentTo(
            [
                PlayerActionType.Hit,
                PlayerActionType.Stand,
                PlayerActionType.Double,
                PlayerActionType.Surrender
            ]);
    }

    [Fact]
    public void GetLegalActions_ShouldRespectHardTenToElevenDoubleRule()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Four),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Five),
            C(CardSuit.Diamonds, CardRank.Nine));

        var state = _engine.StartRound(CreateDefaultOptions(rules: BlackjackRules.Default with
        {
            DoubleDownRule = DoubleDownRule.HardTenToElevenOnly
        }), shoe);

        var actions = _engine.GetLegalActions(state);

        actions.Should().NotContain(PlayerActionType.Double);
        actions.Should().Contain(PlayerActionType.Hit);
        actions.Should().Contain(PlayerActionType.Stand);
    }

    [Fact]
    public void GetLegalActions_ShouldReturnEmpty_WhenNotInPlayerTurn()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Seven),
            C(CardSuit.Diamonds, CardRank.Nine));

        var state = _engine.StartRound(CreateDefaultOptions(), shoe);
        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Stand), shoe);

        _engine.GetLegalActions(state).Should().BeEmpty();
    }

    [Fact]
    public void GetLegalActions_ShouldReturnInsuranceChoices_DuringInsuranceDecision()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Ace),
            C(CardSuit.Clubs, CardRank.Seven),
            C(CardSuit.Diamonds, CardRank.Nine));

        var state = _engine.StartRound(CreateDefaultOptions(), shoe);

        var actions = _engine.GetLegalActions(state);

        actions.Should().BeEquivalentTo([PlayerActionType.Insurance, PlayerActionType.DeclineInsurance]);
    }

    [Fact]
    public void GetLegalActions_ShouldOmitSurrender_AfterHit()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Five),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Four),
            C(CardSuit.Diamonds, CardRank.Nine),
            C(CardSuit.Spades, CardRank.Two));

        var state = _engine.StartRound(CreateDefaultOptions(), shoe);
        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Hit), shoe);

        var actions = _engine.GetLegalActions(state);

        actions.Should().NotContain(PlayerActionType.Surrender);
        actions.Should().NotContain(PlayerActionType.Double);
        actions.Should().BeEquivalentTo([PlayerActionType.Hit, PlayerActionType.Stand]);
    }

    [Fact]
    public void GetLegalActions_ShouldOmitDouble_ForSoftHandUnderHardOnlyRule()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ace),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Seven),
            C(CardSuit.Diamonds, CardRank.Nine));

        var state = _engine.StartRound(CreateDefaultOptions(rules: BlackjackRules.Default with
        {
            DoubleDownRule = DoubleDownRule.HardNineToElevenOnly
        }), shoe);

        var actions = _engine.GetLegalActions(state);

        actions.Should().NotContain(PlayerActionType.Double);
    }

    [Fact]
    public void ApplyPlayerAction_Hit_ShouldDrawCard_AndKeepPlayerTurn_WhenHandRemainsLive()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Five),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Four),
            C(CardSuit.Diamonds, CardRank.Nine),
            C(CardSuit.Spades, CardRank.Two));

        var state = _engine.StartRound(CreateDefaultOptions(), shoe);

        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Hit), shoe);

        state.Phase.Should().Be(GamePhase.PlayerTurn);
        state.Seats[0].Hands[0].Cards.Should().HaveCount(3);
        state.Seats[0].Hands[0].Value.BestTotal.Should().Be(11);
    }

    [Fact]
    public void ApplyPlayerAction_ShouldThrow_WhenActionTargetsWrongActiveSeat()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Seven),
            C(CardSuit.Diamonds, CardRank.Nine));

        var state = _engine.StartRound(CreateDefaultOptions(), shoe);
        var wrongAction = new PlayerAction(new SeatId(999), state.ActiveHandId!.Value, PlayerActionType.Stand);

        var act = () => _engine.ApplyPlayerAction(state, wrongAction, shoe);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*active seat and hand*");
    }

    [Fact]
    public void ApplyPlayerAction_ShouldThrow_WhenActionIsIllegalForCurrentHand()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Five),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Four),
            C(CardSuit.Diamonds, CardRank.Nine),
            C(CardSuit.Spades, CardRank.Two));

        var state = _engine.StartRound(CreateDefaultOptions(), shoe);
        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Hit), shoe);

        var act = () => _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Double), shoe);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not legal*");
    }

    [Fact]
    public void ApplyPlayerAction_Hit_ShouldAdvanceToDealerTurn_WhenHandBecomesResolved()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Six),
            C(CardSuit.Diamonds, CardRank.Nine),
            C(CardSuit.Spades, CardRank.King));

        var state = _engine.StartRound(CreateDefaultOptions(), shoe);

        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Hit), shoe);

        state.Phase.Should().Be(GamePhase.Payout);
        state.Seats[0].Hands[0].IsBust.Should().BeTrue();
    }

    [Fact]
    public void ApplyPlayerAction_Stand_ShouldAdvanceToNextParticipatingSeat()
    {
        var options = new RoundStartOptions(
            new RoundId(1),
            BlackjackRules.Default with { AllowInsurance = false },
            [
                new SeatBet(new SeatId(1), 10m),
                new SeatBet(new SeatId(2), 20m)
            ]);

        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Eight),
            C(CardSuit.Clubs, CardRank.Six),
            C(CardSuit.Diamonds, CardRank.Nine),
            C(CardSuit.Spades, CardRank.Seven),
            C(CardSuit.Hearts, CardRank.Nine));

        var state = _engine.StartRound(options, shoe);

        state.ActiveSeatId.Should().Be(new SeatId(1));
        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Stand), shoe);

        state.Phase.Should().Be(GamePhase.PlayerTurn);
        state.ActiveSeatId.Should().Be(new SeatId(2));
    }

    [Fact]
    public void ApplyPlayerAction_Insurance_ShouldMarkHandAndAdvanceToPlayerTurn()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Ace),
            C(CardSuit.Clubs, CardRank.Seven),
            C(CardSuit.Diamonds, CardRank.Nine));

        var state = _engine.StartRound(CreateDefaultOptions(), shoe);

        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Insurance), shoe);

        state.Phase.Should().Be(GamePhase.PlayerTurn);
        state.Seats[0].Hands[0].HasInsurance.Should().BeTrue();
        state.Seats[0].Hands[0].InsuranceDecisionMade.Should().BeTrue();
    }

    [Fact]
    public void ApplyPlayerAction_DeclineInsurance_ShouldAdvanceAcrossSeatsBeforePlayerTurn()
    {
        var options = new RoundStartOptions(
            new RoundId(1),
            BlackjackRules.Default,
            [
                new SeatBet(new SeatId(1), 10m),
                new SeatBet(new SeatId(2), 10m)
            ]);

        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Nine),
            C(CardSuit.Clubs, CardRank.Ace),
            C(CardSuit.Diamonds, CardRank.Seven),
            C(CardSuit.Clubs, CardRank.Eight),
            C(CardSuit.Hearts, CardRank.Nine));

        var state = _engine.StartRound(options, shoe);

        state.Phase.Should().Be(GamePhase.InsuranceDecision);
        state.ActiveSeatId.Should().Be(new SeatId(1));

        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.DeclineInsurance), shoe);
        state.Phase.Should().Be(GamePhase.InsuranceDecision);
        state.ActiveSeatId.Should().Be(new SeatId(2));

        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.DeclineInsurance), shoe);
        state.Phase.Should().Be(GamePhase.PlayerTurn);
        state.ActiveSeatId.Should().Be(new SeatId(1));
        state.Seats.All(seat => seat.Hands[0].InsuranceDecisionMade).Should().BeTrue();
    }

    [Fact]
    public void ApplyPlayerAction_Double_ShouldDoubleWager_DrawOneCard_AndAdvance()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Five),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Six),
            C(CardSuit.Diamonds, CardRank.Nine),
            C(CardSuit.Spades, CardRank.Ten));

        var state = _engine.StartRound(CreateDefaultOptions(wager: 10m), shoe);

        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Double), shoe);

        state.Phase.Should().Be(GamePhase.DealerTurn);
        state.Seats[0].Hands[0].Wager.Should().Be(20m);
        state.Seats[0].Hands[0].IsDoubledDown.Should().BeTrue();
        state.Seats[0].Hands[0].IsStanding.Should().BeTrue();
        state.Seats[0].Hands[0].Cards.Should().HaveCount(3);
    }

    [Fact]
    public void ApplyPlayerAction_Split_ShouldCreateTwoPlayableHands_AndKeepFirstSplitHandActive()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Eight),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Eight),
            C(CardSuit.Diamonds, CardRank.Nine),
            C(CardSuit.Spades, CardRank.Three),
            C(CardSuit.Hearts, CardRank.Four));

        var state = _engine.StartRound(CreateDefaultOptions(), shoe);

        _engine.GetLegalActions(state).Should().Contain(PlayerActionType.Split);

        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Split), shoe);

        state.Phase.Should().Be(GamePhase.PlayerTurn);
        state.Seats[0].Hands.Should().HaveCount(2);
        state.ActiveHandId.Should().Be(state.Seats[0].Hands[0].Id);
        state.Seats[0].Hands[0].Cards.Select(card => card.Rank).Should().Equal(CardRank.Eight, CardRank.Three);
        state.Seats[0].Hands[1].Cards.Select(card => card.Rank).Should().Equal(CardRank.Eight, CardRank.Four);
        state.Seats[0].Hands.Should().OnlyContain(hand => hand.IsSplitHand);
    }

    [Fact]
    public void GetLegalActions_ShouldOmitDouble_AfterSplitWhenRuleDisallowsIt()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Eight),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Eight),
            C(CardSuit.Diamonds, CardRank.Nine),
            C(CardSuit.Spades, CardRank.Three),
            C(CardSuit.Hearts, CardRank.Four));

        var state = _engine.StartRound(CreateDefaultOptions(rules: BlackjackRules.Default with
        {
            AllowDoubleAfterSplit = false
        }), shoe);

        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Split), shoe);

        var actions = _engine.GetLegalActions(state);

        actions.Should().Contain(PlayerActionType.Hit);
        actions.Should().Contain(PlayerActionType.Stand);
        actions.Should().NotContain(PlayerActionType.Double);
        actions.Should().NotContain(PlayerActionType.Surrender);
    }

    [Fact]
    public void ApplyPlayerAction_SplitAces_ShouldAutoResolveHands_WhenHitSplitAcesIsDisabled()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ace),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Ace),
            C(CardSuit.Diamonds, CardRank.Nine),
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Five),
            C(CardSuit.Clubs, CardRank.Two));

        var state = _engine.StartRound(CreateDefaultOptions(rules: BlackjackRules.Default with
        {
            AllowHitSplitAces = false
        }), shoe);

        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Split), shoe);

        state.Phase.Should().Be(GamePhase.DealerTurn);
        state.Seats[0].Hands.Should().HaveCount(2);
        state.Seats[0].Hands.Should().OnlyContain(hand => hand.IsStanding);
    }

    [Fact]
    public void ApplyPlayerAction_ShouldAllowResplitAces_WhenRuleAllowsIt()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ace),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Ace),
            C(CardSuit.Diamonds, CardRank.Nine),
            C(CardSuit.Spades, CardRank.Ace),
            C(CardSuit.Hearts, CardRank.Five),
            C(CardSuit.Clubs, CardRank.Two),
            C(CardSuit.Diamonds, CardRank.Three));

        var state = _engine.StartRound(CreateDefaultOptions(rules: BlackjackRules.Default with
        {
            AllowHitSplitAces = true,
            AllowResplitAces = true
        }), shoe);

        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Split), shoe);

        _engine.GetLegalActions(state).Should().Contain(PlayerActionType.Split);
    }

    [Fact]
    public void ApplyPlayerAction_ShouldThrow_WhenRoundIsNotInPlayerTurn()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Seven),
            C(CardSuit.Diamonds, CardRank.Nine));

        var state = _engine.StartRound(CreateDefaultOptions(), shoe);
        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Stand), shoe);

        var act = () => _engine.ApplyPlayerAction(
            state,
            new PlayerAction(new SeatId(1), new HandId(1), PlayerActionType.Hit),
            shoe);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*player turn or insurance decision*");
    }

    [Fact]
    public void ApplyPlayerAction_Surrender_ShouldMarkHandSurrendered_AndSkipDealerPlay()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Six),
            C(CardSuit.Diamonds, CardRank.Nine));

        var state = _engine.StartRound(CreateDefaultOptions(wager: 10m), shoe);

        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Surrender), shoe);

        state.Phase.Should().Be(GamePhase.Payout);
        state.Seats[0].Hands[0].IsSurrendered.Should().BeTrue();
    }

    [Fact]
    public void StartRound_ShouldGoDirectlyToPayout_WhenDealerHasPeekedBlackjack()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Ace),
            C(CardSuit.Clubs, CardRank.Nine),
            C(CardSuit.Diamonds, CardRank.King));

        var state = _engine.StartRound(CreateDefaultOptions(), shoe);

        state.Phase.Should().Be(GamePhase.Payout);
        state.Dealer.IsBlackjack.Should().BeTrue();
        state.Dealer.HoleCardRevealed.Should().BeTrue();
        state.ActiveSeatId.Should().BeNull();
        state.ActiveHandId.Should().BeNull();
    }

    [Theory]
    [InlineData(DealerHitRule.StandOnSoft17, 2, false)]
    [InlineData(DealerHitRule.HitSoft17, 3, false)]
    public void PlayDealerTurn_ShouldRespectSoft17Rule(DealerHitRule hitRule, int expectedCardCount, bool expectedBust)
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Ace),
            C(CardSuit.Clubs, CardRank.Six),
            C(CardSuit.Diamonds, CardRank.Six),
            C(CardSuit.Spades, CardRank.King));

        var state = _engine.StartRound(CreateDefaultOptions(rules: BlackjackRules.Default with
        {
            DealerHitRule = hitRule,
            SurrenderRule = SurrenderRule.None,
            AllowInsurance = false
        }), shoe);

        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Stand), shoe);
        state.Phase.Should().Be(GamePhase.DealerTurn);

        state = _engine.PlayDealerTurn(state, shoe);

        state.Phase.Should().Be(GamePhase.Payout);
        state.Dealer.Cards.Should().HaveCount(expectedCardCount);
        state.Dealer.IsBust.Should().Be(expectedBust);
    }

    [Fact]
    public void PlayDealerTurn_ShouldThrow_WhenRoundIsNotInDealerTurn()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Seven),
            C(CardSuit.Diamonds, CardRank.Nine));

        var state = _engine.StartRound(CreateDefaultOptions(), shoe);

        var act = () => _engine.PlayDealerTurn(state, shoe);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*dealer turn*");
    }

    [Fact]
    public void ResolveRound_ShouldPayBlackjackAtConfiguredPayout()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ace),
            C(CardSuit.Hearts, CardRank.Nine),
            C(CardSuit.Clubs, CardRank.King),
            C(CardSuit.Diamonds, CardRank.Seven),
            C(CardSuit.Spades, CardRank.Five));

        var state = _engine.StartRound(CreateDefaultOptions(wager: 20m), shoe);
        state.Phase.Should().Be(GamePhase.Payout);

        var result = _engine.ResolveRound(state);

        result.Seats[0].Hands[0].Outcome.Should().Be(HandOutcomeType.Blackjack);
        result.Seats[0].Hands[0].NetPayout.Should().Be(30m);
        result.Seats[0].NetPayout.Should().Be(30m);
    }

    [Fact]
    public void ResolveRound_ShouldReturnPush_WhenBothPlayerAndDealerHaveBlackjack()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ace),
            C(CardSuit.Hearts, CardRank.Ace),
            C(CardSuit.Clubs, CardRank.King),
            C(CardSuit.Diamonds, CardRank.King));

        var state = _engine.StartRound(CreateDefaultOptions(wager: 10m), shoe);

        var result = _engine.ResolveRound(state);

        result.Seats[0].Hands[0].Outcome.Should().Be(HandOutcomeType.Push);
        result.Seats[0].Hands[0].NetPayout.Should().Be(0m);
    }

    [Fact]
    public void ResolveRound_ShouldApplyInsurancePayout_WhenDealerBlackjackAppearsOnNoPeekTable()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Ace),
            C(CardSuit.Clubs, CardRank.Nine),
            C(CardSuit.Diamonds, CardRank.King));

        var state = _engine.StartRound(CreateDefaultOptions(rules: BlackjackRules.Default with
        {
            DealerPeeksForBlackjack = false
        }, wager: 10m), shoe);

        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Insurance), shoe);
        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Stand), shoe);

        var result = _engine.ResolveRound(state);

        result.Seats[0].Hands[0].UsedInsurance.Should().BeTrue();
        result.Seats[0].Hands[0].Outcome.Should().Be(HandOutcomeType.Lose);
        result.Seats[0].Hands[0].NetPayout.Should().Be(0m);
    }

    [Fact]
    public void ResolveRound_ShouldReturnPush_WhenPlayerAndDealerTie()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Nine),
            C(CardSuit.Clubs, CardRank.Seven),
            C(CardSuit.Diamonds, CardRank.Eight));

        var state = _engine.StartRound(CreateDefaultOptions(wager: 10m), shoe);
        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Stand), shoe);
        state = _engine.PlayDealerTurn(state, shoe);

        var result = _engine.ResolveRound(state);

        result.Seats[0].Hands[0].Outcome.Should().Be(HandOutcomeType.Push);
        result.Seats[0].Hands[0].NetPayout.Should().Be(0m);
    }

    [Fact]
    public void ResolveRound_ShouldReturnLoss_WhenDealerBlackjackBeatsNonBlackjackPlayer()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Ace),
            C(CardSuit.Clubs, CardRank.Nine),
            C(CardSuit.Diamonds, CardRank.King));

        var state = _engine.StartRound(CreateDefaultOptions(wager: 10m), shoe);

        var result = _engine.ResolveRound(state);

        result.Seats[0].Hands[0].Outcome.Should().Be(HandOutcomeType.Lose);
        result.Seats[0].Hands[0].NetPayout.Should().Be(-10m);
    }

    [Fact]
    public void ResolveRound_ShouldReturnLoss_WhenPlayerBusts()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Six),
            C(CardSuit.Diamonds, CardRank.Nine),
            C(CardSuit.Spades, CardRank.King));

        var state = _engine.StartRound(CreateDefaultOptions(wager: 10m), shoe);
        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Hit), shoe);

        var result = _engine.ResolveRound(state);

        result.Seats[0].Hands[0].Outcome.Should().Be(HandOutcomeType.Lose);
        result.Seats[0].Hands[0].NetPayout.Should().Be(-10m);
    }

    [Fact]
    public void ResolveRound_ShouldReturnWin_WhenDealerBusts()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Six),
            C(CardSuit.Diamonds, CardRank.Nine),
            C(CardSuit.Spades, CardRank.King));

        var state = _engine.StartRound(CreateDefaultOptions(wager: 10m), shoe);
        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Stand), shoe);
        state = _engine.PlayDealerTurn(state, shoe);

        var result = _engine.ResolveRound(state);

        result.Seats[0].Hands[0].Outcome.Should().Be(HandOutcomeType.Win);
        result.Seats[0].Hands[0].NetPayout.Should().Be(10m);
    }

    [Fact]
    public void ResolveRound_ShouldReturnLoss_WhenDealerHigherWithoutBust()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Nine),
            C(CardSuit.Clubs, CardRank.Seven),
            C(CardSuit.Diamonds, CardRank.Ten));

        var state = _engine.StartRound(CreateDefaultOptions(wager: 10m), shoe);
        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Stand), shoe);
        state = _engine.PlayDealerTurn(state, shoe);

        var result = _engine.ResolveRound(state);

        result.Seats[0].Hands[0].Outcome.Should().Be(HandOutcomeType.Lose);
        result.Seats[0].Hands[0].NetPayout.Should().Be(-10m);
    }

    [Fact]
    public void ResolveRound_ShouldReturnSurrenderLoss_WhenPlayerSurrenders()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Six),
            C(CardSuit.Diamonds, CardRank.Nine));

        var state = _engine.StartRound(CreateDefaultOptions(wager: 10m), shoe);
        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Surrender), shoe);

        var result = _engine.ResolveRound(state);

        result.Seats[0].Hands[0].Outcome.Should().Be(HandOutcomeType.Surrender);
        result.Seats[0].Hands[0].NetPayout.Should().Be(-5m);
    }

    [Fact]
    public void ResolveRound_ShouldUseDoubledWager_ForWinCalculation()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Five),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Six),
            C(CardSuit.Diamonds, CardRank.Nine),
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Clubs, CardRank.Three));

        var state = _engine.StartRound(CreateDefaultOptions(wager: 10m), shoe);
        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Double), shoe);
        state = _engine.PlayDealerTurn(state, shoe);

        var result = _engine.ResolveRound(state);

        result.Seats[0].Hands[0].Outcome.Should().Be(HandOutcomeType.Win);
        result.Seats[0].Hands[0].Wager.Should().Be(20m);
        result.Seats[0].Hands[0].NetPayout.Should().Be(20m);
    }

    [Fact]
    public void ResolveRound_ShouldThrow_WhenRoundIsNotReadyForPayout()
    {
        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Six),
            C(CardSuit.Clubs, CardRank.Seven),
            C(CardSuit.Diamonds, CardRank.Nine));

        var state = _engine.StartRound(CreateDefaultOptions(), shoe);

        var act = () => _engine.ResolveRound(state);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*payout or completed*");
    }

    [Fact]
    public void ResolveRound_ShouldAggregateMultipleSeatPayouts()
    {
        var options = new RoundStartOptions(
            new RoundId(1),
            BlackjackRules.Default,
            [
                new SeatBet(new SeatId(1), 10m),
                new SeatBet(new SeatId(2), 20m)
            ]);

        var shoe = CreateShoe(
            C(CardSuit.Spades, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Nine),
            C(CardSuit.Clubs, CardRank.Seven),
            C(CardSuit.Diamonds, CardRank.Eight),
            C(CardSuit.Clubs, CardRank.Ten),
            C(CardSuit.Hearts, CardRank.Queen));

        var state = _engine.StartRound(options, shoe);
        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Stand), shoe);
        state = _engine.ApplyPlayerAction(state, ActiveAction(state, PlayerActionType.Stand), shoe);
        state = _engine.PlayDealerTurn(state, shoe);

        var result = _engine.ResolveRound(state);

        result.Seats.Should().HaveCount(2);
        result.Seats[0].NetPayout.Should().Be(10m);
        result.Seats[1].NetPayout.Should().Be(20m);
        result.NetPayout.Should().Be(30m);
    }

    private static RoundStartOptions CreateDefaultOptions(decimal wager = 10m, BlackjackRules? rules = null)
    {
        return new RoundStartOptions(
            new RoundId(1),
            rules ?? BlackjackRules.Default,
            [new SeatBet(new SeatId(1), wager)]);
    }

    private static PlayerAction ActiveAction(RoundState state, PlayerActionType actionType)
    {
        state.ActiveSeatId.Should().NotBeNull();
        state.ActiveHandId.Should().NotBeNull();

        return new PlayerAction(state.ActiveSeatId!.Value, state.ActiveHandId!.Value, actionType);
    }

    private static FixedOrderShoe CreateShoe(params CardDef[] cards)
    {
        return new FixedOrderShoe(cards);
    }

    private static CardDef C(CardSuit suit, CardRank rank)
    {
        return new CardDef(suit, rank);
    }
}

using BlackJackData.Actions;
using BlackJackData.Enums;
using BlackJackData.Results;
using BlackJackData.Rules;
using BlackJackData.States;
using BlackJackData.Structs;
using BlackJackData.ValueObjects;
using BlackJackEngine.Contracts;
using BlackJackEngine.Models;

namespace BlackJackEngine.Engine;

public sealed class BlackjackRoundEngine : IBlackjackRoundEngine
{
    public RoundState StartRound(RoundStartOptions options, IBlackjackShoe shoe)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(shoe);

        if (options.Seats.Count == 0)
        {
            throw new ArgumentException("At least one seat is required to start a round.", nameof(options));
        }

        var handNumber = 1;
        var seats = options.Seats
            .Select(seat => new SeatState(
                seat.SeatId,
                new[]
                {
                    new HandState(new HandId(handNumber++), seat.Wager, Array.Empty<CardDef>())
                },
                seat.Bankroll,
                seat.IsParticipating))
            .ToArray();

        var state = new RoundState(
            options.RoundId,
            options.Rules,
            GamePhase.InitialDeal,
            seats,
            new DealerState(Array.Empty<CardDef>(), HoleCardRevealed: false),
            shoe.CardsRemaining);

        state = DealInitialCards(state, shoe);
        return FinalizeInitialDeal(state, shoe);
    }

    public IReadOnlyList<PlayerActionType> GetLegalActions(RoundState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.ActiveSeatId is null || state.ActiveHandId is null)
        {
            return Array.Empty<PlayerActionType>();
        }

        if (state.Phase == GamePhase.InsuranceDecision)
        {
            return
            [
                PlayerActionType.Insurance,
                PlayerActionType.DeclineInsurance
            ];
        }

        if (state.Phase != GamePhase.PlayerTurn)
        {
            return Array.Empty<PlayerActionType>();
        }

        var hand = GetActiveHand(state);
        var seat = GetActiveSeat(state);
        if (!hand.CanReceiveCards || hand.IsBlackjack)
        {
            return Array.Empty<PlayerActionType>();
        }

        var actions = new List<PlayerActionType>
        {
            PlayerActionType.Hit,
            PlayerActionType.Stand
        };

        if (CanDouble(hand, state.Rules))
        {
            actions.Add(PlayerActionType.Double);
        }

        if (CanSplit(seat, hand, state.Rules))
        {
            actions.Add(PlayerActionType.Split);
        }

        if (CanSurrender(hand, state.Rules))
        {
            actions.Add(PlayerActionType.Surrender);
        }

        return actions;
    }

    public RoundState ApplyPlayerAction(RoundState state, PlayerAction action, IBlackjackShoe shoe)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(shoe);

        if (state.Phase != GamePhase.PlayerTurn && state.Phase != GamePhase.InsuranceDecision)
        {
            throw new InvalidOperationException(
                "Player actions can only be applied during the player turn or insurance decision phases.");
        }

        if (state.ActiveSeatId != action.SeatId || state.ActiveHandId != action.HandId)
        {
            throw new InvalidOperationException("The supplied action does not target the active seat and hand.");
        }

        if (!GetLegalActions(state).Contains(action.ActionType))
        {
            throw new InvalidOperationException($"Action '{action.ActionType}' is not legal for the current hand.");
        }

        if (state.Phase == GamePhase.InsuranceDecision)
        {
            var insuranceHand = GetActiveHand(state);
            var insuranceUpdatedHand = action.ActionType == PlayerActionType.Insurance
                ? insuranceHand with { HasInsurance = true, InsuranceDecisionMade = true }
                : insuranceHand with { InsuranceDecisionMade = true };

            var insuranceState = ReplaceHand(state, action.SeatId, insuranceUpdatedHand) with
            {
                ShoeCardsRemaining = shoe.CardsRemaining
            };

            return AdvanceAfterInsuranceDecision(insuranceState);
        }

        var activeHand = GetActiveHand(state);
        if (action.ActionType == PlayerActionType.Split)
        {
            return ApplySplit(state, action.SeatId, activeHand, shoe);
        }

        var updatedHand = action.ActionType switch
        {
            PlayerActionType.Hit => ApplyHit(activeHand, shoe),
            PlayerActionType.Stand => activeHand with { IsStanding = true },
            PlayerActionType.Double => ApplyDouble(activeHand, shoe),
            PlayerActionType.Surrender => activeHand with { IsStanding = true, IsSurrendered = true },
            _ => throw new InvalidOperationException($"Unsupported action '{action.ActionType}'.")
        };

        var updatedState = ReplaceHand(state, action.SeatId, updatedHand) with
        {
            ShoeCardsRemaining = shoe.CardsRemaining
        };

        return AdvanceAfterPlayerAction(updatedState);
    }

    public RoundState PlayDealerTurn(RoundState state, IBlackjackShoe shoe)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(shoe);

        if (state.Phase != GamePhase.DealerTurn)
        {
            throw new InvalidOperationException("Dealer play can only occur during the dealer turn phase.");
        }

        var dealer = state.Dealer with { HoleCardRevealed = true };

        while (ShouldDealerHit(dealer, state.Rules))
        {
            dealer = dealer with { Cards = AppendCard(dealer.Cards, shoe.Draw()) };
        }

        return state with
        {
            Dealer = dealer,
            Phase = GamePhase.Payout,
            ShoeCardsRemaining = shoe.CardsRemaining,
            ActiveSeatId = null,
            ActiveHandId = null
        };
    }

    public RoundResult ResolveRound(RoundState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Phase != GamePhase.Payout && state.Phase != GamePhase.Completed)
        {
            throw new InvalidOperationException(
                "The round must be in payout or completed phase before it can be resolved.");
        }

        var dealerValue = state.Dealer.Value;
        var seatResults = state.Seats
            .Select(seat =>
            {
                var handResults = seat.Hands
                    .Select(hand => ResolveHand(seat.Id, state.Rules, hand, dealerValue))
                    .ToArray();

                return new SeatResult(seat.Id, handResults, handResults.Sum(result => result.NetPayout));
            })
            .ToArray();

        return new RoundResult(state.Id, seatResults, state.Dealer.Cards.ToArray(), dealerValue);
    }

    private static RoundState DealInitialCards(RoundState state, IBlackjackShoe shoe)
    {
        foreach (var seat in state.Seats.Where(seat => seat.IsParticipating))
        {
            state = DealCardToSeatHand(state, seat.Id, seat.Hands[0].Id, shoe.Draw());
        }

        state = state with
        {
            Dealer = state.Dealer with { Cards = AppendCard(state.Dealer.Cards, shoe.Draw()) }
        };

        foreach (var seat in state.Seats.Where(seat => seat.IsParticipating))
        {
            state = DealCardToSeatHand(state, seat.Id, seat.Hands[0].Id, shoe.Draw());
        }

        return state with
        {
            Dealer = state.Dealer with { Cards = AppendCard(state.Dealer.Cards, shoe.Draw()) },
            ShoeCardsRemaining = shoe.CardsRemaining
        };
    }

    private static RoundState FinalizeInitialDeal(RoundState state, IBlackjackShoe shoe)
    {
        var dealer = state.Dealer;
        var insuranceOffered = state.Rules.AllowInsurance && dealer.UpCard?.Rank == CardRank.Ace;
        var dealerHasBlackjack = dealer.IsBlackjack;
        var shouldPeek = state.Rules.DealerPeeksForBlackjack && IsPeekCard(dealer.UpCard);

        if (shouldPeek)
        {
            state = state with { Dealer = dealer with { HoleCardRevealed = dealerHasBlackjack } };
        }

        if (dealerHasBlackjack && shouldPeek)
        {
            return state with
            {
                Phase = GamePhase.Payout,
                InsuranceOffered = insuranceOffered,
                ActiveSeatId = null,
                ActiveHandId = null,
                ShoeCardsRemaining = shoe.CardsRemaining
            };
        }

        if (insuranceOffered && TryGetNextInsuranceSeat(state, out var insuranceSeatId, out var insuranceHandId))
        {
            return state with
            {
                Phase = GamePhase.InsuranceDecision,
                InsuranceOffered = true,
                ActiveSeatId = insuranceSeatId,
                ActiveHandId = insuranceHandId,
                ShoeCardsRemaining = shoe.CardsRemaining
            };
        }

        if (TryGetNextActiveHand(state, out var activeSeatId, out var activeHandId))
        {
            return state with
            {
                Phase = GamePhase.PlayerTurn,
                InsuranceOffered = insuranceOffered,
                ActiveSeatId = activeSeatId,
                ActiveHandId = activeHandId,
                ShoeCardsRemaining = shoe.CardsRemaining
            };
        }

        return state with
        {
            Phase = ShouldDealerPlay(state) ? GamePhase.DealerTurn : GamePhase.Payout,
            InsuranceOffered = insuranceOffered,
            ActiveSeatId = null,
            ActiveHandId = null,
            ShoeCardsRemaining = shoe.CardsRemaining
        };
    }

    private static bool CanDouble(HandState hand, BlackjackRules rules)
    {
        if (hand.Cards.Count != 2 || hand.IsDoubledDown)
        {
            return false;
        }

        if (hand.IsSplitHand && !rules.AllowDoubleAfterSplit)
        {
            return false;
        }

        return rules.DoubleDownRule switch
        {
            DoubleDownRule.AnyTwoCards => true,
            DoubleDownRule.HardNineToElevenOnly => !hand.Value.IsSoft && hand.Value.BestTotal is >= 9 and <= 11,
            DoubleDownRule.HardTenToElevenOnly => !hand.Value.IsSoft && hand.Value.BestTotal is >= 10 and <= 11,
            _ => false
        };
    }

    private static bool CanSurrender(HandState hand, BlackjackRules rules)
    {
        return rules.SurrenderRule != SurrenderRule.None && hand.Cards.Count == 2 && !hand.IsSplitHand;
    }

    private static bool CanSplit(SeatState seat, HandState hand, BlackjackRules rules)
    {
        if (hand.Cards.Count != 2 || seat.Hands.Count >= rules.MaxHandsPerSeat)
        {
            return false;
        }

        if (hand.Cards[0].Rank != hand.Cards[1].Rank)
        {
            return false;
        }

        if (hand.IsSplitHand)
        {
            if (hand.Cards[0].Rank == CardRank.Ace)
            {
                return rules.AllowResplitAces;
            }

            return rules.AllowResplitHands;
        }

        return true;
    }

    private static HandState ApplyHit(HandState hand, IBlackjackShoe shoe)
    {
        var updatedHand = hand with { Cards = AppendCard(hand.Cards, shoe.Draw()) };
        return updatedHand.Value.BestTotal >= 21
            ? updatedHand with { IsStanding = true }
            : updatedHand;
    }

    private static HandState ApplyDouble(HandState hand, IBlackjackShoe shoe)
    {
        return hand with
        {
            Wager = hand.Wager * 2m,
            IsDoubledDown = true,
            IsStanding = true,
            Cards = AppendCard(hand.Cards, shoe.Draw())
        };
    }

    private static RoundState ApplySplit(RoundState state, SeatId seatId, HandState hand, IBlackjackShoe shoe)
    {
        var nextHandIdValue = state.Seats
            .SelectMany(seat => seat.Hands)
            .Select(existingHand => existingHand.Id.Value)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var isAceSplit = hand.Cards[0].Rank == CardRank.Ace;
        var replacementOne = shoe.Draw();
        var replacementTwo = shoe.Draw();
        var splitDepth = hand.SplitDepth + 1;
        var resolveSplitAcesImmediately = isAceSplit && !state.Rules.AllowHitSplitAces;

        var firstHand = new HandState(
            hand.Id,
            hand.Wager,
            [hand.Cards[0], replacementOne],
            IsStanding: resolveSplitAcesImmediately,
            IsDoubledDown: false,
            IsSurrendered: false,
            IsSplitHand: true,
            HasInsurance: hand.HasInsurance,
            InsuranceDecisionMade: hand.InsuranceDecisionMade,
            SplitDepth: splitDepth);

        var secondHand = new HandState(
            new HandId(nextHandIdValue),
            hand.Wager,
            [hand.Cards[1], replacementTwo],
            IsStanding: resolveSplitAcesImmediately,
            IsDoubledDown: false,
            IsSurrendered: false,
            IsSplitHand: true,
            HasInsurance: false,
            InsuranceDecisionMade: hand.InsuranceDecisionMade,
            SplitDepth: splitDepth);

        var updatedSeats = state.Seats
            .Select(seat => seat.Id != seatId
                ? seat
                : seat with
                {
                    Hands = ReplaceHandWithSplitHands(seat.Hands, hand.Id, firstHand, secondHand)
                })
            .ToArray();

        var splitState = state with
        {
            Seats = updatedSeats,
            ActiveSeatId = seatId,
            ActiveHandId = firstHand.Id,
            ShoeCardsRemaining = shoe.CardsRemaining
        };

        return AdvanceAfterPlayerAction(splitState);
    }

    private static RoundState AdvanceAfterInsuranceDecision(RoundState state)
    {
        if (TryGetNextInsuranceSeat(state, out var nextSeatId, out var nextHandId))
        {
            return state with
            {
                Phase = GamePhase.InsuranceDecision,
                ActiveSeatId = nextSeatId,
                ActiveHandId = nextHandId
            };
        }

        if (TryGetNextActiveHand(state with { ActiveSeatId = null, ActiveHandId = null }, out var activeSeatId,
                out var activeHandId))
        {
            return state with
            {
                Phase = GamePhase.PlayerTurn,
                ActiveSeatId = activeSeatId,
                ActiveHandId = activeHandId
            };
        }

        return state with
        {
            Phase = ShouldDealerPlay(state) ? GamePhase.DealerTurn : GamePhase.Payout,
            ActiveSeatId = null,
            ActiveHandId = null
        };
    }

    private static RoundState AdvanceAfterPlayerAction(RoundState state)
    {
        if (state.ActiveSeatId is not null && state.ActiveHandId is not null)
        {
            var currentHand = GetActiveHand(state);
            if (!currentHand.IsResolved)
            {
                return state with
                {
                    Phase = GamePhase.PlayerTurn
                };
            }
        }

        if (TryGetNextActiveHand(state, out var nextSeatId, out var nextHandId))
        {
            return state with
            {
                Phase = GamePhase.PlayerTurn,
                ActiveSeatId = nextSeatId,
                ActiveHandId = nextHandId
            };
        }

        return state with
        {
            Phase = ShouldDealerPlay(state) ? GamePhase.DealerTurn : GamePhase.Payout,
            ActiveSeatId = null,
            ActiveHandId = null
        };
    }

    private static bool TryGetNextInsuranceSeat(RoundState state, out SeatId? seatId, out HandId? handId)
    {
        var beginSearching = state.ActiveSeatId is null || state.ActiveHandId is null;

        foreach (var seat in state.Seats.Where(seat => seat.IsParticipating))
        {
            var hand = seat.Hands.FirstOrDefault();
            if (hand is null)
            {
                continue;
            }

            if (!beginSearching)
            {
                if (seat.Id == state.ActiveSeatId && hand.Id == state.ActiveHandId)
                {
                    beginSearching = true;
                }

                continue;
            }

            if (!hand.InsuranceDecisionMade)
            {
                seatId = seat.Id;
                handId = hand.Id;
                return true;
            }
        }

        seatId = null;
        handId = null;
        return false;
    }

    private static bool TryGetNextActiveHand(RoundState state, out SeatId? seatId, out HandId? handId)
    {
        var beginSearching = state.ActiveSeatId is null || state.ActiveHandId is null;

        foreach (var seat in state.Seats.Where(seat => seat.IsParticipating))
        {
            foreach (var hand in seat.Hands)
            {
                if (!beginSearching)
                {
                    if (seat.Id == state.ActiveSeatId && hand.Id == state.ActiveHandId)
                    {
                        beginSearching = true;
                    }

                    continue;
                }

                if (!hand.IsResolved)
                {
                    seatId = seat.Id;
                    handId = hand.Id;
                    return true;
                }
            }
        }

        seatId = null;
        handId = null;
        return false;
    }

    private static bool ShouldDealerPlay(RoundState state)
    {
        if (state.Dealer.IsBlackjack)
        {
            return false;
        }

        return state.Seats
            .Where(seat => seat.IsParticipating)
            .SelectMany(seat => seat.Hands)
            .Any(hand => !hand.IsBust && !hand.IsSurrendered && !hand.IsBlackjack);
    }

    private static bool ShouldDealerHit(DealerState dealer, BlackjackRules rules)
    {
        if (dealer.Value.IsBust)
        {
            return false;
        }

        if (dealer.Value.BestTotal < 17)
        {
            return true;
        }

        return rules.DealerHitRule == DealerHitRule.HitSoft17 &&
               dealer.Value.BestTotal == 17 &&
               dealer.Value.IsSoft;
    }

    private static bool IsPeekCard(CardDef? card)
    {
        if (card is null)
        {
            return false;
        }

        return card.Value.Rank is CardRank.Ace or CardRank.Ten or CardRank.Jack or CardRank.Queen or CardRank.King;
    }

    private static SeatState GetActiveSeat(RoundState state)
    {
        return state.Seats.FirstOrDefault(seat => seat.Id == state.ActiveSeatId)
               ?? throw new InvalidOperationException("The active seat was not found in the round state.");
    }

    private static RoundState ReplaceHand(RoundState state, SeatId seatId, HandState updatedHand)
    {
        return state with
        {
            Seats = state.Seats
                .Select(seat => seat.Id != seatId
                    ? seat
                    : seat with
                    {
                        Hands = seat.Hands
                            .Select(hand => hand.Id == updatedHand.Id ? updatedHand : hand)
                            .ToArray()
                    })
                .ToArray()
        };
    }

    private static HandState[] ReplaceHandWithSplitHands(
        IReadOnlyList<HandState> hands,
        HandId handId,
        HandState firstHand,
        HandState secondHand)
    {
        var updatedHands = new List<HandState>(hands.Count + 1);

        foreach (var hand in hands)
        {
            if (hand.Id == handId)
            {
                updatedHands.Add(firstHand);
                updatedHands.Add(secondHand);
                continue;
            }

            updatedHands.Add(hand);
        }

        return updatedHands.ToArray();
    }

    private static RoundState DealCardToSeatHand(RoundState state, SeatId seatId, HandId handId, CardDef card)
    {
        return state with
        {
            Seats = state.Seats
                .Select(seat => seat.Id != seatId
                    ? seat
                    : seat with
                    {
                        Hands = seat.Hands
                            .Select(hand =>
                                hand.Id != handId ? hand : hand with { Cards = AppendCard(hand.Cards, card) })
                            .ToArray()
                    })
                .ToArray()
        };
    }

    private static CardDef[] AppendCard(IReadOnlyList<CardDef> cards, CardDef card)
    {
        var updated = new CardDef[cards.Count + 1];
        for (var index = 0; index < cards.Count; index++)
        {
            updated[index] = cards[index];
        }

        updated[^1] = card;
        return updated;
    }

    private static HandState GetActiveHand(RoundState state)
    {
        return state.Seats
                   .Where(seat => seat.Id == state.ActiveSeatId)
                   .SelectMany(seat => seat.Hands)
                   .FirstOrDefault(hand => hand.Id == state.ActiveHandId)
               ?? throw new InvalidOperationException("The active hand was not found in the round state.");
    }

    private static HandResult ResolveHand(SeatId seatId, BlackjackRules rules, HandState hand, HandValue dealerValue)
    {
        var playerValue = hand.Value;
        var insuranceNet = hand.HasInsurance
            ? (dealerValue.IsBlackjack ? hand.Wager * 0.5m * rules.InsurancePayout : -hand.Wager * 0.5m)
            : 0m;

        if (hand.IsSurrendered)
        {
            return new HandResult(seatId, hand.Id, HandOutcomeType.Surrender, hand.Wager,
                insuranceNet - (hand.Wager / 2m), hand.Cards.ToArray(), playerValue, Array.Empty<CardDef>(), dealerValue, hand.HasInsurance);
        }

        if (playerValue.IsBust)
        {
            return new HandResult(seatId, hand.Id, HandOutcomeType.Lose, hand.Wager, insuranceNet - hand.Wager,
                hand.Cards.ToArray(), playerValue, Array.Empty<CardDef>(), dealerValue, hand.HasInsurance);
        }

        if (playerValue.IsBlackjack && dealerValue.IsBlackjack)
        {
            return new HandResult(seatId, hand.Id, HandOutcomeType.Push, hand.Wager, insuranceNet,
                hand.Cards.ToArray(), playerValue, Array.Empty<CardDef>(), dealerValue, hand.HasInsurance);
        }

        if (playerValue.IsBlackjack)
        {
            return new HandResult(seatId, hand.Id, HandOutcomeType.Blackjack, hand.Wager,
                insuranceNet + (hand.Wager * rules.BlackjackPayout), hand.Cards.ToArray(), playerValue, Array.Empty<CardDef>(), dealerValue, hand.HasInsurance);
        }

        if (dealerValue.IsBlackjack)
        {
            return new HandResult(seatId, hand.Id, HandOutcomeType.Lose, hand.Wager, insuranceNet - hand.Wager,
                hand.Cards.ToArray(), playerValue, Array.Empty<CardDef>(), dealerValue, hand.HasInsurance);
        }

        if (dealerValue.IsBust)
        {
            return new HandResult(seatId, hand.Id, HandOutcomeType.Win, hand.Wager, insuranceNet + hand.Wager,
                hand.Cards.ToArray(), playerValue, Array.Empty<CardDef>(), dealerValue, hand.HasInsurance);
        }

        if (playerValue.BestTotal > dealerValue.BestTotal)
        {
            return new HandResult(seatId, hand.Id, HandOutcomeType.Win, hand.Wager, insuranceNet + hand.Wager,
                hand.Cards.ToArray(), playerValue, Array.Empty<CardDef>(), dealerValue, hand.HasInsurance);
        }

        if (playerValue.BestTotal < dealerValue.BestTotal)
        {
            return new HandResult(seatId, hand.Id, HandOutcomeType.Lose, hand.Wager, insuranceNet - hand.Wager,
                hand.Cards.ToArray(), playerValue, Array.Empty<CardDef>(), dealerValue, hand.HasInsurance);
        }

        return new HandResult(seatId, hand.Id, HandOutcomeType.Push, hand.Wager, insuranceNet, hand.Cards.ToArray(), playerValue, Array.Empty<CardDef>(), dealerValue,
            hand.HasInsurance);
    }
}

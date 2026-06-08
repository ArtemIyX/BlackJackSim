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

        if (state.Phase != GamePhase.PlayerTurn || state.ActiveSeatId is null || state.ActiveHandId is null)
        {
            return Array.Empty<PlayerActionType>();
        }

        var hand = GetActiveHand(state);
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

        if (state.Phase != GamePhase.PlayerTurn)
        {
            throw new InvalidOperationException("Player actions can only be applied during the player turn phase.");
        }

        if (state.ActiveSeatId != action.SeatId || state.ActiveHandId != action.HandId)
        {
            throw new InvalidOperationException("The supplied action does not target the active seat and hand.");
        }

        if (!GetLegalActions(state).Contains(action.ActionType))
        {
            throw new InvalidOperationException($"Action '{action.ActionType}' is not legal for the current hand.");
        }

        var activeHand = GetActiveHand(state);
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
            throw new InvalidOperationException("The round must be in payout or completed phase before it can be resolved.");
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

        return new RoundResult(state.Id, seatResults, dealerValue);
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

    private static RoundState AdvanceAfterPlayerAction(RoundState state)
    {
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
                            .Select(hand => hand.Id != handId ? hand : hand with { Cards = AppendCard(hand.Cards, card) })
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
            return new HandResult(seatId, hand.Id, HandOutcomeType.Surrender, hand.Wager, insuranceNet - (hand.Wager / 2m), playerValue, dealerValue, hand.HasInsurance);
        }

        if (playerValue.IsBust)
        {
            return new HandResult(seatId, hand.Id, HandOutcomeType.Lose, hand.Wager, insuranceNet - hand.Wager, playerValue, dealerValue, hand.HasInsurance);
        }

        if (playerValue.IsBlackjack && dealerValue.IsBlackjack)
        {
            return new HandResult(seatId, hand.Id, HandOutcomeType.Push, hand.Wager, insuranceNet, playerValue, dealerValue, hand.HasInsurance);
        }

        if (playerValue.IsBlackjack)
        {
            return new HandResult(seatId, hand.Id, HandOutcomeType.Blackjack, hand.Wager, insuranceNet + (hand.Wager * rules.BlackjackPayout), playerValue, dealerValue, hand.HasInsurance);
        }

        if (dealerValue.IsBlackjack)
        {
            return new HandResult(seatId, hand.Id, HandOutcomeType.Lose, hand.Wager, insuranceNet - hand.Wager, playerValue, dealerValue, hand.HasInsurance);
        }

        if (dealerValue.IsBust)
        {
            return new HandResult(seatId, hand.Id, HandOutcomeType.Win, hand.Wager, insuranceNet + hand.Wager, playerValue, dealerValue, hand.HasInsurance);
        }

        if (playerValue.BestTotal > dealerValue.BestTotal)
        {
            return new HandResult(seatId, hand.Id, HandOutcomeType.Win, hand.Wager, insuranceNet + hand.Wager, playerValue, dealerValue, hand.HasInsurance);
        }

        if (playerValue.BestTotal < dealerValue.BestTotal)
        {
            return new HandResult(seatId, hand.Id, HandOutcomeType.Lose, hand.Wager, insuranceNet - hand.Wager, playerValue, dealerValue, hand.HasInsurance);
        }

        return new HandResult(seatId, hand.Id, HandOutcomeType.Push, hand.Wager, insuranceNet, playerValue, dealerValue, hand.HasInsurance);
    }
}

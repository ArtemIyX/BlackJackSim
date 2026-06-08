using BlackJackData.Enums;
using BlackJackData.Rules;
using BlackJackData.States;
using BlackJackStrategy.Contracts;
using BlackJackStrategy.Models;
using BlackJackStrategy.Tables;

namespace BlackJackStrategy.Strategies;

public sealed class BasicStrategyBot : IBlackjackStrategy
{
    private readonly BasicStrategyTables _tables;

    public BasicStrategyBot(decimal wager)
        : this(wager, BlackjackRules.Default)
    {
    }

    public BasicStrategyBot(decimal wager, BlackjackRules rules)
    {
        if (wager <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(wager), wager, "Wager must be positive.");
        }

        Wager = wager;
        _tables = BasicStrategyTableGenerator.Generate(rules);
    }

    public decimal Wager { get; }

    public decimal GetWager(StrategyWagerContext context)
    {
        return Math.Min(Wager, context.Bankroll);
    }

    public PlayerActionType GetAction(StrategyActionContext context)
    {
        if (context.LegalActions.Contains(PlayerActionType.Insurance))
        {
            return PlayerActionType.DeclineInsurance;
        }

        var activeHand = GetActiveHand(context.State);
        var dealerUpCard = context.State.DealerUpCard
                           ?? throw new InvalidOperationException("Dealer up card is not available.");
        var dealerValue = GetDealerValue(dealerUpCard.Rank);

        var tableAction = TryGetTableAction(activeHand, dealerValue);
        if (tableAction.HasValue)
        {
            return ResolveLegalAction(tableAction.Value, context.LegalActions);
        }

        return ResolveLegalAction(PlayerActionType.Stand, context.LegalActions);
    }

    private PlayerActionType? TryGetTableAction(HandState activeHand, int dealerValue)
    {
        if (activeHand.Cards.Count == 2 && activeHand.Cards[0].Rank == activeHand.Cards[1].Rank)
        {
            var pairValue = GetDealerValue(activeHand.Cards[0].Rank);
            return _tables.GetPairAction(pairValue, dealerValue);
        }

        if (activeHand.Value.IsSoft)
        {
            return _tables.GetSoftAction(activeHand.Value.BestTotal, dealerValue);
        }

        return _tables.GetHardAction(activeHand.Value.BestTotal, dealerValue);
    }

    public void OnRoundCompleted(StrategyRoundResultContext context)
    {
    }

    public void Reset()
    {
    }

    private static PlayerActionType ResolveLegalAction(PlayerActionType tableAction, IReadOnlyList<PlayerActionType> legalActions)
    {
        if (legalActions.Contains(tableAction))
        {
            return tableAction;
        }

        if (tableAction == PlayerActionType.Double)
        {
            if (legalActions.Contains(PlayerActionType.Hit))
            {
                return PlayerActionType.Hit;
            }

            if (legalActions.Contains(PlayerActionType.Stand))
            {
                return PlayerActionType.Stand;
            }
        }

        if (tableAction == PlayerActionType.Split)
        {
            if (legalActions.Contains(PlayerActionType.Hit))
            {
                return PlayerActionType.Hit;
            }

            if (legalActions.Contains(PlayerActionType.Stand))
            {
                return PlayerActionType.Stand;
            }
        }

        if (tableAction == PlayerActionType.Surrender)
        {
            if (legalActions.Contains(PlayerActionType.Hit))
            {
                return PlayerActionType.Hit;
            }

            if (legalActions.Contains(PlayerActionType.Stand))
            {
                return PlayerActionType.Stand;
            }
        }

        return legalActions.First();
    }

    private static HandState GetActiveHand(RoundState state)
    {
        if (state.ActiveSeatId is null || state.ActiveHandId is null)
        {
            throw new InvalidOperationException("No active hand is available.");
        }

        return state.Seats
                   .Where(seat => seat.Id == state.ActiveSeatId)
                   .SelectMany(seat => seat.Hands)
                   .First(hand => hand.Id == state.ActiveHandId);
    }

    private static int GetDealerValue(CardRank rank)
    {
        return rank switch
        {
            CardRank.Ace => 11,
            >= CardRank.Jack and <= CardRank.King => 10,
            _ => (int)rank
        };
    }
}

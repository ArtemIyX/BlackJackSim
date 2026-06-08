using BlackJackData.Enums;
using BlackJackStrategy.Contracts;
using BlackJackStrategy.Models;

namespace BlackJackStrategy.Strategies;

public sealed class FixedBetFallbackStrategy : IBlackjackStrategy
{
    public FixedBetFallbackStrategy(decimal wager)
    {
        if (wager <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(wager), wager, "Wager must be positive.");
        }

        Wager = wager;
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

        if (context.State.ActiveSeatId is null || context.State.ActiveHandId is null)
        {
            throw new InvalidOperationException("No active hand is available for strategy action selection.");
        }

        var activeHand = context.State.Seats
            .Where(seat => seat.Id == context.State.ActiveSeatId)
            .SelectMany(seat => seat.Hands)
            .First(hand => hand.Id == context.State.ActiveHandId);

        if (context.LegalActions.Contains(PlayerActionType.Split) &&
            activeHand.Cards.Count == 2 &&
            activeHand.Cards[0].Rank is CardRank.Ace or CardRank.Eight)
        {
            return PlayerActionType.Split;
        }

        if (context.LegalActions.Contains(PlayerActionType.Double) && activeHand.Value.BestTotal == 11)
        {
            return PlayerActionType.Double;
        }

        if (activeHand.Value.BestTotal >= 17 && context.LegalActions.Contains(PlayerActionType.Stand))
        {
            return PlayerActionType.Stand;
        }

        if (activeHand.Value.BestTotal < 17 && context.LegalActions.Contains(PlayerActionType.Hit))
        {
            return PlayerActionType.Hit;
        }

        if (context.LegalActions.Contains(PlayerActionType.Stand))
        {
            return PlayerActionType.Stand;
        }

        return context.LegalActions.First();
    }

    public void OnRoundCompleted(StrategyRoundResultContext context)
    {
    }

    public void Reset()
    {
    }
}

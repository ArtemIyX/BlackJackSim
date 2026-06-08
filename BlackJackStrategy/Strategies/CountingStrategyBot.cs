using BlackJackData.Enums;
using BlackJackStrategy.Contracts;
using BlackJackStrategy.Models;

namespace BlackJackStrategy.Strategies;

public sealed class CountingStrategyBot : IBlackjackStrategy
{
    private readonly ICardCountingSystem _countingSystem;
    private readonly IBetRamp _betRamp;
    private readonly IBlackjackStrategy _playStrategy;

    public CountingStrategyBot(
        ICardCountingSystem countingSystem,
        IBetRamp betRamp,
        IBlackjackStrategy playStrategy,
        decimal unitSize)
    {
        if (unitSize <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(unitSize), unitSize, "Unit size must be positive.");
        }

        _countingSystem = countingSystem ?? throw new ArgumentNullException(nameof(countingSystem));
        _betRamp = betRamp ?? throw new ArgumentNullException(nameof(betRamp));
        _playStrategy = playStrategy ?? throw new ArgumentNullException(nameof(playStrategy));
        UnitSize = unitSize;
    }

    public decimal UnitSize { get; }

    public decimal GetWager(StrategyWagerContext context)
    {
        var decksRemaining = context.TotalCards == 0 ? 0d : (double)context.CardsRemaining / 52d;
        var snapshot = context.CountSnapshot ?? _countingSystem.GetSnapshot(decksRemaining);

        var rampContext = new BetRampContext(
            context.Bankroll,
            context.MinimumWager,
            context.Bankroll,
            UnitSize,
            snapshot);

        return _betRamp.GetWager(rampContext);
    }

    public PlayerActionType GetAction(StrategyActionContext context)
    {
        return _playStrategy.GetAction(context);
    }

    public CardCountingSnapshot GetCurrentCountSnapshot(double decksRemaining)
    {
        return _countingSystem.GetSnapshot(decksRemaining);
    }

    public void OnRoundCompleted(StrategyRoundResultContext context)
    {
        foreach (var seat in context.RoundResult.Seats)
        {
            foreach (var hand in seat.Hands)
            {
                _countingSystem.ObserveCards(hand.PlayerCards);
            }
        }

        _countingSystem.ObserveCards(context.RoundResult.DealerCards);

        _playStrategy.OnRoundCompleted(context);
    }

    public void Reset()
    {
        _countingSystem.Reset();
        _playStrategy.Reset();
    }
}

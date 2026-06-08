using BlackJackData.Enums;
using BlackJackStrategy.Models;

namespace BlackJackStrategy.Contracts;

public interface IBlackjackStrategy
{
    decimal GetWager(StrategyWagerContext context);

    PlayerActionType GetAction(StrategyActionContext context);

    void OnRoundCompleted(StrategyRoundResultContext context);

    void Reset();
}

using BlackJackData.Actions;
using BlackJackData.Enums;
using BlackJackData.Results;
using BlackJackData.States;
using BlackJackEngine.Models;

namespace BlackJackEngine.Contracts;

public interface IBlackjackRoundEngine
{
    RoundState StartRound(RoundStartOptions options, IBlackjackShoe shoe);

    IReadOnlyList<PlayerActionType> GetLegalActions(RoundState state);

    RoundState ApplyPlayerAction(RoundState state, PlayerAction action, IBlackjackShoe shoe);

    RoundState PlayDealerTurn(RoundState state, IBlackjackShoe shoe);

    RoundResult ResolveRound(RoundState state);
}

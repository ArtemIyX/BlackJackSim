using BlackJackData.Enums;
using BlackJackData.ValueObjects;

namespace BlackJackData.Actions;

public readonly record struct PlayerAction(
    SeatId SeatId,
    HandId HandId,
    PlayerActionType ActionType,
    decimal Amount = 0m);

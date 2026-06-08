using BlackJackData.Enums;
using BlackJackData.States;

namespace BlackJackStrategy.Models;

public sealed record StrategyActionContext(
    long RoundNumber,
    decimal CurrentBankroll,
    RoundState State,
    IReadOnlyList<PlayerActionType> LegalActions);

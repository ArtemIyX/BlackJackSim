using BlackJackData.Results;

namespace BlackJackStrategy.Models;

public sealed record StrategyRoundResultContext(
    long RoundNumber,
    decimal BankrollBeforeRound,
    decimal BankrollAfterRound,
    decimal Wager,
    RoundResult RoundResult,
    SimulationRoundRecord RoundRecord);

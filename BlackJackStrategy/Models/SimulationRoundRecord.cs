namespace BlackJackStrategy.Models;

public sealed record SimulationRoundRecord(
    long RoundNumber,
    decimal StartingBankroll,
    decimal Wager,
    decimal NetPayout,
    decimal EndingBankroll,
    bool UsedFreshShoe,
    int CardsRemainingAfterRound,
    IReadOnlyList<SimulationHandRecord> Hands);

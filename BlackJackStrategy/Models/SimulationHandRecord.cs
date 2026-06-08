using BlackJackData.Enums;

namespace BlackJackStrategy.Models;

public sealed record SimulationHandRecord(
    HandOutcomeType Outcome,
    decimal Wager,
    decimal NetPayout,
    bool WasSplitHand,
    bool UsedInsurance,
    bool WasDoubledDown,
    bool WasBust);

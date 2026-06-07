using BlackJackData.Enums;
using BlackJackData.ValueObjects;

namespace BlackJackData.Results;

public sealed record HandResult(
    SeatId SeatId,
    HandId HandId,
    HandOutcomeType Outcome,
    decimal Wager,
    decimal NetPayout,
    HandValue PlayerValue,
    HandValue DealerValue,
    bool UsedInsurance = false);

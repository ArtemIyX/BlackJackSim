using BlackJackData.Enums;
using BlackJackData.Structs;
using BlackJackData.ValueObjects;

namespace BlackJackData.Results;

public sealed record HandResult(
    SeatId SeatId,
    HandId HandId,
    HandOutcomeType Outcome,
    decimal Wager,
    decimal NetPayout,
    IReadOnlyList<CardDef> PlayerCards,
    HandValue PlayerValue,
    IReadOnlyList<CardDef> DealerCards,
    HandValue DealerValue,
    bool UsedInsurance = false);

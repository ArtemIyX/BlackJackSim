using BlackJackData.Rules;
using BlackJackData.ValueObjects;

namespace BlackJackEngine.Models;

public sealed record RoundStartOptions(
    RoundId RoundId,
    BlackjackRules Rules,
    IReadOnlyList<SeatBet> Seats);

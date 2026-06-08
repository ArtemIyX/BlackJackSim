using BlackJackData.ValueObjects;

namespace BlackJackEngine.Models;

public sealed record SeatBet(
    SeatId SeatId,
    decimal Wager,
    decimal Bankroll = 0m,
    bool IsParticipating = true);

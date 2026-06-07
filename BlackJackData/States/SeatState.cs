using BlackJackData.ValueObjects;

namespace BlackJackData.States;

public sealed record SeatState(
    SeatId Id,
    IReadOnlyList<HandState> Hands,
    decimal Bankroll = 0m,
    bool IsParticipating = true)
{
    public bool HasActiveHands => Hands.Any(hand => !hand.IsResolved);
}

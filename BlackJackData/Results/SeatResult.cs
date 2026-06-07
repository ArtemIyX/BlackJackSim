using BlackJackData.ValueObjects;

namespace BlackJackData.Results;

public sealed record SeatResult(
    SeatId SeatId,
    IReadOnlyList<HandResult> Hands,
    decimal NetPayout)
{
    public bool HasHands => Hands.Count > 0;
}

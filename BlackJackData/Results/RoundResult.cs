using BlackJackData.ValueObjects;

namespace BlackJackData.Results;

public sealed record RoundResult(
    RoundId RoundId,
    IReadOnlyList<SeatResult> Seats,
    HandValue DealerValue)
{
    public decimal NetPayout => Seats.Sum(seat => seat.NetPayout);
}

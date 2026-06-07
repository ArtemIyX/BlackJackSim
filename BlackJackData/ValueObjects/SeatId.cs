namespace BlackJackData.ValueObjects;

public readonly record struct SeatId(int Value)
{
    public override string ToString() => Value.ToString();
}

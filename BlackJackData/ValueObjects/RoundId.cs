namespace BlackJackData.ValueObjects;

public readonly record struct RoundId(long Value)
{
    public override string ToString() => Value.ToString();
}

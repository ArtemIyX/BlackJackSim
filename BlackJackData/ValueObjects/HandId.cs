namespace BlackJackData.ValueObjects;

public readonly record struct HandId(int Value)
{
    public override string ToString() => Value.ToString();
}

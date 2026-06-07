using BlackJackData.Structs;
using BlackJackData.ValueObjects;

namespace BlackJackData.States;

public sealed record DealerState(
    IReadOnlyList<CardDef> Cards,
    bool HoleCardRevealed = false)
{
    public HandValue Value => HandValue.FromCards(Cards);

    public bool IsBlackjack => Value.IsBlackjack;

    public bool IsBust => Value.IsBust;

    public CardDef? UpCard => Cards.Count > 0 ? Cards[0] : null;
}

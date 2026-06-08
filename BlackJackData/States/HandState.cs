using BlackJackData.Structs;
using BlackJackData.ValueObjects;

namespace BlackJackData.States;

public sealed record HandState(
    HandId Id,
    decimal Wager,
    IReadOnlyList<CardDef> Cards,
    bool IsStanding = false,
    bool IsDoubledDown = false,
    bool IsSurrendered = false,
    bool IsSplitHand = false,
    bool HasInsurance = false,
    bool InsuranceDecisionMade = false,
    int SplitDepth = 0)
{
    public HandValue Value => HandValue.FromCards(Cards);

    public bool IsBlackjack => !IsSplitHand && Value.IsBlackjack;

    public bool IsBust => Value.IsBust;

    public bool IsResolved => IsStanding || IsSurrendered || IsBust || IsBlackjack;

    public CardDef? UpCard => Cards.Count > 0 ? Cards[0] : null;

    public bool CanReceiveCards => !IsStanding && !IsSurrendered && !IsBust;
}

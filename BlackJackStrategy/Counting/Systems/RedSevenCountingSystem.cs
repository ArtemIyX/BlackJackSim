using BlackJackData.Enums;
using BlackJackData.Structs;
using BlackJackStrategy.Models;

namespace BlackJackStrategy.Counting.Systems;

public sealed class RedSevenCountingSystem : CardCountingSystemBase
{
    public RedSevenCountingSystem()
        : base("Red Seven", false, new CardCountTagSet(1, 1, 1, 1, 1, 0, 0, 0, -1, -1))
    {
    }

    protected override double GetCardTag(CardDef card)
    {
        if (card.Rank == CardRank.Seven)
        {
            return card.Suit is CardSuit.Hearts or CardSuit.Diamonds ? 1d : 0d;
        }

        return base.GetCardTag(card);
    }
}
